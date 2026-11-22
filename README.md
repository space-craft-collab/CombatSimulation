# Orleans Monster Arena

[![CI](https://github.com/space-craft-collab/CombatSimulation/actions/workflows/ci.yml/badge.svg)](https://github.com/space-craft-collab/CombatSimulation/actions/workflows/ci.yml)

Turn-based monster battles powered by
[Microsoft Orleans](https://learn.microsoft.com/dotnet/orleans/).

A public showcase repo demonstrating the actor model and
distributed-systems patterns inside a **modular monolith**
backend, with a **SvelteKit** frontend.

## Status

Phase 4 — Frontend (in progress). Phase 4.5 — Combat
depth is done: six species in three elements, three
abilities each with cooldowns, status effects, and a
bot that fields a team of its own. A simulator plays
the bot against itself and writes
[`docs/balance.md`](docs/balance.md); tests hold the
numbers inside their guard rails
([ADR-0012](docs/adr/0012-combat-rules.md)). Live
battles keep their state in Table Storage and survive
a restart; finished battles, the Catalog and the
player stats live in SQL Server through EF Core. See
[`docs/ROADMAP.md`](docs/ROADMAP.md).

The short way to a round in the browser is one script
(Docker and the .NET SDK are all it needs); it starts
the infrastructure, the host and the frontend, and
prints the address:

```bash
scripts/play-start.sh
scripts/play-stop.sh    # --wipe also deletes the data
```

Step by step: to run the host, start the infrastructure
first (Docker; the steps are in
[`compose.yaml`](compose.yaml)):

```bash
docker compose up -d
dotnet run --project src/AppHost
```

The frontend runs in a container of its own (no Node
on the host), on <http://localhost:5173>:

```bash
cd frontend
docker compose run --rm frontend npm ci
docker compose up
```

There a visitor picks a nickname, an arena and a team
of up to three monsters and fights the bot, round by
round, live over SignalR. Waiting for a human
opponent and the player dashboard follow.

A battle against the bot is already playable over HTTP:

```bash
# Pick an arena
curl localhost:5000/arenas

# The species, each with its element and abilities
curl localhost:5000/species

# Start a battle; the bot fields a monster of its own
curl -X POST localhost:5000/arenas/volcano/battles \
  -H 'Content-Type: application/json' \
  -d '{"playerId":"me","monsterSpeciesIds":["ember-fox"]}'

# Your monster (slot 0) uses Flame Dash on the bot's (slot 1);
# without an abilityId it is the basic strike
curl -X POST localhost:5000/battles/<battleId>/actions \
  -H 'Content-Type: application/json' \
  -d '{"participantId":"me","actorSlot":0,"targetSlot":1,"abilityId":"flame-dash"}'

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
- Frontend (Phase 4, in progress): SvelteKit + Svelte 5 runes,
  a Node server of its own (`adapter-node`); the browser
  calls the API and the SignalR hub directly

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
| Hosting | Azure Container Apps |
| CI/CD | GitHub Actions |

## Coding Standards

See [`CLAUDE.md`](CLAUDE.md).

## Artwork

The arena pictures are AI-generated (Google Gemini,
"Nano Banana"). The monsters are hand-drawn SVG.

## License

[MIT](LICENSE)
