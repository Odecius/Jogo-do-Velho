# Testes

Os testes cobrem configuração, domínio, coordenação concorrente, endpoints HTTP, antiforgery, rate limiting e um fluxo SignalR real com dois clientes independentes. `/ready` e migrations são validados com PostgreSQL real pelo Compose. Upload continua fora do escopo.

Comandos previstos:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

`/health` verifica o processo web. `/ready` inclui a disponibilidade do PostgreSQL.
