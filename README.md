# Jogo do Velho — Real-Time Selfie Tic-Tac-Toe

This repository is preparing a secure two-player tic-tac-toe application built with .NET 8, SignalR and PostgreSQL.

## Repository Status

The `main` branch currently contains project documentation only. The application code, automated tests and deployment assets described below are implemented and validated in [Draft PR #1](https://github.com/Odecius/Jogo-do-Velho/pull/1), pending review and merge into `main`.

Cloning `main` does not currently provide a runnable application. To inspect the implementation before it is merged, review the Draft PR and its branch without treating it as a released baseline.

## Scope Implemented in Draft PR #1

- Real-time multiplayer with ASP.NET Core SignalR
- Invitation by shareable room link
- Server-authoritative game rules and player identity
- Temporary selfie/avatar upload with validation and cleanup
- Live board, score and connection-state synchronization
- Reconnect and mutual-consent rematch flows
- Health and readiness endpoints
- PostgreSQL metadata persistence
- Docker deployment assets
- Unit and integration tests, including real SignalR clients
- Security hardening for sessions, HTTP mutations, uploads and containers

These capabilities are not yet part of `main`.

## Proposed Architecture

Draft PR #1 separates domain rules, infrastructure and the ASP.NET Core web application:

- **Domain:** board, moves, wins, draws and game state
- **Web:** HTTP endpoints, SignalR hub, sessions and multiplayer coordination
- **Infrastructure:** PostgreSQL metadata and private filesystem avatar storage
- **Tests:** domain, HTTP, security and real-time integration coverage

The proposed design resolves player identity server-side and does not trust client-supplied player positions or game identifiers.

See [Architecture](docs/architecture.md) and the [Threat Model proposed in Draft PR #1](https://github.com/Odecius/Jogo-do-Velho/blob/agent/jogo-do-velho-foundation/docs/threat-model.md). Some linked documents describe the pending PR implementation rather than code already present on `main`.

## Privacy and Security Design

Avatars are treated as temporary personal data, not identity documents. The pending implementation validates and normalizes uploads, stores files outside the public web root, authorizes access by game session, disables caching and removes obsolete data.

The security design also covers secure cookies, antiforgery validation, rate limiting, server-side authorization, defensive HTTP headers, a non-root container and externally supplied secrets.

See [PRIVACY.md](PRIVACY.md), [SECURITY.md](SECURITY.md) and the [pending Threat Model](https://github.com/Odecius/Jogo-do-Velho/blob/agent/jogo-do-velho-foundation/docs/threat-model.md).

## Validation Status

The Draft PR contains automated domain and integration suites and records a controlled Linux/Docker validation of the two-player journey. Those results support review of the PR; they are not evidence that `main` currently builds or runs the application.

Before merging, the PR should be evaluated for build, tests, formatting, security checks, mergeability, divergence and unresolved review comments.

## Known Limitations of the Pending Implementation

- In-memory room state and single-instance operation
- Non-distributed rate limiting
- No persistent game history
- No browser E2E automation

## Merge Policy

Draft PR #1 must not be merged automatically. After its checks and review are complete, merge requires an explicit decision. Until then, `main` remains the documentation baseline and the Draft PR remains the implementation candidate.
