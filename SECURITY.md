# Segurança

## Baseline

- Segredos devem vir de User Secrets, variáveis de ambiente ou `.env` ignorado.
- `.env`, uploads, logs, `bin/` e `obj/` não podem ser versionados.
- O servidor será autoridade sobre partidas e jogadas.
- Tokens privados não poderão aparecer em URLs ou logs.
- Endpoints sensíveis terão validação, autorização e rate limiting nas fases correspondentes.
- HTTPS será obrigatório em produção.
- Criação e entrada em partidas exigem antiforgery token via header.
- Cookies de sessão são opacos, `HttpOnly`, `SameSite=Lax` e `Secure` fora de ambientes locais/teste.
- Códigos públicos não concedem autorização e endpoints de criação/entrada possuem rate limiting.
- SignalR resolve a posição exclusivamente pela sessão server-side.

## Upload de avatar

- Somente JPEG, PNG e WebP, até 5 MiB e 4096 × 4096.
- MIME declarado, magic bytes, formato decodificado e dimensões precisam concordar.
- ImageSharp decodifica, faz crop 512 × 512, remove perfis de metadados e reencoda em WebP.
- O storage configurável fica fora de `wwwroot`; nomes são GUIDs e caminhos são canonicalizados.
- Path traversal e reparse points são rejeitados antes de leitura ou exclusão.
- Upload exige sessão pertencente à partida, antiforgery e limite de 10 tentativas por IP a cada 15 minutos.
- Leitura exige sessão da mesma partida; código público isolado não autoriza.
- Respostas usam MIME controlado, `Content-Disposition: inline`, `nosniff` e cache privado `no-store`.
- Substituição é permitida somente antes de `Playing`; o arquivo anterior é removido.
- Cleanup idempotente roda a cada 15 minutos e remove registros com 24 horas, continuando após falhas individuais.
- A câmera só é solicitada após clique e requer HTTPS em produção (localhost é contexto seguro no desenvolvimento).

## Relato de vulnerabilidades

Não publique credenciais ou dados pessoais em issues. Comunique o responsável pelo repositório por canal privado.

## Produção

- deploy público ativo somente pelo proxy e túnel; a aplicação não expõe porta no host;
- `AllowedHosts` aceita apenas o hostname público configurado;
- apenas o proxy interno configurado é confiável para `X-Forwarded-For` e `X-Forwarded-Proto`;
- segredos ficam em arquivo operacional protegido no servidor e não são versionados;
- banco e role são dedicados, sem privilégios administrativos;
- health checks, headers, antiforgery, cookies e isolamento do avatar foram verificados pela rota pública.

## UX multiplayer

Refresh, reconnect e rematch reutilizam exclusivamente o cookie opaco existente; nenhum token é enviado em URL, DOM ou snapshot. O servidor continua validando turno, estado da rodada e identidade, independentemente dos bloqueios visuais do tabuleiro. Um terceiro jogador continua sem acesso mesmo após disconnect ou refresh.

## Hardening e riscos residuais

- salas/tokens expiram após 24 horas de inatividade e são varridos a cada 15 minutos;
- CSP não permite inline/eval/wildcard e impede framing; câmera fica restrita à própria origem;
- o container Web é não-root, read-only, sem capabilities e com recursos moderadamente limitados;
- PostgreSQL mantém código público único, jogador único por posição e constraint de posição 1/2;
- não existe rate limit por mensagem SignalR nem defesa distribuída: proxy/edge será necessário antes de produção;
- uma única instância é suportada; reinício encerra partidas e limites em memória reiniciam;
- o ambiente Development possui diagnóstico e nunca deve ser exposto publicamente.
- chaves de Data Protection são efêmeras; reiniciar a aplicação invalida cookies antiforgery existentes;
- TLS termina no edge e o salto interno do túnel ao proxy é HTTP em rede Docker privada;
- teste final com dois dispositivos físicos e webcam real permanece pendente para a Fase J.

Consulte `docs/threat-model.md` e `checklists/security-review.md`.
