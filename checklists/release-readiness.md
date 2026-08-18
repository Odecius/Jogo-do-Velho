# Release readiness local

- [x] Restore, build e analyzers: 0 erros/0 warnings.
- [x] Testes unitários e de integração aprovados.
- [x] `dotnet format --verify-no-changes` aprovado.
- [x] `git diff --check` aprovado.
- [x] Dependências vulneráveis auditadas.
- [x] Secret scan e arquivos rastreados revisados.
- [x] Migrations listadas e modelo sem drift.
- [x] Docker config/build/run e usuário não-root validados.
- [x] `/health` e `/ready` sem detalhes sensíveis.
- [x] Threat model, segurança, privacidade e documentação atualizados.
- [x] Git local limpo ao encerrar.
- [x] Produção não acessada; nenhum deploy, remote ou push.
- [ ] Revisão humana antes de publicação no GitHub.
- [ ] Validação em dispositivos/browsers reais e webcam física.
- [ ] Configuração de produção, HTTPS, observabilidade e resposta a incidentes.
