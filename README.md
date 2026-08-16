# Jogo do Velho — ABC Solutions

MVP experimental de um jogo da velha multiplayer no qual duas pessoas usarão selfies como símbolos do tabuleiro.

## Estado atual

Fase B — foundation técnica e documental. O jogo, o multiplayer e o upload de selfies ainda não estão implementados.

## Stack planejada

- .NET 8 e ASP.NET Core 8;
- SignalR (fase futura);
- HTML, CSS e JavaScript vanilla;
- PostgreSQL, Entity Framework Core e Npgsql;
- xUnit para testes;
- Docker Compose para desenvolvimento local.

## Arquitetura

A solution será dividida em `Domain`, `Infrastructure` e `Web`, com projetos separados para testes unitários e de integração. Consulte `docs/architecture.md` e `DECISIONS.md`.

## Execução e testes

As instruções serão mantidas em `docs/setup.md` e `docs/testing.md` à medida que a foundation técnica for adicionada nesta fase.

## Privacidade

Selfies serão dados pessoais temporários, sem reconhecimento ou análise facial. A política técnica está em `PRIVACY.md`. O upload ainda não está implementado.

## Limitações atuais

- nenhuma regra de jogo implementada;
- nenhum fluxo multiplayer;
- nenhum upload de imagem;
- nenhuma persistência de partidas;
- nenhum deploy realizado.

## Roadmap

Consulte `ROADMAP.md`.

**Production deployment: not performed**

Developed by Abc Solutions | Built with quality and care

