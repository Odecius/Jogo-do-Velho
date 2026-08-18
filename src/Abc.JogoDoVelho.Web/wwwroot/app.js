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
const avatarPanel = element("#avatar-panel");
const avatarPreview = element("#avatar-preview");
const cameraPreview = element("#camera-preview");
const captureCanvas = element("#capture-canvas");
const startCameraButton = element("#start-camera");
const captureButton = element("#capture-photo");
const cancelCameraButton = element("#cancel-camera");
const uploadButton = element("#upload-avatar");
const photoFile = element("#photo-file");
let connection;
let selectedAvatar;
let cameraStream;

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

async function secureUpload(url, formData) {
  const tokenResponse = await fetch("/api/antiforgery", { credentials: "same-origin" });
  if (!tokenResponse.ok) throw new Error("Não foi possível iniciar uma operação segura.");
  const token = (await tokenResponse.json()).requestToken;
  return fetch(url, { method: "POST", credentials: "same-origin",
    headers: { "X-CSRF-TOKEN": token }, body: formData });
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
    cells[index].replaceChildren();
    const avatarUrl = value === "Player1" ? snapshot.player1AvatarUrl : value === "Player2" ? snapshot.player2AvatarUrl : null;
    if (avatarUrl) {
      const image = document.createElement("img");
      image.src = avatarUrl;
      image.alt = value === snapshot.youAre ? "Seu avatar" : "Avatar do outro jogador";
      cells[index].append(image);
    }
    cells[index].className = `cell ${value ? value.toLowerCase() : ""}`;
    cells[index].disabled = Boolean(value) || snapshot.roomStatus !== "Playing" ||
      snapshot.gameStatus !== "InProgress" || snapshot.currentPlayer !== snapshot.youAre;
  });
  player1Status.textContent = `Jogador 1: ${snapshot.player1Connected ? "online" : "offline"}${snapshot.player1HasAvatar ? ", foto pronta" : ", sem foto"}`;
  player2Status.textContent = `Jogador 2: ${snapshot.player2Connected ? "online" : "offline"}${snapshot.player2HasAvatar ? ", foto pronta" : ", sem foto"}`;
  const youHaveAvatar = snapshot.youAre === "Player1" ? snapshot.player1HasAvatar : snapshot.player2HasAvatar;
  const yourAvatarUrl = snapshot.youAre === "Player1" ? snapshot.player1AvatarUrl : snapshot.player2AvatarUrl;
  avatarPanel.hidden = snapshot.roomStatus === "Playing" || snapshot.roomStatus === "Finished";
  if (youHaveAvatar && yourAvatarUrl) { avatarPreview.src = yourAvatarUrl; avatarPreview.hidden = false; }
  if (snapshot.roomStatus === "WaitingForPlayer") message.textContent = "Aguardando segundo jogador…";
  else if (snapshot.roomStatus === "WaitingForAvatars") message.textContent = youHaveAvatar ? "Aguardando a foto do outro jogador…" : "Envie sua foto para começar.";
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

function stopCamera() {
  cameraStream?.getTracks().forEach(track => track.stop()); cameraStream = null;
  cameraPreview.hidden = true; captureButton.hidden = true; cancelCameraButton.hidden = true;
}

function selectAvatar(blob) {
  selectedAvatar = blob; avatarPreview.src = URL.createObjectURL(blob); avatarPreview.hidden = false;
  uploadButton.hidden = false;
}

startCameraButton.addEventListener("click", async () => {
  showError("");
  try {
    cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false });
    cameraPreview.srcObject = cameraStream; cameraPreview.hidden = false;
    captureButton.hidden = false; cancelCameraButton.hidden = false;
  } catch { showError("Não foi possível acessar a câmera. Você pode escolher uma foto."); }
});
cancelCameraButton.addEventListener("click", stopCamera);
captureButton.addEventListener("click", () => {
  const size = Math.min(cameraPreview.videoWidth, cameraPreview.videoHeight);
  const x = (cameraPreview.videoWidth - size) / 2; const y = (cameraPreview.videoHeight - size) / 2;
  captureCanvas.width = 512; captureCanvas.height = 512;
  captureCanvas.getContext("2d").drawImage(cameraPreview, x, y, size, size, 0, 0, 512, 512);
  captureCanvas.toBlob(blob => { if (blob) selectAvatar(blob); }, "image/jpeg", .9);
  stopCamera();
});
photoFile.addEventListener("change", () => { if (photoFile.files[0]) selectAvatar(photoFile.files[0]); });
uploadButton.addEventListener("click", async () => {
  if (!selectedAvatar) return;
  uploadButton.disabled = true; showError("");
  try {
    const form = new FormData(); form.append("avatar", selectedAvatar, "avatar");
    const response = await secureUpload(`${window.location.pathname.replace("/game/", "/api/games/")}/avatar`, form);
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error({ ImageTooLarge: "A foto excede 5 MiB.", UnsupportedImageType: "Use JPEG, PNG ou WebP.",
      InvalidImageSignature: "O conteúdo não corresponde ao formato declarado.", CorruptImage: "A imagem está corrompida.",
      ImageDimensionsTooLarge: "A imagem excede 4096 × 4096.", GameAlreadyStarted: "A partida já começou." }[payload.error] || "Não foi possível enviar a foto.");
    uploadButton.hidden = true; selectedAvatar = null;
  } catch (error) { showError(error.message); } finally { uploadButton.disabled = false; }
});

createButton.addEventListener("click", createGame);
copyButton.addEventListener("click", async () => {
  await navigator.clipboard.writeText(joinLink.value);
  copyButton.textContent = "Copiado";
});

const route = window.location.pathname.match(/^\/game\/([2-9A-HJ-NP-Z]{8})$/i);
if (route) openGame(route[1].toUpperCase()).catch(error => showError(error.message));
