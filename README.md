# Jogo do Velho — ABC Solutions

MVP experimental de um jogo da velha multiplayer no qual duas pessoas usarão selfies como símbolos do tabuleiro.

## Estado atual

Fase I — MVP publicado em produção com deploy isolado, proxy reverso, HTTPS no edge e validação pública controlada.

## Funcionalidades

- criação e entrada por convite;
- multiplayer server-authoritative com SignalR;
- avatar temporário por câmera ou arquivo;
- vitória, empate, placar da sessão e rematch consentido;
- reconnect/refresh com sessão temporária;
- expiração automática de avatars e salas inativas.

## Stack técnica

- .NET 8 e ASP.NET Core 8;
- SignalR para atualização multiplayer em tempo real;
- HTML, CSS e JavaScript vanilla;
- PostgreSQL, Entity Framework Core e Npgsql;
- xUnit para testes;
- Docker Compose para desenvolvimento local.

## Arquitetura

A solution será dividida em `Domain`, `Infrastructure` e `Web`, com projetos separados para testes unitários e de integração. Consulte `docs/architecture.md` e `DECISIONS.md`.

## Requisitos

- .NET SDK 8 para build/testes locais;
- Docker Desktop/Engine com Compose para a stack completa;
- browser moderno; HTTPS será obrigatório fora de localhost para câmera e cookie Secure.

## Execução local

Copie `.env.example` para `.env`, substitua a senha de exemplo e execute:

```powershell
docker compose up --build -d
```

A aplicação estará em `http://127.0.0.1:8080`. Consulte `docs/setup.md`.

## Configuração

`.env.example` contém somente placeholders. Defina `POSTGRES_PASSWORD` localmente; a connection string é montada por variáveis no Compose. `AvatarStorage` limita upload/dimensões e `GameSessions` define TTL/cleanup. Configuração crítica inválida interrompe o startup sem imprimir credenciais.

## Docker

A imagem usa build multi-stage e runtime ASP.NET 8 como usuário não-root. O serviço possui filesystem read-only, `/tmp` limitado, capabilities removidas e volume gravável exclusivo para avatars. PostgreSQL não publica porta no host.

## Testes

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Testes usam imagens artificiais e dois clientes SignalR independentes. Consulte `docs/testing.md` e os checklists de release/segurança.

## Segurança

Códigos públicos localizam salas, mas não autorizam ações. Sessões opacas, antiforgery, rate limiting, locks por sala, upload normalizado, storage privado, CSP e expiração limitam as ameaças conhecidas. Consulte `SECURITY.md` e `docs/threat-model.md`.

## Privacidade

Selfies são avatares pessoais temporários, sem reconhecimento ou análise facial. JPEG, PNG e WebP de até 5 MiB e 4096 × 4096 são normalizados para WebP 512 × 512 sem metadados. Consulte `PRIVACY.md`.

## Limitações atuais

- partidas e sessões ativas são perdidas quando a aplicação reinicia;
- avatares dependem do filesystem local e não suportam múltiplas instâncias;
- placar e rodadas de rematch existem somente em memória durante a sessão;
- uma única instância da aplicação; sem escala horizontal ou limite distribuído de SignalR;
- salas expiram após 24 horas de inatividade;
- a sessão antiforgery não persiste entre reinícios; partidas ativas também são encerradas;
- validação final em dois dispositivos físicos e webcam real permanece para a Fase J.

## Roadmap

Consulte `ROADMAP.md`.

**Production deployment: active at https://jogo.abc-solutuions.com**

Developed by Abc Solutions | Built with quality and care
