# Jogo do Velho — ABC Solutions

MVP experimental de um jogo da velha multiplayer no qual duas pessoas usarão selfies como símbolos do tabuleiro.

## Estado atual

Fase D — primeiro multiplayer funcional concluído localmente. Selfies e upload ainda não estão implementados.

## Stack planejada

- .NET 8 e ASP.NET Core 8;
- SignalR para atualização multiplayer em tempo real;
- HTML, CSS e JavaScript vanilla;
- PostgreSQL, Entity Framework Core e Npgsql;
- xUnit para testes;
- Docker Compose para desenvolvimento local.

## Arquitetura

A solution será dividida em `Domain`, `Infrastructure` e `Web`, com projetos separados para testes unitários e de integração. Consulte `docs/architecture.md` e `DECISIONS.md`.

## Execução local

Copie `.env.example` para `.env`, substitua a senha de exemplo e execute:

```powershell
docker compose up --build -d
```

A aplicação estará em `http://127.0.0.1:8080`. Consulte `docs/setup.md`.

## Testes

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Privacidade

Selfies serão dados pessoais temporários, sem reconhecimento ou análise facial. A política técnica está em `PRIVACY.md`. O upload ainda não está implementado.

## Limitações atuais

- partidas e sessões ativas são perdidas quando a aplicação reinicia;
- o frontend usa marcadores temporários `P1` e `P2`;
- nenhum upload de imagem;
- `DbContext` configurado, mas ainda sem entidades ou migrations;
- nenhum deploy realizado.

## Roadmap

Consulte `ROADMAP.md`.

**Production deployment: not performed**

Developed by Abc Solutions | Built with quality and care
