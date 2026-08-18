# Decisões técnicas

## 2026-08-16 — Arquitetura simples em três projetos

Usar `Domain`, `Infrastructure` e `Web`, sem um projeto `Application`. O MVP ainda não possui complexidade que justifique uma camada adicional.

## 2026-08-16 — Stack do MVP

Usar ASP.NET Core 8, PostgreSQL, Entity Framework Core, Npgsql e frontend vanilla. SignalR será introduzido na fase de multiplayer. React e outros frameworks SPA não serão usados sem necessidade clara.

## 2026-08-16 — Estado ativo em memória

O estado de partidas ativas será mantido inicialmente em memória; PostgreSQL persistirá somente metadados que tragam benefício real. Reinícios perderão partidas ativas. Redis não será introduzido no MVP.

## 2026-08-16 — Migrations não artificiais

A infraestrutura do EF Core será preparada na foundation, mas a primeira migration aguardará o modelo real de `Game` e `Player`. Não será criada migration vazia.

## 2026-08-16 — Branch específica da foundation

Usar `agent/jogo-do-velho-foundation` por instrução específica do projeto. Esta é uma exceção documentada ao padrão geral `feature/...` do ABC Development Standard.

## 2026-08-16 — Selfies temporárias

As imagens ficarão fora de diretórios executáveis, com retenção planejada de 24 horas, validação real do conteúdo e remoção automática. A implementação ocorrerá em fase posterior.

## 2026-08-16 — Engine pura do jogo

O domínio usa `Game`, `Board`, `PlayerPosition`, `GameStatus` e `MoveResult`, sem dependências de infraestrutura. O tabuleiro possui nove células indexadas de 0 a 8. `Player1` sempre começa.

Erros esperados de gameplay são retornados por `MoveResult`: `Success`, `InvalidCell`, `NotPlayersTurn`, `CellOccupied` e `GameFinished`. O domínio não contém mensagens de interface nem usa exceptions para esses fluxos.

A engine começa diretamente em `InProgress`; `Waiting` pertence à coordenação multiplayer e não ao estado lógico de uma rodada. Os estados terminais são `Won` e `Draw`.

O array interno do tabuleiro não é exposto. Consumidores recebem uma coleção somente leitura, e toda mutação passa por `Game.PlaceMove`. A engine é síncrona e não implementa locks; concorrência será tratada pela camada coordenadora futura.

Uma nova rodada será representada por uma nova instância de `Game`, sem método `Reset`, reduzindo mutabilidade e evitando estado residual.

## 2026-08-17 — Coordenação multiplayer em memória

`GameSessionManager` mantém salas, engine ativa, sessões e conexões em memória. Cada sala possui seu próprio `SemaphoreSlim`, garantindo uma mutação por vez sem bloquear partidas diferentes. Reiniciar a aplicação encerra partidas e invalida sessões ativas; Redis não será usado no MVP.

O estado da sala (`WaitingForPlayer`, `Playing`, `Finished`) representa presença e ciclo multiplayer, enquanto `Domain.GameStatus` representa somente as regras da rodada (`InProgress`, `Won`, `Draw`).

## 2026-08-17 — Códigos e identidade temporária

Códigos públicos usam oito caracteres de um alfabeto sem `0`, `O`, `1`, `I` e `L`, gerados com `RandomNumberGenerator` e comparados sem diferença entre maiúsculas e minúsculas. Eles permitem localizar uma sala, mas não autorizam jogadas.

Cada jogador recebe token opaco de 256 bits em cookie `HttpOnly`, `SameSite=Lax`, `Secure` fora de Development/Testing e validade de oito horas. Tokens ficam somente em memória e não aparecem em URL, resposta JSON, DOM ou logs.

## 2026-08-17 — SignalR e snapshots personalizados

O Hub expõe somente `JoinGame(publicCode)` e `PlaceMove(cellIndex)`. O jogador é determinado pelo cookie; posição ou ID não são aceitos do cliente. Cada posição possui grupo interno baseado no `GameId`, permitindo snapshots com `YouAre` sem expor IDs internos. Os eventos são `GameStateChanged` e `MoveRejected`.

Reconnect reutiliza a sessão e reassocia uma nova conexão. Disconnect apenas atualiza presença e não libera a vaga nem causa derrota.

## 2026-08-17 — Persistência mínima e proteção HTTP

PostgreSQL guarda somente metadados de `GameEntity` e `PlayerEntity`; jogadas e tokens não são persistidos. A migration inicial é `InitialGameMetadata`.

Criação e entrada usam antiforgery por cookie/header. O frontend obtém um request token em `/api/antiforgery`. SignalR não usa antiforgery porque sua autorização depende do cookie `SameSite` e da validação server-side da sessão no Hub. Rate limiting fixo protege criação e entrada.

## 2026-08-18 — Avatares temporários normalizados

Usar ImageSharp 3.1.12, compatível com .NET 8 e sem dependência de `System.Drawing.Common`. A licença aplicável e sua condição de elegibilidade estão em `THIRD_PARTY_NOTICES.md`.

O servidor aceita JPEG, PNG e WebP de até 5 MiB e 4096 × 4096, exige concordância entre MIME, magic bytes e decoder, aplica orientação e crop central, remove EXIF/XMP/IPTC/ICC e guarda somente WebP 512 × 512. Arquivos recebem GUID e ficam em `storage/avatars`, fora de `wwwroot`.

A partida fica `WaitingForAvatars` até dois jogadores e dois avatares estarem presentes. Substituição é permitida antes de `Playing` e bloqueada depois. `Domain.GameStatus` não foi alterado. A retenção é de 24 horas, com cleanup a cada 15 minutos. O volume temporário não deve ser incluído em backups permanentes.

## 2026-08-18 — Rematch e placar voláteis

Cada jogador solicita rematch pelo mesmo método SignalR. Uma solicitação isolada apenas atualiza o snapshot; somente o consentimento dos dois cria uma nova instância de `Domain.Game`. Avatares e sessões são preservados, flags são limpas e Player1 começa novamente.

Vitórias e empates são contabilizados somente na sala em memória. Não há tabela de rodada, histórico persistente ou alteração no Domain. Reiniciar a aplicação zera o placar e encerra a sala.

Convites usam `window.location.origin`, Clipboard API com fallback por seleção e Web Share API quando disponível. QR Code permanece no roadmap para evitar dependência e escopo adicionais.
