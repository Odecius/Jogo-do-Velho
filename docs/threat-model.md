# Threat model

## Escopo e atacantes

Ativos: cookie de sessão do jogador, sala e estado da partida, código público, avatar temporário, metadados PostgreSQL, conexão/grupo SignalR, filesystem de avatars e CPU/memória/disco do servidor.

Atacantes plausíveis: jogador malicioso da própria partida, terceiro que conhece o código público, usuário anônimo, browser modificado, upload malicioso e bot distribuído. Administrador comprometido, host comprometido e ataques de infraestrutura ficam fora do threat model do MVP.

## Ameaças e controles

| Ameaça | Impacto | Controles implementados | Risco residual |
|---|---|---|---|
| Enumeração de códigos | Descoberta de sala | códigos aleatórios de 8 caracteres, rate limit; código não autoriza | tentativas distribuídas continuam possíveis |
| Sequestro/fixação de sessão | Impersonação | token aleatório de 256 bits, HttpOnly, SameSite=Lax, Secure fora de local/teste, TTL e ausência em URL/DOM/log | malware ou host/browser comprometido não é mitigado |
| CSRF | Mutação em nome do jogador | antiforgery em todos os POST HTTP e SameSite | SignalR depende da sessão/cookie e validação server-side |
| Jogada/rematch não autorizado | Corrupção da partida | jogador resolvido server-side; Hub não aceita posição/GameId; lock por sala | rate limit específico de mensagens SignalR não existe |
| Acesso cross-game | Exposição/interferência | token precisa pertencer ao GameId; grupos derivados no servidor; teste Game A/Game B | código público revela somente existência/convite |
| Upload malicioso | RCE, exaustão ou XSS | limite 5 MiB, MIME, magic bytes, decoder, dimensões, reencode WebP, sem metadata, storage privado | processamento concorrente distribuído pode consumir CPU |
| Path traversal/symlink | Leitura/exclusão arbitrária | GUID interno, canonicalização, raiz fixa, rejeição de separadores e reparse points | troca do arquivo após verificação depende da segurança do host |
| Exaustão de storage | Disco cheio | avatar 512×512 normalizado, 10 uploads/15 min/IP, substituição remove anterior, TTL 24 h | botnet pode contornar limite por IP; monitoramento futuro necessário |
| Flood/abuso SignalR | CPU/memória | métodos mínimos, validação controlada, estado por sala e expiração após 24 h | não há quota por conexão; proxy/edge deverá limitar em produção |
| Race condition | Turnos/placar incorretos | `SemaphoreSlim` por sala, liberação em `finally`, testes concorrentes | processo único; não suporta múltiplas instâncias |
| Information disclosure | Segredos ou dados pessoais | erros controlados, headers, avatar autenticado/no-store, logs sem tokens/binários | Development possui diagnóstico e não deve ser exposto publicamente |
| Sessão/sala obsoleta | Crescimento ilimitado | expiração por 24 h de inatividade e varredura a cada 15 min | reinício perde todo estado ativo por design |

## Fronteiras de confiança

O browser e todos os seus valores são não confiáveis. HTTP/SignalR entram na aplicação Web; identidade vem apenas do cookie opaco. `GameSessionManager` coordena autorização e concorrência antes do Domain. PostgreSQL guarda metadados mínimos e o filesystem guarda somente WebP normalizado fora de `wwwroot`.
