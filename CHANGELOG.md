# Changelog

Todas as mudanças relevantes serão registradas neste arquivo.

## Unreleased

### Added

- Baseline documental e de qualidade inicial.
- Solution .NET 8 com projetos de domínio, infraestrutura, web e testes.
- Foundation de EF Core e PostgreSQL sem migration artificial.
- Ambiente Docker local com health checks `/health` e `/ready`.
- Frontend mínimo com identidade da ABC Solutions.
- Testes de configuração e integração da aplicação.
- Engine pura do jogo da velha com jogadores, turnos, validações, vitória e empate.
- Cobertura unitária das oito combinações vencedoras e do encapsulamento do tabuleiro.
- Coordenação de salas multiplayer e sessões privadas temporárias.
- Hub SignalR com snapshots server-authoritative, reconnect e presença.
- Endpoints protegidos por antiforgery e rate limiting.
- Persistência mínima de metadados com migration `InitialGameMetadata`.
- Frontend responsivo para criação, compartilhamento e partida com `P1`/`P2`.

## 2026-08-18 — Fase I

- deploy de produção isolado em container não-root, sem porta publicada no host;
- banco e usuário PostgreSQL dedicados, com migrations executadas explicitamente;
- publicação por proxy reverso e Cloudflare Tunnel com HTTPS público;
- smoke tests públicos de health, criação de partida, SignalR, cookies e avatar privado;
- procedimento de rollback e riscos residuais documentados.

## 2026-08-18 — Fase G

- threat model e checklists de segurança/release;
- isolamento obrigatório entre duas partidas e cenários de cliente malicioso;
- expiração de salas e sessões após 24 horas de inatividade;
- validação de configuração crítica e constraint de posição do jogador;
- hardening do container Web e regressões adicionais de upload, CSP e antiforgery.

## 2026-08-18 — Fase F

- home, lobby e convite final com Clipboard e Web Share opcional;
- estados amigáveis para entrada, upload, turno, disconnect e reconnect;
- resultado, placar em memória e rematch consentido com nova instância de `Game`;
- interface responsiva, acessível por teclado e avatares como foco do tabuleiro;
- testes de rematch, placar, reconnect e fluxo integrado completo.

## 2026-08-18 — Fase E

- upload autenticado de JPEG, PNG e WebP com validação real e antiforgery;
- normalização WebP 512 × 512 sem metadados;
- storage privado temporário, retenção de 24 horas e cleanup automático;
- câmera, fallback por arquivo, consentimento e avatares no tabuleiro;
- estado multiplayer `WaitingForAvatars` e atualização SignalR;
- migration `AddPlayerAvatarMetadata` e volume Docker dedicado.
