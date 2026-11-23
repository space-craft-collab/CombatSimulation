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
Phases 0–3 were condensed on 2026-11-21, Phase 4.5 on
2026-11-22, Phase 4 on 2026-11-23; the full entries are in
this file's history ([Phases 0–3][full-changelog],
[Phase 4.5][changelog-4.5], [Phase 4][changelog-4]).

## [Unreleased]

### Phase 4.5 — Combat depth ✅

Inserted while Phase 4 was under way; condensed on
2026-11-22. The rules are in
[ADR-0012](docs/adr/0012-combat-rules.md), the numbers in
[`docs/balance.md`](docs/balance.md).

#### Added
- Elements: Fire > Nature > Frost > Fire, on every species
  and every ability (`Element`, `ElementChart`); ×1.5 with
  the advantage, ×0.75 against it. The basic `strike` is
  neutral.
- Abilities as Catalog data (`catalog.Abilities`,
  `catalog.SpeciesAbilities`), three per species: `strike`,
  an elemental attack and a utility. `GET /species` and
  `MonsterSpeciesDto` list them.
- Cooldowns per monster and ability. `MoveRule` says which
  moves are legal; an ability the monster does not know or
  has not ready is a 400.
- Status effects: burn, guard, slow and heal.
- Two species, Glacier Yak and Thorn Lynx: six in all, a
  fast and a sturdy one per element.
- A bot with a team of its own, as large as the visitor's,
  drawn from the Catalog (`BotTeam`, `BotOpponent`), and a
  rule that rates every legal move (`BotStrategy`).
- Balance simulator in the test project: the bot against
  itself on the pure rules, every one-on-one and every team
  of three against every other. Its report is
  `docs/balance.md`; `BalanceReportTests` keeps it current.
- `BalanceGuardRailTests`: a species wins 40–60 % of its
  team battles; nobody wins less than 30 % one on one except
  against its element counter; a one-on-one lasts 4–9 rounds
  on average, three against three 8–14, none over 30; every
  ability is used in at least a tenth of its battles.
- Frontend: an ability picker with cooldowns, element
  badges, effect chips, a hint on the element advantage and
  a log that words every event; elements and abilities in
  the team picker; drawings for the two new species.
- `ICatalogQueryService.ListSpeciesAsync`.
- Catalog migrations `AddElements`, `AddAbilities`,
  `AddUtilityAbilities`, `AddYakAndLynx`, `RetuneCombat`.
- [ADR-0012](docs/adr/0012-combat-rules.md): the combat
  rules.

#### Changed
- The rules of a round left `LiveBattleGrain` for the pure
  `RoundResolver`: the actions by speed, then burns, then
  effects and cooldowns run down.
- Damage: defense scales a hit by `10 / (10 + defense)`
  instead of being subtracted; the ability's power, the
  element and a guard scale it too, in whole numbers.
- A battle stores its monsters' abilities, cooldowns and
  effects; `GET /battles/{id}` and the hub show them.
- The hub's round delta carries `events` (an ability used,
  or a burn's damage) instead of `strikes`, and every
  monster as it stands after the round.
- `POST /battles/{id}/actions` checks `abilityId` against
  the acting monster; a guard or a heal targets the monster
  itself.
- The monster grain is told its monster's hit points after
  a round (`SetHitPointsAsync`) instead of each hit
  (`ApplyDamageAsync`); a round that fails halfway falls
  back to the stored state and can be played again.
- The bot no longer mirrors the visitor's team, and no
  longer sends its strongest monster at the weakest
  opponent.
- Every species and ability retuned. A team battle went
  from 79 rounds on average, often undecided after 100, to
  13; one on one from a strict ladder to matchups the
  element or a close race decides.
- Tests: the battle tests play to a condition through
  `GrainBattleDriver` and `HttpBattleDriver` instead of
  pinning hit points and round counts; the test hosts give
  the bot a draw without chance. New unit tests for the
  rules, the bot, the seed, the stored format of a battle
  (old state included) and the frontend's wording.

#### Fixed
- A storage failure in the middle of a round could apply a
  hit twice when the round was replayed at the deadline, or
  leave a round that never resolved.

#### Decided
- [ADR-0012](docs/adr/0012-combat-rules.md) `Accepted`.
- The phase is numbered 4.5, so Phases 5–8 keep the numbers
  the ADRs refer to.
- The guard rail on battle length is set per kind of battle;
  the single range of 5–12 rounds first planned cannot hold
  for one against one and three against three at once.

### Phase 4 — Frontend ✅

Closed 2026-11-23. The frontend's decisions are in
[ADR-0004](docs/adr/0004-sveltekit-frontend.md), the Easy
bot's in [ADR-0013](docs/adr/0013-bot-difficulty.md).

#### Added
- `frontend/`: SvelteKit 3 with Svelte 5 runes, TypeScript,
  Tailwind CSS and `adapter-node`, built and run in
  containers only (`frontend/compose.yaml`, a `Dockerfile`).
- OpenAPI document at `/openapi/v1.json`, committed as
  `docs/api/openapi.json`, with a tag per module and the
  400/409 problem responses; `GET /species`.
- Kiota TypeScript client under `frontend/src/lib/api`,
  generated by `scripts/generate-api-client.sh`;
  `src/lib/arena` wraps it and types the hub by hand.
- CORS for the origins under `Cors:AllowedOrigins`, without
  credentials.
- The solo path, phone-width first: a nickname on the first
  visit (in `localStorage`), the arena browser, a team picker
  for one to three monsters, and the battle against the bot.
- Live battle view: each round's delta from the hub, the turn
  deadline as a countdown, a battle log and the result; a
  reload picks a battle up again, a finished one from SQL.
- Waiting for a human: open a battle, or join one from the
  arena's list of open battles in the team picker; the bot
  offer shows at once, `BattleStarted` starts the battle.
- Against a human the view names the opponent, shows "Your
  move is in" until the other side moved, and words the
  result for either side; "Nobody joined" for a lapsed one.
- Player dashboard at `/players/{nickname}`: wins, losses,
  win rate and the last battles, or an empty state; linked
  from the header's nickname and from the result.
- An "Easy" difficulty against the bot: its monsters enter
  with 75 % of their hit points and attack. Chosen in the
  team bar, shown as a badge in the battle view.
- `difficulty` on the start request, on
  `POST /battles/{id}/bot` and on every battle response;
  stored hot and cold (migration `BattleDifficulty`).
- A picture per arena (AI-generated WebP): the tile on the
  start page, a banner on the team picker and a dimmed
  backdrop behind the battle.
- `scripts/play-start.sh` and `scripts/play-stop.sh`: the
  infrastructure, the host and the frontend for a round in
  the browser (compose project `oma-play`).
- `frontend/scripts/check-malware.mjs`: checks the lockfile
  against the GitHub Advisory Database.
- CI: a `frontend` job (malware check, lint, type check,
  Vitest, build), an `e2e` job running Playwright against the
  app, and a stale-client check in `build-test`.
- Dependabot watches the frontend's npm packages and its
  `Dockerfile`.
- `CONTRIBUTING.md`: the API contract and client, the
  frontend, and the rules for npm dependencies.
- Tests: Vitest for the battle state, the log, the lobby, the
  dashboard and the nickname rules; Playwright for the solo
  path, two players, the dashboard; the Easy handicap.

#### Changed
- ADR-0004 records the container-only toolchain,
  `adapter-node` after a spike, and Kiota as the only client
  generator (the `@hey-api/openapi-ts` fallback is dropped).
- The HTTP API reads numbers from JSON numbers only, no
  longer from strings as well.
- The team picker: a whole species card adds the monster,
  with stat bars; the team sits in a bar at the bottom
  (`TeamDock`); which element beats which above the list.
- "Fight again" hands the team and its difficulty back to
  the picker in the address; an arena chosen from the list
  starts empty, on Normal.
- A new nickname may hold no slash and may not be `.` or
  `..`.
- A battle takes a monster's full hit points from the
  monster as it enters, no longer from its species.
- `docs/balance.md` says that Easy lies outside the guard
  rails.
- Smaller UI changes: arena cards are links as a whole, the
  header fits a phone, monsters stand on tiles in their
  element's colour.

#### Fixed
- `dotnet run --project src/AppHost` started the host as
  Production; a `launchSettings.json` now selects
  Development and `http://localhost:5000`.
- An arena chosen from the list no longer starts with the
  team of the last battle.

#### Decided
- [ADR-0004](docs/adr/0004-sveltekit-frontend.md) `Accepted`.
- [ADR-0013](docs/adr/0013-bot-difficulty.md) `Accepted`.
- npm packages: exact versions, a week's cooldown, no install
  scripts, a malware check before every install.
- Monster portraits deferred; the monsters stay inline SVG.
- `frontend` and `e2e` are required checks on `main`, next
  to `build-test` and `integration` (2026-11-23).

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
[changelog-4.5]: https://github.com/space-craft-collab/CombatSimulation/blob/9cb41b8d7615ea00348d290a291b887d9ad4d0ee/CHANGELOG.md
[changelog-4]: https://github.com/space-craft-collab/CombatSimulation/blob/419df4f54797e449d592fd1b94c7803d41d7f904/CHANGELOG.md
