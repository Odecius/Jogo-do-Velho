# Configuração local

## Pré-requisitos

- .NET SDK 8;
- Docker com Docker Compose.

## Docker Compose

1. Copie `.env.example` para `.env`.
2. Substitua a senha de exemplo por uma senha exclusiva de desenvolvimento local.
3. Execute `docker compose up --build -d`.
4. Acesse `http://127.0.0.1:8080`.
5. Encerre com `docker compose down`. O volume PostgreSQL é preservado.

O PostgreSQL permanece somente na rede interna do Compose e não publica porta no host.

## Execução sem Docker

Defina `ConnectionStrings__Postgres` por variável de ambiente ou User Secrets e execute:

```powershell
dotnet run --project src/Abc.JogoDoVelho.Web
```

Nenhuma credencial real deve ser adicionada à documentação ou ao Git.
