# Segurança

## Baseline

- Segredos devem vir de User Secrets, variáveis de ambiente ou `.env` ignorado.
- `.env`, uploads, logs, `bin/` e `obj/` não podem ser versionados.
- O servidor será autoridade sobre partidas e jogadas.
- Tokens privados não poderão aparecer em URLs ou logs.
- Endpoints sensíveis terão validação, autorização e rate limiting nas fases correspondentes.
- HTTPS será obrigatório em produção.

## Relato de vulnerabilidades

Não publique credenciais ou dados pessoais em issues. Comunique o responsável pelo repositório por canal privado.

## Produção

Nenhum deploy foi realizado. Configurações de produção não fazem parte da foundation local.

