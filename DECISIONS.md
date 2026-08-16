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

