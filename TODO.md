# TODO

## Fase B — Foundation

- [x] Criar solution e projetos .NET 8.
- [x] Configurar EF Core e PostgreSQL.
- [x] Preparar Docker local.
- [x] Adicionar health checks e testes de integração.
- [x] Executar todas as validações da foundation.

## Fase C — Domain

- [x] Modelar jogadores, tabuleiro e estados.
- [x] Implementar jogadas e alternância de turno.
- [x] Validar índice, turno, ocupação e término.
- [x] Detectar as oito vitórias e empate.
- [x] Proteger o estado contra alteração externa.
- [x] Adicionar cobertura unitária das regras.

## Fase D — Multiplayer

- [x] Criar e entrar em salas com código público criptográfico.
- [x] Associar Player1 e Player2 a sessões privadas temporárias.
- [x] Coordenar jogadas concorrentes usando lock por sala.
- [x] Transmitir snapshots personalizados com SignalR.
- [x] Persistir metadados mínimos de jogos e jogadores.
- [x] Adicionar antiforgery, rate limiting e interface multiplayer mínima.
- [x] Cobrir HTTP e SignalR com testes de integração.

## Fase E — Avatares

- [x] Validar, normalizar e armazenar avatares temporários.
- [x] Adicionar câmera, fallback de arquivo e cleanup automático.

## Fase F — Fluxo final

- [x] Melhorar home, lobby, convite e mensagens de erro.
- [x] Adicionar rematch com consentimento dos dois jogadores.
- [x] Adicionar placar volátil da sessão.
- [x] Cobrir reconnect, refresh lógico e fluxo multiplayer completo.

## Fase I — Produção controlada

- [x] Criar stack de produção isolada e aplicar migrations explicitamente.
- [x] Publicar pelo proxy reverso e Cloudflare Tunnel com HTTPS.
- [x] Validar health, readiness, assets, SignalR, cookies e avatar pela rota pública.
- [x] Documentar operação, rollback e riscos residuais.
- [ ] Executar validação em dois dispositivos físicos e webcam real na Fase J.
