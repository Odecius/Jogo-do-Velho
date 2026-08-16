# Arquitetura

## Projetos

- `Domain`: regras e modelos sem dependência de ASP.NET Core ou EF Core.
- `Infrastructure`: EF Core, PostgreSQL e armazenamento temporário futuro.
- `Web`: HTTP, configuração, health checks, frontend e SignalR futuro.
- `Tests`: testes unitários.
- `IntegrationTests`: testes da aplicação em execução.

Dependências permitidas: `Infrastructure` referencia `Domain`; `Web` referencia ambos. `Domain` não referencia outros projetos da solution.

