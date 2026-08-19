# Revisão de segurança — Fase G

- [x] Entradas e regras validadas no servidor.
- [x] Isolamento Game A/Game B e atacante anônimo testados.
- [x] Todos os POST HTTP exigem antiforgery; token ausente/incorreto testado.
- [x] Rate limits de criação, entrada e upload revisados; 429 testado para criação/upload.
- [x] Sessões revisadas: entropia, cookie, TTL, refresh e ausência em URL/DOM/log.
- [x] Hub possui apenas `JoinGame`, `PlaceMove` e `RequestRematch`; identidade/grupo são server-side.
- [x] Upload, decoder, dimensões, metadata stripping e storage privado testados.
- [x] Traversal, caminhos absolutos, separadores e arquivo ausente avaliados.
- [x] CSP sem `unsafe-inline`, `unsafe-eval` ou wildcard; demais headers testados.
- [x] Dependências diretas/transitivas auditadas contra vulnerabilidades conhecidas.
- [x] Segredos e arquivos rastreados revisados.
- [x] Docker não-root, filesystem read-only, capabilities removidas e volumes mínimos.
- [x] Retenção de avatar e expiração de salas revisadas.
- [x] Privacidade e licenças comparadas com a implementação.
- [ ] Teste manual com dois perfis de browser isolados — indisponível neste ambiente.
- [ ] Webcam física — não testada.
- [ ] Controles de edge/WAF/limites distribuídos — dependem do futuro deploy.
