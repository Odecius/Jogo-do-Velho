# Testes

Os testes cobrem configuração, domínio, coordenação concorrente, endpoints HTTP, antiforgery, rate limiting e fluxos SignalR com dois clientes independentes. Incluem vitória, empate, placar, consentimento de rematch, nova rodada, reconnect com a mesma sessão e rejeição de terceiro jogador. `/ready` e migrations são validados com PostgreSQL real pelo Compose.

Comandos previstos:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

`/health` verifica o processo web. `/ready` inclui a disponibilidade do PostgreSQL.
Os testes de avatar geram imagens artificiais em memória. Nenhuma selfie ou fixture de pessoa real é usada ou versionada. A suíte cobre formatos, assinaturas, decoder, dimensões, storage, autorização, antiforgery, rate limit e snapshot SignalR.
