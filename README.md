# Orleans Monster Arena

Turn-based monster battles powered by
[Microsoft Orleans](https://learn.microsoft.com/dotnet/orleans/).

A public showcase repo demonstrating the actor model and
distributed-systems patterns inside a **modular monolith**
backend, with a **SvelteKit** frontend.

## Status

Phase 0 — Foundations & ADRs. Not yet runnable.

## Architecture (planned)

- One binary: ASP.NET Core host + Orleans silo co-hosted
- Modular monolith: `Catalog`, `Battles`, `Players`
- Hot path: Orleans + Azure Table Storage
- Cold path: EF Core + Azure SQL
- Frontend: SvelteKit + Svelte 5 runes (separate deploy)

See [`docs/adr/`](docs/adr/) for architecture decisions
(coming in Phase 0).

## Tech Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10 |
| Distributed | Microsoft Orleans 9 |
| API | ASP.NET Core Minimal API + SignalR |
| Persistence | EF Core (cold), Azure Table Storage (hot) |
| Logging | NLog |
| Frontend | SvelteKit + TypeScript |
| OpenAPI client | Kiota |
| Tests | xUnit + Orleans TestingHost + Testcontainers |
| Hosting | Azure Container Apps + Static Web Apps |
| CI/CD | GitHub Actions |

## Coding Standards

See [`CLAUDE.md`](CLAUDE.md).

## License

[MIT](LICENSE)
