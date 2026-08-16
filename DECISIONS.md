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
