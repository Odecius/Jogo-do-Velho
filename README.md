# Jogo do Velho — Real-Time Selfie Tic-Tac-Toe

A secure two-player tic-tac-toe application built with .NET 8, SignalR and PostgreSQL.

## Project Overview

Jogo do Velho turns a familiar game into a real-time multiplayer experience. One player creates a room and shares an invitation link; both players add temporary avatars, play through synchronized turns and can request a rematch while preserving the session score.

## Features

- Real-time multiplayer with ASP.NET Core SignalR
- Invitation by shareable room link
- Server-authoritative game rules and player identity
- Temporary selfie/avatar upload
- Live board, score and connection-state synchronization
- Reconnect using the same protected player session
- Mutual-consent rematch flow
- Room expiration and avatar cleanup
- Health and readiness endpoints
- Docker deployment with PostgreSQL

## Architecture

The solution separates domain rules, infrastructure and the ASP.NET Core web application:

- **Domain:** board, moves, wins, draws and game state
- **Web:** HTTP endpoints, SignalR hub, sessions and multiplayer coordination
- **Infrastructure:** PostgreSQL metadata and private filesystem avatar storage
- **Tests:** unit and integration coverage, including real SignalR clients

Game commands never trust a client-supplied player position or game identifier. The server resolves identity from an opaque session and coordinates concurrent actions per room.

See [Architecture](docs/architecture.md) and [Threat Model](docs/threat-model.md).

## Avatar Privacy

Avatars are temporary personal data, not identity documents. The application does not perform facial recognition or biometric analysis.

- Accepted files are decoded and normalized to WebP.
- Dimensions, signatures and upload rates are validated.
- Files remain outside the public web root.
- Access requires an authorized game session.
- Responses disable caching.
- Replacement, expiration and cleanup remove obsolete files and metadata.
- Tests use generated images rather than real selfies.

See [PRIVACY.md](PRIVACY.md).

## Security

- Secure, HTTP-only, SameSite session cookies
- Antiforgery validation for HTTP mutations
- Rate limiting for room creation, joining and avatar upload
- Server-side authorization for moves, rematches and avatar access
- Cross-room and manipulated-session tests
- Content Security Policy and defensive HTTP headers
- Non-root container runtime
- PostgreSQL not directly published by the application stack
- Secrets supplied outside source control

Residual limitations are documented rather than hidden, including in-memory room state, single-instance operation and non-distributed rate limiting.

See [SECURITY.md](SECURITY.md) and [docs/threat-model.md](docs/threat-model.md).

## Testing

The automated suites cover domain rules, concurrency, HTTP endpoints, antiforgery, rate limiting, image validation, storage authorization and full SignalR flows with two independent clients. PostgreSQL readiness and migrations are validated with a disposable database environment.

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Browser E2E automation remains future work; the critical journey is exercised through `WebApplicationFactory` and real SignalR test clients.

## Deployment

The application has been deployed and validated in a controlled Linux/Docker environment. Production validation covered two players on separate networks, invitation, avatars, synchronized gameplay, score, refresh/session continuity and cleanup.

Operational addresses, paths, commands and credentials are intentionally excluded from this public overview. See [docs/deployment.md](docs/deployment.md) for the sanitized deployment status and known limitations.

## Current Status

- Multiplayer, avatars, PostgreSQL metadata, security hardening and automated tests are implemented.
- Controlled production deployment and the two-player journey were validated.
- The implementation currently remains in Draft PR #1 pending final integration into `main`.
- Horizontal scaling, persistent game history and browser E2E tests are not implemented.

## Key Lessons Learned

- Real-time clients must not be trusted as the authority for identity or game state.
- Personal images require validation, access control, retention and cleanup as one lifecycle.
- Reconnect and rematch behavior need explicit concurrency rules.
- Monitoring, rollback and production validation are part of completing a feature.
- Known single-instance limitations should be stated before attempting scale-out.
