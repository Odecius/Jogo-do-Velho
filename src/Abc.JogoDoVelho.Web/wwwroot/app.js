"use strict";

const element = selector => document.querySelector(selector);
const homeView = element("#home-view");
const gameView = element("#game-view");
const createButton = element("#create-game");
const codeLabel = element("#public-code");
const joinLink = element("#join-link");
const copyButton = element("#copy-link");
const message = element("#game-message");
const errorMessage = element("#error-message");
const board = element("#board");
const player1Status = element("#player1-status");
const player2Status = element("#player2-status");
let connection;

for (let index = 0; index < 9; index += 1) {
  const cell = document.createElement("button");
  cell.type = "button";
  cell.className = "cell";
  cell.setAttribute("aria-label", `Célula ${index + 1}`);
  cell.addEventListener("click", () => placeMove(index));
  board.append(cell);
}

async function securePost(url) {
  const tokenResponse = await fetch("/api/antiforgery", { credentials: "same-origin" });
  if (!tokenResponse.ok) throw new Error("Não foi possível iniciar uma operação segura.");
  const token = (await tokenResponse.json()).requestToken;
  return fetch(url, { method: "POST", credentials: "same-origin", headers: { "X-CSRF-TOKEN": token } });
}

async function createGame() {
  showError("");
  createButton.disabled = true;
  try {
    const response = await securePost("/api/games");
    if (!response.ok) throw new Error("Não foi possível criar a partida.");
    window.location.assign((await response.json()).joinUrl);
  } catch (error) {
    showError(error.message);
    createButton.disabled = false;
  }
}

async function openGame(code) {
  homeView.hidden = true;
  gameView.hidden = false;
  codeLabel.textContent = code;
  joinLink.value = window.location.href;
  const response = await securePost(`/api/games/${encodeURIComponent(code)}/join`);
  if (response.status === 409) throw new Error("Esta partida já possui dois jogadores.");
  if (!response.ok) throw new Error("Partida não encontrada ou indisponível.");
  await connect(code);
}

async function connect(code) {
  connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").withAutomaticReconnect().build();
  connection.on("GameStateChanged", renderSnapshot);
  connection.on("MoveRejected", outcome => showError(moveError(outcome)));
  connection.onreconnecting(() => { message.textContent = "Reconectando…"; });
  connection.onreconnected(() => connection.invoke("JoinGame", code));
  connection.onclose(() => { message.textContent = "Ligação encerrada."; });
  await connection.start();
  await connection.invoke("JoinGame", code);
}

async function placeMove(index) {
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
  showError("");
  await connection.invoke("PlaceMove", index);
}

function renderSnapshot(snapshot) {
  const cells = board.querySelectorAll(".cell");
  snapshot.board.forEach((value, index) => {
    cells[index].textContent = value === "Player1" ? "P1" : value === "Player2" ? "P2" : "";
    cells[index].className = `cell ${value ? value.toLowerCase() : ""}`;
    cells[index].disabled = Boolean(value) || snapshot.roomStatus !== "Playing" ||
      snapshot.gameStatus !== "InProgress" || snapshot.currentPlayer !== snapshot.youAre;
  });
  player1Status.textContent = `P1 ${snapshot.player1Connected ? "online" : "offline"}`;
  player2Status.textContent = `P2 ${snapshot.player2Connected ? "online" : "offline"}`;
  if (snapshot.roomStatus === "WaitingForPlayer") message.textContent = "Aguardando segundo jogador…";
  else if (snapshot.gameStatus === "Won") message.textContent = snapshot.winner === snapshot.youAre ? "Você venceu." : "O outro jogador venceu.";
  else if (snapshot.gameStatus === "Draw") message.textContent = "Empate.";
  else message.textContent = snapshot.currentPlayer === snapshot.youAre ? "Sua vez." : "Aguarde a jogada do outro jogador.";
}

function moveError(outcome) {
  return { RoomNotReady: "Aguardando o segundo jogador.", InvalidCell: "Célula inválida.",
    NotPlayersTurn: "Não é a sua vez.", CellOccupied: "Esta célula já está ocupada.",
    GameFinished: "A partida já terminou." }[outcome] || "Jogada rejeitada.";
}

function showError(value) { errorMessage.textContent = value; }

createButton.addEventListener("click", createGame);
copyButton.addEventListener("click", async () => {
  await navigator.clipboard.writeText(joinLink.value);
  copyButton.textContent = "Copiado";
});

const route = window.location.pathname.match(/^\/game\/([2-9A-HJ-NP-Z]{8})$/i);
if (route) openGame(route[1].toUpperCase()).catch(error => showError(error.message));
