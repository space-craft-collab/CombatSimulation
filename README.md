# Orleans Monster Arena

[![CI](https://github.com/space-craft-collab/CombatSimulation/actions/workflows/ci.yml/badge.svg)](https://github.com/space-craft-collab/CombatSimulation/actions/workflows/ci.yml)

Turn-based monster battles powered by
[Microsoft Orleans](https://learn.microsoft.com/dotnet/orleans/).

A public showcase repo demonstrating the actor model and
distributed-systems patterns inside a **modular monolith**
backend, with a **SvelteKit** frontend.

## Status

Phase 3 — Persistence (hot/cold) is done; Phase 4
(the SvelteKit frontend) is next. Live battles keep
their state in Table Storage and survive a restart;
finished battles, the Catalog and the player stats
live in SQL Server through EF Core.
See [`docs/ROADMAP.md`](docs/ROADMAP.md).

To run the host locally, start the infrastructure
first (Docker; the steps are in
[`compose.yaml`](compose.yaml)):

```bash
docker compose up -d
dotnet run --project src/AppHost
```

A battle against the bot is already playable over HTTP:

```bash
# Pick an arena
curl localhost:5000/arenas

# Start a battle; the bot mirrors your team
curl -X POST localhost:5000/arenas/volcano/battles \
  -H 'Content-Type: application/json' \
  -d '{"playerId":"me","monsterSpeciesIds":["ember-fox"]}'

# Strike the bot's monster (slot 1) with yours (slot 0)
curl -X POST localhost:5000/battles/<battleId>/actions \
  -H 'Content-Type: application/json' \
  -d '{"participantId":"me","actorSlot":0,"targetSlot":1}'

# Read the state
curl localhost:5000/battles/<battleId>
```

Or wait for a human opponent:

```bash
# Open a battle and wait; it shows up in the arena's list
curl -X POST localhost:5000/arenas/volcano/battles \
  -H 'Content-Type: application/json' \
  -d '{"playerId":"me","monsterSpeciesIds":["ember-fox"],"opponent":"Human"}'
curl localhost:5000/arenas/volcano/battles

# A second player joins ...
curl -X POST localhost:5000/battles/<battleId>/participants \
  -H 'Content-Type: application/json' \
  -d '{"playerId":"you","monsterSpeciesIds":["lava-crab"]}'

# ... or the host plays the bot instead
curl -X POST localhost:5000/battles/<battleId>/bot
```

Live clients watch the SignalR hub at `/hubs/battles`
instead of polling: invoke `Watch(battleId)`, then
receive a `RoundResolved` delta after every round.

## Architecture

- One binary: ASP.NET Core host + Orleans silo co-hosted
- Modular monolith: `Catalog`, `Battles`, `Players`
- Hot path: Orleans + Azure Table Storage
- Cold path: EF Core + Azure SQL
- Frontend (planned, Phase 4): SvelteKit + Svelte 5 runes
  (separate deploy)

See [`docs/adr/`](docs/adr/) for architecture decisions.

## Tech Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10 |
| Distributed | Microsoft Orleans 10 |
| API | ASP.NET Core Minimal API + SignalR |
| Persistence | EF Core (cold), Azure Table Storage (hot) |
| Logging | NLog |
| Frontend | SvelteKit + TypeScript |
| OpenAPI client | Kiota |
| Tests | xUnit v3 + Testcontainers |
| Hosting | Azure Container Apps + Static Web Apps |
| CI/CD | GitHub Actions |

## Coding Standards

See [`CLAUDE.md`](CLAUDE.md).

## License

[MIT](LICENSE)
