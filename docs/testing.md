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

O hardening acrescenta Game A/Game B, atacante anônimo, cookie cross-game, índices manipulados, stress concorrente leve, rematch prematuro, expiração de sala, antiforgery incorreto, CSP restrita e payloads com assinaturas enganosas. Browser E2E dedicado permanece futuro; a jornada crítica usa `WebApplicationFactory` e clientes SignalR reais do processo de teste.

## Validação final em produção

Dois jogadores reais, em redes/localizações diferentes, confirmaram convite público, fotos de ambos, sincronização das jogadas e do placar. Um refresh controlado preservou o jogador, a posição, o código da sala, o snapshot e o placar sem criar nova vaga. O cleanup foi executado sobre uma partida artificial exclusiva: upload 200, metadata presente antes, metadata ausente depois e arquivo removido.
