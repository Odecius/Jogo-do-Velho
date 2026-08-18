# Deployment

**Production deployment: active**

Endpoint público: `https://jogo.abc-solutuions.com`.

## Topologia

- stack Compose exclusiva da aplicação;
- aplicação sem porta publicada no host, conectada às redes internas de proxy e banco;
- PostgreSQL compartilhado no servidor, mas com database e role exclusivos da aplicação;
- Nginx Proxy Manager encaminha para o container da aplicação;
- Cloudflare Tunnel publica o hostname e termina HTTPS no edge.

## Operação

O arquivo de ambiente operacional deve permanecer fora do Git e com acesso restrito. O deploy usa a branch aprovada e não depende de merge na `main`. Antes de subir a aplicação, execute a migration explícita com o mesmo Compose de produção; depois, suba somente o serviço da aplicação e aguarde o health check.

Valide `/health`, `/ready`, assets, criação de partida, SignalR, cookies, upload/leitura privada do avatar e headers de segurança pela URL pública. Não use `docker compose down`, prune global ou restart do daemon em manutenção normal.

## Rollback

1. Registre o commit em execução e faça checkout do commit anterior aprovado no diretório da aplicação.
2. Reconstrua e recrie somente o serviço `app` da stack `jogo-do-velho`.
3. Aguarde o health check e repita os smoke tests públicos.
4. Se o código anterior for incompatível com a migration aplicada, interrompa o rollback e restaure o backup/schema por procedimento aprovado; não reverta migrations destrutivamente de forma automática.
5. Para retirada completa, remova apenas a rota pública e o proxy host desta aplicação, depois pare somente a stack dela. Banco e volume devem ser preservados até aprovação explícita de exclusão.

## Riscos residuais

Partidas, placar e sessões ativas estão em memória e são perdidos em restart. Chaves de Data Protection são efêmeras. Há uma única instância e o rate limiting não é distribuído.

## Validação final

A Fase J confirmou em produção dois jogadores reais em redes/localizações distintas, convite, fotos, partida e placar sincronizados. Refresh preservou a sessão e o cleanup real foi validado com avatar artificial isolado. As limitações de estado em memória, instância única, ausência de escala horizontal, rate limiting não distribuído e ausência de histórico persistente permanecem.
