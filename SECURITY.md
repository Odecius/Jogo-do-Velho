# Segurança

## Baseline

- Segredos devem vir de User Secrets, variáveis de ambiente ou `.env` ignorado.
- `.env`, uploads, logs, `bin/` e `obj/` não podem ser versionados.
- O servidor será autoridade sobre partidas e jogadas.
- Tokens privados não poderão aparecer em URLs ou logs.
- Endpoints sensíveis terão validação, autorização e rate limiting nas fases correspondentes.
- HTTPS será obrigatório em produção.
- Criação e entrada em partidas exigem antiforgery token via header.
- Cookies de sessão são opacos, `HttpOnly`, `SameSite=Lax` e `Secure` fora de ambientes locais/teste.
- Códigos públicos não concedem autorização e endpoints de criação/entrada possuem rate limiting.
- SignalR resolve a posição exclusivamente pela sessão server-side.

## Relato de vulnerabilidades

Não publique credenciais ou dados pessoais em issues. Comunique o responsável pelo repositório por canal privado.

## Produção

Nenhum deploy foi realizado. Configurações de produção não fazem parte da foundation local.
