"use strict";
const $ = selector => document.querySelector(selector);
const ui = { home: $("#home-view"), game: $("#game-view"), routeError: $("#route-error"), routeDetail: $("#route-error-detail"), create: $("#create-game"), code: $("#public-code"), link: $("#join-link"), copy: $("#copy-link"), share: $("#share-link"), message: $("#game-message"), connection: $("#connection-message"), error: $("#error-message"), board: $("#board"), p1: $("#player1-status"), p2: $("#player2-status"), avatarPanel: $("#avatar-panel"), avatarPreview: $("#avatar-preview"), camera: $("#camera-preview"), canvas: $("#capture-canvas"), startCamera: $("#start-camera"), capture: $("#capture-photo"), cancelCamera: $("#cancel-camera"), upload: $("#upload-avatar"), file: $("#photo-file"), youAvatar: $("#you-avatar"), opponentAvatar: $("#opponent-avatar"), youPlaceholder: $("#you-placeholder"), opponentPlaceholder: $("#opponent-placeholder"), youScore: $("#you-score"), opponentScore: $("#opponent-score"), draws: $("#draw-score"), result: $("#result-panel"), resultTitle: $("#result-title"), rematchMessage: $("#rematch-message"), rematch: $("#rematch") };
let connection; let selectedAvatar; let cameraStream; let latestSnapshot; let movePending = false;

for (let index = 0; index < 9; index += 1) {
  const cell = document.createElement("button"); cell.type = "button"; cell.className = "cell";
  cell.setAttribute("aria-label", `Casa ${index + 1}, vazia`); cell.addEventListener("click", () => placeMove(index)); ui.board.append(cell);
}

async function csrf() { const response = await fetch("/api/antiforgery", { credentials: "same-origin" }); if (!response.ok) throw new Error("Não foi possível iniciar uma operação segura."); return (await response.json()).requestToken; }
async function securePost(url, body) { return fetch(url, { method: "POST", credentials: "same-origin", headers: { "X-CSRF-TOKEN": await csrf() }, body }); }
function showError(value) { ui.error.textContent = value; }

async function createGame() {
  showError(""); ui.create.disabled = true; ui.create.textContent = "Criando partida…";
  try { const response = await securePost("/api/games"); if (!response.ok) throw new Error("Não foi possível criar a partida. Verifique sua conexão e tente novamente."); window.location.assign((await response.json()).joinUrl); }
  catch (error) { showError(error.message); ui.create.disabled = false; ui.create.textContent = "Criar partida"; }
}

async function openGame(code) {
  ui.home.hidden = true; ui.game.hidden = false; ui.code.textContent = code; ui.link.value = `${window.location.origin}/game/${code}`; ui.share.hidden = typeof navigator.share !== "function";
  const response = await securePost(`/api/games/${encodeURIComponent(code)}/join`);
  if (response.status === 404) return showRouteError("Partida não encontrada ou expirada.");
  if (response.status === 409) return showRouteError("Esta partida já tem dois jogadores.");
  if (!response.ok) throw new Error("Não foi possível entrar na partida. Tente novamente.");
  await connect(code);
}

function showRouteError(detail) { ui.game.hidden = true; ui.routeError.hidden = false; ui.routeDetail.textContent = detail; }
async function connect(code) {
  connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").withAutomaticReconnect().build();
  connection.on("GameStateChanged", renderSnapshot); connection.on("MoveRejected", outcome => { movePending = false; renderSnapshot(latestSnapshot); showError(moveError(outcome)); });
  connection.on("RematchRejected", () => showError("A revanche só pode ser solicitada depois do fim da rodada."));
  connection.onreconnecting(() => { ui.connection.textContent = "Reconectando…"; lockBoard(); });
  connection.onreconnected(async () => { ui.connection.textContent = "Conectado novamente."; await connection.invoke("JoinGame", code); });
  connection.onclose(() => { ui.connection.textContent = "Não foi possível reconectar. Recarregue a página para tentar novamente."; lockBoard(); });
  await connection.start(); await connection.invoke("JoinGame", code); ui.connection.textContent = "";
}

async function placeMove(index) { if (!connection || movePending) return; movePending = true; lockBoard(); showError(""); try { await connection.invoke("PlaceMove", index); } catch { movePending = false; showError("A jogada não foi enviada. Verifique sua conexão."); renderSnapshot(latestSnapshot); } }
function lockBoard() { ui.board.querySelectorAll(".cell").forEach(cell => { cell.disabled = true; }); }

function renderSnapshot(snapshot) {
  if (!snapshot) return; latestSnapshot = snapshot; movePending = false;
  const yours = snapshot.youAre === "Player1" ? snapshot.player1AvatarUrl : snapshot.player2AvatarUrl;
  const theirs = snapshot.youAre === "Player1" ? snapshot.player2AvatarUrl : snapshot.player1AvatarUrl;
  setAvatar(ui.youAvatar, ui.youPlaceholder, yours, "Sem foto"); setAvatar(ui.opponentAvatar, ui.opponentPlaceholder, theirs, opponentConnected(snapshot) ? "Sem foto" : "Aguardando…");
  snapshot.board.forEach((value, index) => {
    const cell = ui.board.children[index]; cell.replaceChildren(); const url = value === "Player1" ? snapshot.player1AvatarUrl : value === "Player2" ? snapshot.player2AvatarUrl : null;
    if (url) { const image = document.createElement("img"); image.src = url; image.alt = value === snapshot.youAre ? "Sua jogada" : "Jogada do adversário"; cell.append(image); }
    cell.className = `cell ${value ? value.toLowerCase() : ""}`; cell.setAttribute("aria-label", value ? `Casa ${index + 1}, ${value === snapshot.youAre ? "sua jogada" : "jogada do adversário"}` : `Casa ${index + 1}, vazia`);
    cell.disabled = Boolean(value) || movePending || snapshot.roomStatus !== "Playing" || snapshot.gameStatus !== "InProgress" || snapshot.currentPlayer !== snapshot.youAre;
  });
  ui.p1.textContent = `Jogador 1 ${snapshot.player1Connected ? "online" : "offline"}`; ui.p2.textContent = `Jogador 2 ${snapshot.player2Connected ? "online" : "offline"}`;
  const youAreP1 = snapshot.youAre === "Player1"; ui.youScore.textContent = youAreP1 ? snapshot.player1Score : snapshot.player2Score; ui.opponentScore.textContent = youAreP1 ? snapshot.player2Score : snapshot.player1Score; ui.draws.textContent = snapshot.draws;
  const youHaveAvatar = youAreP1 ? snapshot.player1HasAvatar : snapshot.player2HasAvatar; ui.avatarPanel.hidden = snapshot.roomStatus === "Playing" || snapshot.roomStatus === "Finished";
  if (youHaveAvatar && yours) { ui.avatarPreview.src = yours; ui.avatarPreview.hidden = false; }
  ui.result.hidden = snapshot.roomStatus !== "Finished";
  if (snapshot.roomStatus === "WaitingForPlayer") ui.message.textContent = youHaveAvatar ? "Aguardando o outro jogador." : "Adicione sua selfie e compartilhe o convite.";
  else if (snapshot.roomStatus === "WaitingForAvatars") ui.message.textContent = youHaveAvatar ? "Aguardando a selfie do outro jogador." : "Adicione sua selfie para começar.";
  else if (!opponentConnected(snapshot)) ui.message.textContent = "O outro jogador desconectou. Aguardando reconexão…";
  else if (snapshot.gameStatus === "InProgress") ui.message.textContent = snapshot.currentPlayer === snapshot.youAre ? "Sua vez." : "Vez do outro jogador.";
  else showResult(snapshot);
}

function opponentConnected(snapshot) { return snapshot.youAre === "Player1" ? snapshot.player2Connected : snapshot.player1Connected; }
function setAvatar(image, placeholder, url, text) { image.hidden = !url; placeholder.hidden = Boolean(url); if (url) image.src = url; else placeholder.textContent = text; }
function showResult(snapshot) { ui.resultTitle.textContent = snapshot.gameStatus === "Draw" ? "Empate!" : snapshot.winner === snapshot.youAre ? "Você venceu!" : "O outro jogador venceu."; ui.message.textContent = ui.resultTitle.textContent; ui.rematch.disabled = snapshot.youRequestedRematch; ui.rematch.textContent = snapshot.youRequestedRematch ? "Revanche solicitada" : "Jogar novamente"; ui.rematchMessage.textContent = snapshot.youRequestedRematch ? "Aguardando o outro jogador aceitar…" : snapshot.opponentRequestedRematch ? "O outro jogador quer uma revanche." : "Ambos precisam aceitar para iniciar uma nova rodada."; }
function moveError(outcome) { return { RoomNotReady: "A partida ainda não está pronta ou já terminou.", InvalidCell: "Essa casa não existe.", NotPlayersTurn: "Ainda não é sua vez.", CellOccupied: "Essa casa já está ocupada.", GameFinished: "Esta partida já terminou." }[outcome] || "Jogada rejeitada."; }

function stopCamera() { cameraStream?.getTracks().forEach(track => track.stop()); cameraStream = null; ui.camera.hidden = true; ui.capture.hidden = true; ui.cancelCamera.hidden = true; }
function selectAvatar(blob) { selectedAvatar = blob; ui.avatarPreview.src = URL.createObjectURL(blob); ui.avatarPreview.hidden = false; ui.upload.hidden = false; ui.upload.textContent = "Usar esta foto"; }
ui.startCamera.addEventListener("click", async () => { showError(""); try { cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false }); ui.camera.srcObject = cameraStream; ui.camera.hidden = false; ui.capture.hidden = false; ui.cancelCamera.hidden = false; } catch { showError("Não foi possível acessar a câmera. Você ainda pode escolher uma foto do seu dispositivo."); } });
ui.cancelCamera.addEventListener("click", stopCamera); ui.capture.addEventListener("click", () => { const size = Math.min(ui.camera.videoWidth, ui.camera.videoHeight); ui.canvas.width = 512; ui.canvas.height = 512; ui.canvas.getContext("2d").drawImage(ui.camera, (ui.camera.videoWidth - size) / 2, (ui.camera.videoHeight - size) / 2, size, size, 0, 0, 512, 512); ui.canvas.toBlob(blob => { if (blob) selectAvatar(blob); }, "image/jpeg", .9); stopCamera(); });
ui.file.addEventListener("change", () => { if (ui.file.files[0]) selectAvatar(ui.file.files[0]); });
ui.upload.addEventListener("click", async () => { if (!selectedAvatar) return; ui.upload.disabled = true; ui.upload.textContent = "Enviando foto…"; ui.startCamera.disabled = true; showError(""); try { const form = new FormData(); form.append("avatar", selectedAvatar, "avatar"); const response = await securePost(`${window.location.pathname.replace("/game/", "/api/games/")}/avatar`, form); const payload = await response.json().catch(() => ({})); if (!response.ok) throw new Error({ ImageTooLarge: "A foto excede 5 MiB.", UnsupportedImageType: "Use JPEG, PNG ou WebP.", InvalidImageSignature: "O conteúdo não corresponde ao formato declarado.", CorruptImage: "A imagem está corrompida.", ImageDimensionsTooLarge: "A imagem excede 4096 × 4096.", GameAlreadyStarted: "A partida já começou." }[payload.error] || "Não foi possível enviar a foto."); ui.upload.hidden = true; selectedAvatar = null; ui.message.textContent = "Foto pronta."; } catch (error) { showError(error.message); ui.upload.textContent = "Tentar novamente"; } finally { ui.upload.disabled = false; ui.startCamera.disabled = false; } });

ui.create.addEventListener("click", createGame); ui.copy.addEventListener("click", async () => { const invitation = `Vamos jogar Jogo do Velho comigo:\n${ui.link.value}`; try { await navigator.clipboard.writeText(invitation); ui.copy.textContent = "Convite copiado"; } catch { ui.link.focus(); ui.link.select(); showError("Copie o convite selecionado acima."); } });
ui.share.addEventListener("click", async () => { try { await navigator.share({ title: "Jogo do Velho", text: "Vamos jogar Jogo do Velho comigo!", url: ui.link.value }); } catch (error) { if (error.name !== "AbortError") showError("Não foi possível compartilhar. Use Copiar convite."); } });
ui.rematch.addEventListener("click", async () => { ui.rematch.disabled = true; showError(""); try { await connection.invoke("RequestRematch"); } catch { ui.rematch.disabled = false; showError("Não foi possível solicitar a revanche."); } });

const route = window.location.pathname.match(/^\/game\/([2-9A-HJ-NP-Z]{8})$/i); if (route) openGame(route[1].toUpperCase()).catch(error => showError(error.message));
