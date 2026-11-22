# Changelog

All notable changes to this project will be documented
in this file.

The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Entries are grouped by roadmap phase
([`docs/ROADMAP.md`](docs/ROADMAP.md)). The reasons behind the
design are in the ADRs ([`docs/adr/`](docs/adr/)), not here.
Phases 0–3 were condensed on 2026-11-21; the full entries are
in [this file's history][full-changelog].

## [Unreleased]

### Phase 4.5 — Combat depth (in progress)

#### Added
- ROADMAP Phase 4.5 checklist: elements, abilities with
  cooldowns, status effects, more species, a bot with a team
  of its own and a balance simulator, before the rest of
  Phase 4.

- Balance simulator in the test project (`Balance/`): plays
  the bot against itself on `RoundResolver`, without a silo,
  every one-on-one and every team of three against every
  other, 20 times per seat order. Both sides mix 20 % random
  legal moves into the bot rule; the random source is seeded
  per battle, so a rerun gives the same numbers.
- `docs/balance.md`, the simulator's report: win rates per
  species and per pair, battle lengths, ability use.
  `BalanceReportTests` fails when the rules or the Catalog
  seed no longer give the committed report
  (`UPDATE_BALANCE=1` refreshes it).
- The first report records the starting point: a strict
  ladder one on one (Moss Golem 100 %, Frost Owl 0 %), Lava
  Crab against Moss Golem at 60 rounds, and teams of three
  at 79 rounds on average, with 40 of 240 battles still
  undecided after 100 rounds.

- Elements: `Element` (Fire, Frost, Nature, Neutral) in
  `Catalog.Contracts`, a column on `catalog.Species`
  (migration `AddElements`), in `GET /species` and on every
  combatant of a battle. `ElementChart` holds the triangle
  Fire > Nature > Frost > Fire.
- [ADR-0012](docs/adr/0012-combat-rules.md), `Proposed`: the
  combat rules of Phase 4.5.
- `GrainStateSerializationTests`: the battle's stored state
  goes through the Orleans serializer without a silo.

- Test helpers that play battles without knowing the numbers
  (`GrainBattleDriver`, `HttpBattleDriver`, `BattleRounds`,
  `BattleAssert`).

#### Changed
- The rules of a round left `LiveBattleGrain` for
  `RoundResolver` in `Battles/Domain`: fighters and actions
  in, events and the fighters afterwards out, with no grain
  or storage in reach. The grain applies each hit to its
  monster grain as before. Same rules, now unit-tested
  without a silo.
- Damage rule: defense scales a hit down by
  `10 / (10 + defense)` instead of being subtracted, the
  ability's power and the element scale it, in whole-number
  arithmetic. Lava Crab against Moss Golem went from 60
  rounds to 15, teams of three from 79 on average to 28.
- The battle tests no longer pin hit points, round counts or
  the bot's moves: they play to a condition, use three
  monsters against one where a winner must be known, and
  assert relations ("took damage", "fell"). The Catalog tests
  compare against the seed.

### Phase 4 — Frontend (paused)

#### Added
- ROADMAP Phase 4 checklist: the container-only toolchain
  and the adapter spike, the backend pieces the frontend
  needs (OpenAPI document, `GET /species`, Kiota client),
  the solo path against the bot first, then waiting for a
  human and the player dashboard, frontend tests and CI.
- `GET /species`: every species with its base stats, for the
  team picker.
- OpenAPI document at `/openapi/v1.json`, committed as
  `docs/api/openapi.json`; `OpenApiDocumentTests` fails when
  the two differ. Operations carry a tag per module and
  their 400/409 problem responses.
- CORS for the frontend: the origins under
  `Cors:AllowedOrigins` may call the API and the hub, without
  credentials.
- `frontend/`: SvelteKit 3 with Svelte 5 runes, TypeScript,
  Tailwind CSS and `adapter-node`, scaffolded and run in
  containers only (Node 24 LTS image, `frontend/compose.yaml`
  for development, a multi-stage `Dockerfile` for
  production).
- Kiota TypeScript client under `frontend/src/lib/api`,
  generated from the committed document by
  `scripts/generate-api-client.sh` (Kiota is a pinned
  `dotnet` tool). `frontend/src/lib/arena` wraps it: client
  factory, strict Catalog types, and the hand-written types
  of the SignalR hub.
- The solo path in the browser: a nickname on the first visit
  (kept in `localStorage`), the arena browser, a team picker
  for one to three monsters, and the live battle against the
  bot. The battle view applies each round's delta from the
  hub, shows the turn deadline as a countdown, a battle log
  and the result; a reload picks a battle up again, a
  finished one from the cold store.
- Monsters drawn as inline SVG; the layout is built for a
  phone-width screen first.
- Frontend tests: Vitest for the battle logic and the nickname
  rules, and a Playwright smoke test in a container
  (`docker compose run --rm e2e`) that plays the solo path in
  Chromium, from the nickname to the result.
- `frontend/scripts/check-malware.mjs`: checks every package
  of the lockfile against the malware advisories of the
  GitHub Advisory Database.
- CI: a `frontend` job (malware check, install, lint, type
  check, Vitest, build, all in the container image), and a
  step in `build-test` that fails when the checked-in Kiota
  client is stale. Dependabot watches the frontend's npm
  packages and its `Dockerfile`.
- `CONTRIBUTING.md`: the API contract and client, the
  frontend, and the rules for npm dependencies.

#### Changed
- [ADR-0004](docs/adr/0004-sveltekit-frontend.md) records
  what was settled at the phase start: toolchain in
  containers only, Tailwind CSS with inline-SVG monsters, a
  nickname in `localStorage` as the player id until Phase 6,
  Vitest plus one Playwright smoke test.
- ADR-0004 locks the adapter after the spike:
  **`adapter-node`**. The browser calls the API and the hub
  directly; the SvelteKit server reaches the API under an
  internal URL while it renders. The frontend becomes a
  second Container App instead of a static site. The spike's
  findings on Kiota with SvelteKit are in the ADR.
- Kiota is the only client generator: the
  `@hey-api/openapi-ts` fallback is dropped, since it is a
  community project and the generator should come from
  Microsoft. Kiota's TypeScript support is still in preview.
- The HTTP API reads numbers from JSON numbers only, no
  longer from strings as well; the OpenAPI document typed
  every integer as "integer or string" otherwise.

#### Fixed
- `dotnet run --project src/AppHost` started the host as
  Production, so `appsettings.Development.json` and the
  startup migrations did not apply. A `launchSettings.json`
  now selects Development and `http://localhost:5000`.

#### Decided (process)
- Every npm package is pinned to an exact version and
  checked for known malware before it is installed. A
  release must be a week old, and no package runs an
  install script.

### Phase 3 — Persistence (hot/cold) ✅

Closed 2026-11-21.

#### Added
- `compose.yaml` with Azurite (Table Storage) and SQL Server
  for local runs, with example settings files to copy from.
- Grain state and reminders on Azure Table Storage when
  `ConnectionStrings:TableStorage` is set; in-memory stays the
  default ([ADR-0002](docs/adr/0002-orleans-cohosted.md)). A
  live battle keeps its state, round and turn deadline across
  a host restart.
- Stored grain format frozen (`[Alias]`, `[GrainType]`,
  explicit enum values); rules in `CONTRIBUTING.md`, guarded
  by `StoredFormatTests`.
- EF Core base in `Shared.Infrastructure`: one `DbContext` and
  one SQL schema per module, each with its own migrations
  history; migrations are applied on startup in Development.
- Catalog species and arenas in SQL, seeded through
  migrations; `GET /arenas`.
- Player stats in SQL: `IPlayerStatsService` in
  `Players.Contracts`, counters raised in SQL so concurrent
  battles lose no count; `GET /players/{id}`.
- Hot→cold bridge ([ADR-0003](docs/adr/0003-no-outbox.md)): a
  completed battle is stored as a snapshot in SQL, a decided
  one is reported to the player stats, then the battle and
  monster grains clear their hot state. A failed write is
  logged and keeps the hot state.
- `GET /battles/{id}` and the hub's `Watch` fall back to the
  cold store once the hot state is gone.
- `GET /players/{id}/battles`: a player's completed battles,
  newest first, at most 50. The slice lives in Battles, which
  owns the data.
- `GET /health/ready`: one check per module database and one
  for Table Storage; 503 when a store is unreachable.
- CI: an `integration` job for the `Category=Integration`
  tests, after `build-test`; both are required checks on
  `main`. Coverage of the fast suite is measured with
  `dotnet-coverage` (96 % of the lines). Dependabot watches
  the images in `compose.yaml`.
- Tests on Testcontainers SQL Server and Azurite: the host
  restart, the bridge, the accepted gap in ADR-0003, store
  outages, and the `ExactIds` migrations on databases that
  already hold rows, up and back down.
- Architecture tests for the module boundaries: no module
  references another module's implementation, and the
  `.Contracts` assemblies reference no Orleans, EF Core or
  ASP.NET Core.

#### Changed
- SQL Server is required to run the host and the tests
  ([ADR-0011](docs/adr/0011-sql-server-required.md)): one
  Testcontainers SQL Server per test run, a database per host.
- Ids are taken exactly as written, in SQL as they already
  were as grain keys: id columns use the binary collation
  `Latin1_General_100_BIN2`, so `Volcano` is not the arena
  `volcano` and `Alice` and `alice` are two players. Player
  ids are capped at 64 characters and take no whitespace at
  either end.
- Battles against the bot count for the human side; bots and
  abandoned battles count for nobody.
- Stricter answers: 404 for an arena the Catalog does not
  know, 400 for an ability other than `strike`, 409 for an
  action on, or a join of, a battle already handed over.
- `GET /health` is a pure liveness probe and runs no check.
- Container images are pinned in `compose.yaml`, the one place
  they are named; the test fixtures read them from there.
  Both CI jobs stop after ten minutes.
- Orleans packages aligned on 10.4.2; Testcontainers 4.17.0.

#### Removed
- `coverlet.collector`: it only attaches under `dotnet test`,
  which the xUnit v3 `dotnet run` never starts.
- The unused `Microsoft.Orleans.TestingHost` pin; the tests
  boot the real `AppHost`.
- Three more pins nothing references or plans to:
  `Microsoft.Orleans.Client`,
  `Microsoft.EntityFrameworkCore.Tools`, `NLog.Web.AspNetCore`.

#### Fixed
- Table Storage grain storage sets `DeleteStateOnClear`;
  Orleans' default only blanks a cleared row, so a completed
  battle's hot rows would have stayed forever.
- A bot-only battle's final round never stored its result.
- After a host restart, a turn deadline could come a minute
  late: the reminder's start time is stored in whole
  milliseconds and could tick just before the deadline.
- A start that failed on a later participant's unknown
  species left the earlier monsters in the battle.
- With SQL Server's default collation, ids that differed only
  in case or trailing spaces were treated as one (see "Ids
  are taken exactly" above).
- Diagrams, `CLAUDE.md` and the README brought in line with
  the code.

#### Decided
- ADR **0011** (SQL Server required) added as `Accepted`.
- ADRs **0003**, **0006**, **0007** and **0010** promoted
  `Proposed` → `Accepted`; **0004** stays `Proposed` until the
  frontend exists.
- ADR convention: a factual correction that leaves the
  decision unchanged goes into a `## Corrections` section of
  an `Accepted` ADR
  ([conventions](docs/adr/README.md#conventions)).
- The migration bundle moves to Phase 5, where the first
  deployment needs it.

### Phase 2 — Orleans embedded ✅

Closed 2026-10-26.

#### Added
- Co-hosted Orleans silo in the `AppHost`
  ([ADR-0002](docs/adr/0002-orleans-cohosted.md)), with
  localhost clustering and in-memory storage and reminders.
- Grain interfaces in `Battles.Grains.Abstractions`:
  `IArenaGrain` (one per arena), `ILiveBattleGrain` (Guid
  battle id) and `IMonsterInstanceGrain`
  (`"{BattleId}:{Slot}"`). Their DTOs are
  `[GenerateSerializer]` records; collections use
  `ImmutableArray<T>`, a concrete type Orleans has a codec
  for.
- Grain implementations in `Battles/Grains/`: a battle runs
  `Created → InProgress → Completed`. A round resolves once
  every side still standing has acted, fastest monster first;
  the last side standing wins. Damage is `attack - defense`,
  at least 1.
- Rule-based bot ([ADR-0007](docs/adr/0007-turn-based.md)):
  its strongest monster attacks the weakest standing
  opponent. Bot turns run inline, so human vs bot needs no
  timer.
- Round loop: an Orleans reminder enforces the human turn
  deadline (60 s); a battle in which no human acted ends
  abandoned. A grain timer plays battles with only bots left
  (500 ms per round). Pacing comes from `Battles:Timing`.
- Battles feature slices: `POST /arenas/{arenaId}/battles`
  (the bot mirrors the visitor's team),
  `POST /battles/{id}/actions` and `GET /battles/{id}`.
- SignalR round deltas: `BattleHub` at `/hubs/battles`;
  `Watch(battleId)` returns the current state, and
  `RoundResolved` follows every round once it is stored.
- Wait for a human: status `WaitingForPlayers`,
  `"opponent": "Human"` on the start request,
  `GET /arenas/{arenaId}/battles`,
  `POST /battles/{id}/participants` and
  `POST /battles/{id}/bot`. A join deadline (5 minutes)
  abandons battles nobody joins.
- `ICatalogQueryService` in `Catalog.Contracts`, the first
  cross-module contract
  ([ADR-0005](docs/adr/0005-inter-module-services.md)).
- Tests play full battles through the silo, over HTTP and
  over SignalR, on one booted host shared by a collection.

#### Changed
- Orleans 9.0.0 → 10.3.1, which ships a native `net10.0`
  build and requires `Microsoft.Extensions.*`, ASP.NET Core
  and EF Core 10.0.12. Also bumped: OpenTelemetry 1.19.1 and
  `xunit.v3` 4.0.1.
- Tests run via `dotnet run --project
  tests/OrleansMonsterArena.Tests` instead of `dotnet test`:
  xUnit v3 test projects are executables.
- The architecture guard also forbids `Domain` → `Grains`.
- ADR-0007 defines arenas (Catalog templates that host any
  number of battles) and makes the showcase playable solo:
  a rule-based bot opponent moves into Phase 2, and Phase 8
  only swaps its strategy.

#### Decided
- ADRs **0001** (modular monolith) and **0002** (co-hosted
  silo) promoted `Proposed` → `Accepted`.

### Phase 1 — Walking skeleton ✅

Closed 2026-09-22.

#### Added
- `OrleansMonsterArena.slnx` with the full project graph (11
  projects): three module projects with
  `Domain/Features/Infrastructure` folders, per-module
  Contracts, `Battles.Grains.Abstractions`, `Shared.Kernel`,
  `Shared.Infrastructure`, `AppHost` and the test project.
- `Shared.Infrastructure`: NLog wiring
  ([ADR-0006](docs/adr/0006-nlog-logging.md)) and
  OpenTelemetry tracing and metrics (OTLP always, console
  exporter in Development).
- `AppHost` with `GET /health` and `nlog.config`.
- xUnit v3 tests: a `/health` smoke test and the NetArchTest
  guard for the layering of
  [ADR-0009](docs/adr/0009-module-internal-structure.md).
- CI workflow (restore, build, test on push and PR to
  `main`) with least-privilege permissions; SDK pinned in
  `global.json`; Dependabot for NuGet and GitHub Actions.
- `SECURITY.md` and `CONTRIBUTING.md`.
- [ADR-0010](docs/adr/0010-delegate-test-seams.md): function
  delegates as test seams instead of test-only interfaces.

#### Changed
- Test stack moved to xUnit v3.
- OpenTelemetry 1.10.0 → 1.17.0 (known vulnerabilities).
- ROADMAP: new Phase 6 (Auth & accounts); the OTEL wiring is
  pulled into Phase 1, so Phase 7 only swaps the backend.

#### Removed
- The standalone `Microsoft.AspNetCore.SignalR` pin (SignalR
  ships in the shared framework) and the EF Core InMemory pin
  (the tests use Testcontainers).
- `FluentAssertions`: pinned but never referenced, and its
  licence changed at v8. Assertions stay on xUnit's `Assert`.

#### Fixed
- `nlog.config`: the `Microsoft.*` noise filter also swallowed
  `Microsoft.Hosting.Lifetime`, so the host booted without any
  console output.
- The console exporter was documented but never wired.
- `.gitignore` now covers `appsettings.Development.json` and
  `appsettings.Local.json`.

#### Decided
- ADRs **0005**, **0008** and **0009** promoted `Proposed` →
  `Accepted`.
- Repository made public (2026-07-31). Ruleset on `main` with
  `build-test` as a required check and an admin bypass for
  the one-man workflow.

### Phase 0 — Foundations & ADRs ✅

#### Added
- Repo scaffolding: MIT `LICENSE`, `.gitignore`, `README.md`,
  `CLAUDE.md` (coding standards), `mobile.md` and
  `.editorconfig`.
- `Directory.Build.props` (`net10.0`, nullable, warnings as
  errors, XML docs) and `Directory.Packages.props` (Central
  Package Management).
- `docs/adr/` with a MADR template and ADRs 0001–0009, all
  `Proposed` while the project is pre-code.
- `docs/ROADMAP.md` and two diagrams in `docs/diagrams/`:
  the project dependencies and the hot/cold storage path.

#### Changed
- Grain interfaces move out of the `<Module>.Contracts`
  projects into `Battles.Grains.Abstractions`, which keeps
  the Contracts free of Orleans
  ([ADR-0005](docs/adr/0005-inter-module-services.md)).

#### Decided
- Stack: .NET 10, Orleans, SvelteKit + Svelte 5, Azure SQL +
  Azure Table Storage, ASP.NET Core Minimal API + SignalR.
- Turn-based interaction model
  ([ADR-0007](docs/adr/0007-turn-based.md)).
- Plain service interfaces in per-module Contracts, no
  in-process bus
  ([ADR-0005](docs/adr/0005-inter-module-services.md)).
- NLog, not Serilog
  ([ADR-0006](docs/adr/0006-nlog-logging.md)); Kiota, not
  NSwag ([ADR-0004](docs/adr/0004-sveltekit-frontend.md)).

[full-changelog]: https://github.com/space-craft-collab/CombatSimulation/blob/54da1cd2d0c3e3772e18a4766cfff9ca939b3087/CHANGELOG.md
