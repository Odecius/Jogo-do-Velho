# Arquitetura

## Projetos

- `Domain`: regras e modelos sem dependência de ASP.NET Core ou EF Core.
- `Infrastructure`: EF Core, PostgreSQL e armazenamento temporário futuro.
- `Web`: HTTP, configuração, health checks, frontend e SignalR futuro.
- `Tests`: testes unitários.
- `IntegrationTests`: testes da aplicação em execução.

Dependências permitidas: `Infrastructure` referencia `Domain`; `Web` referencia ambos. `Domain` não referencia outros projetos da solution.

## Multiplayer

O fluxo é `browser → GameHub → GameSessionManager → Domain.Game`. O Hub não contém regras do jogo. O coordenador mantém salas e conexões em memória, usa um lock por partida e produz snapshots personalizados para grupos SignalR internos de Player1 e Player2.

PostgreSQL persiste somente metadados de jogos e jogadores. Partidas ativas e sessões são deliberadamente voláteis.
## Avatares temporários

`AvatarImageProcessor` valida e normaliza imagens; `FileSystemAvatarStorage` restringe operações à raiz configurada; `EfAvatarMetadataStore` persiste somente referência, MIME e timestamps. O endpoint nunca publica o diretório físico. `AvatarCleanupService` remove itens expirados e limpa metadados. O estado ativo continua em memória e uma partida só fica jogável com dois jogadores e dois avatares.
