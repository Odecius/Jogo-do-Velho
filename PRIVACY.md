# Privacidade

As selfies serão usadas exclusivamente como avatares visuais durante partidas. Não haverá reconhecimento facial, identificação biométrica, comparação de rostos ou inferência de características pessoais.

## Tratamento implementado

- finalidade exclusiva: exibir o avatar na partida;
- JPEG, PNG e WebP são decodificados e normalizados para WebP 512 × 512;
- EXIF, GPS, modelo do aparelho, data, orientação, XMP, IPTC e ICC não são preservados;
- armazenamento temporário fora de `wwwroot`, com nomes aleatórios gerados pelo servidor;
- retenção máxima de 24 horas a partir de cada upload;
- cleanup automático a cada 15 minutos; arquivo ausente é tolerado e metadados expirados são limpos;
- acesso restrito aos dois jogadores autenticados pela sessão daquela partida;
- não há publicação pública, backup permanente, reconhecimento facial ou qualquer inferência sobre a imagem;
- IP e fingerprint não são persistidos como metadados do avatar;
- logs sem imagem, nome original, token ou conteúdo pessoal.

O nome original nunca é armazenado. O volume temporário de avatares não deve integrar backups de longo prazo.

Rematch reutiliza os mesmos avatares enquanto ainda estiverem válidos e não prolonga automaticamente sua expiração. Placar e consentimentos existem somente na memória da sala e não criam histórico permanente.

Uma sala inativa expira em até 24 horas, removendo suas sessões em memória e antecipando a remoção dos avatars associados. Não há analytics, reconhecimento facial, persistência de IP ou publicação pública das imagens.

O fluxo com fotos reais foi validado em produção por dois participantes, sem registrar nomes, imagens ou outros dados pessoais no repositório ou na documentação. O cleanup também foi confirmado separadamente com imagem artificial: após expiração controlada, o arquivo foi removido e a metadata foi limpa.
