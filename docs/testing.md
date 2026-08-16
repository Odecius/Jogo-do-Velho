# Testes

Na foundation, os testes validam a configuração obrigatória, a inicialização web, `/health`, o frontend mínimo e headers de segurança. `/ready` é validado com PostgreSQL real pelo Compose. As fases seguintes adicionarão cobertura de domínio, multiplayer, autorização e upload.

Comandos previstos:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

`/health` verifica o processo web. `/ready` inclui a disponibilidade do PostgreSQL.
