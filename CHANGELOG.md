# Changelog

All notable changes to this project will be documented
in this file.

The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 3 — Persistence (hot/cold) ✅

#### Added
- ROADMAP Phase 3 checklist: Azure Table Storage for grain
  state and reminders, per-module EF Core stores (Catalog
  species and arenas, player stats, battle snapshots), the
  hot→cold bridge, Testcontainers-backed tests and a
  `docker compose` setup for local runs.
- `compose.yaml` with Azurite (Table Storage) and SQL Server
  for local runs, with `.env.example` and
  `appsettings.Development.example.json` to copy from.
- Grain state and reminders switch to Azure Table Storage
  when `ConnectionStrings:TableStorage` is set; in-memory
  stays the default for tests and a bare `dotnet run`.
- Stored grain format frozen: `[Alias]` on every serializable
  grain type, explicit `BattleStatus` values, `[GrainType]`
  pinned on the three grains (same names Orleans derived
  before, so existing data stays readable); rules in
  `CONTRIBUTING.md`, guarded by `StoredFormatTests`.
- Host restart test on Testcontainers Azurite: a live battle
  keeps its state, round and pending turn deadline across a
  restart, and the deadline reminder fires on the new host.
- CI `integration` job for the `Category=Integration` tests;
  it runs after `build-test`, which now skips them. Both are
  required checks on `main`.
- EF Core base in `Shared.Infrastructure`: `ModuleDbContext`
  (one schema per module), `AddModuleDatabase<T>` over a
  context factory, per-schema migrations history, and pending
  migrations applied on startup in Development. `dotnet-ef`
  pinned as a local tool.
- Catalog species in SQL (`catalog.Species`), seeded through
  the `InitialCatalog` migration; `ICatalogQueryService` reads
  through `SpeciesReadRepository`.
- Catalog arenas in SQL (`catalog.Arenas`: Volcano, Glacier,
  Moss Grove), seeded through the `AddArenas` migration;
  `GET /arenas` lists them and `ICatalogQueryService` gains
  `FindArenaAsync`.
- Player stats in SQL (`players.PlayerStats`, `InitialPlayers`
  migration): `IPlayerStatsService.RecordResultAsync` in
  `Players.Contracts` counts a win and the losses of a decided
  battle through `PlayerStatsWriteRepository` (counters raised
  in SQL, safe under concurrent battles); `GET /players/{id}`
  reads through `PlayerStatsReadRepository` and returns 404
  before a player's first decided battle.
- Hot→cold bridge ([ADR-0003](docs/adr/0003-no-outbox.md)): a
  completed battle is stored in `battles.Battles` and
  `battles.BattleCombatants` (`InitialBattles` migration)
  through `BattleSnapshotWriteRepository`, a decided one is
  reported to `IPlayerStatsService`, then the battle and
  monster grains clear their hot state and deactivate — all in
  the turn that ended the battle. A failed write is logged and
  keeps the hot state; nothing is counted.
- `GET /battles/{id}` (and the hub's `Watch`) fall back to the
  cold store once a battle's hot state is gone; 404 only when
  neither store knows the id.
- `GET /players/{id}/battles`: a player's completed battles from
  the cold store, newest first (at most 50); an empty list before
  the first one. The slice lives in Battles, which owns the data.
- Integration test of the bridge on Testcontainers SQL Server and
  Azurite: the finished battle is in SQL and the player's stats,
  `GET /battles/{id}` still returns it, and its Table Storage rows
  are gone.

- Test coverage pass over the Phase 3 state: teams of two and
  three monsters are played (slots, a side standing while one
  monster stands, a fallen monster cannot act), the damage rule
  and the bot's tie-breaks have unit tests, the action, join and
  bot-seat validations have their 400/404/409 cases, a deadline
  re-arms itself over several rounds, the last round's SignalR
  delta carries the winner, `Unwatch` stops the deltas, and the
  history stops at 50 entries.
- On Table Storage: a finished battle leaves no reminder row,
  and an open battle survives a host restart — still listed,
  seat still free.
- A test for the accepted gap in ADR-0003: when the stats
  report fails after the snapshot was stored, the snapshot and
  the hot state stay and the battle still reads as completed.
- Architecture tests for the module boundaries: no module
  references another module's implementation, the `.Contracts`
  assemblies reference no Orleans, EF Core or ASP.NET Core, and
  every module's `Domain` namespace has types, so the layering
  rule cannot pass on an empty set.
- `GET /health/ready`, a readiness probe: one check per module
  database (`sql-catalog`, `sql-battles`, `sql-players`) and,
  when the silo persists to it, one for Table Storage
  (`table-storage`), each cut off after five seconds. An
  unreachable store answers 503. Tested against a host without
  its SQL Server and against one whose Azurite is stopped.
- Tests of the `ExactIds` migrations on databases that already
  hold rows: each module stops one migration earlier, gets rows
  under the old collation and is then migrated the way the host
  does it on startup. Stats, snapshots and the seeded Catalog
  rows are kept, the primary keys are back, and ids that differ
  only in case are separate from then on.
- Test coverage in CI: `dotnet-coverage` wraps the fast suite's
  `dotnet run` and `reportgenerator` writes the result into the
  run summary and an HTML artifact. Both are pinned as local
  tools; `tests/coverage.settings.xml` limits the measurement to
  the project's own assemblies, without migrations and generated
  code. The fast suite covers 96 % of the lines.
- Dependabot watches the images in `compose.yaml`.

#### Changed
- Player ids are capped at 64 characters; longer ones get a
  400 on starting or joining a battle.
- Battles against the bot count for the human side:
  `BattleResultDto.WinnerId` is null when the bot won, and
  `LoserIds` may be empty when the player beat it. Bots
  themselves are never counted.
- The arena's battle list drops `Created` entries (battles
  handed over and cleared) as well as `Completed` ones.
  Submitting an action to, or joining, a handed-over battle
  answers 409, as for any completed battle.
- Starting, opening or listing battles on an arena the Catalog
  does not know now returns 404 instead of creating an arena
  grain for any id. The HTTP tests play on the seeded `volcano`
  arena.
- Ids are taken exactly as written, in SQL as they already
  were as grain keys: every id column uses the binary collation
  `Latin1_General_100_BIN2` (`IdCollation.Ordinal`, migration
  `ExactIds` in each module), and lookups by id reject a match
  that differs in trailing spaces. `Volcano` is not the arena
  `volcano` (404), `Lava-Crab` is not a species (400), `Alice`
  and `alice` are two players. Player ids with whitespace at
  either end get a 400.
- An action with an ability other than `strike` gets a 400;
  before, any string was accepted and sent on to the watchers.
- SQL Server is now required to run the host
  ([ADR-0011](docs/adr/0011-sql-server-required.md)). The tests share
  one Testcontainers SQL Server, with a database per host; the
  integration trait is renamed `Category=Containers` →
  `Category=Integration`, since every test now uses a container.
- Testcontainers pins 4.0.0 → 4.17.0.
- `GET /health` is the liveness probe and runs no check, so an
  outage of SQL Server or Table Storage never gets the host
  restarted; the dependencies are asked on `/health/ready`.
- The container images are pinned to a version instead of
  `latest`: Azurite `3.38.0` and SQL Server
  `2022-CU28-ubuntu-22.04`, the versions `latest` pointed to.
  `compose.yaml` is the one place they are named; the test
  fixtures read their images from it.
- Both CI jobs stop after ten minutes (`timeout-minutes`)
  instead of GitHub's six-hour default.
- Grain storage, the reminder service and the Table Storage
  health check share one `TableServiceClient`.
- All Orleans packages aligned on 10.4.2 (the Azure Storage,
  Client and TestingHost pins were still on 10.3.1).

#### Removed
- `coverlet.collector`: it only attaches through VSTest
  (`dotnet test`), which the xUnit v3 `dotnet run` never starts,
  so it never measured anything here.
- The unused `Microsoft.Orleans.TestingHost` pin; the README no
  longer lists the TestingHost either. The tests boot the real
  `AppHost` with its co-hosted silo.

#### Fixed
- Table Storage grain storage sets `DeleteStateOnClear`: Orleans'
  default only blanks a cleared row, so a completed battle's hot
  rows would have stayed forever.
- A bot-only battle's final round never stored its result: the
  round stopped the bot loop, which cancelled the timer token the
  state write was still using.
- After a host restart on Table Storage, a turn deadline could
  come a minute late: the reminder's start time is stored in
  whole milliseconds, so its tick could arrive a fraction of a
  millisecond before the deadline and be ignored as early. The
  deadline is now cut to whole milliseconds as well. It showed
  when the battle's grain was already active on the new host.
- With SQL Server's default collation, `POST
  /arenas/Volcano/battles` opened a battle on an arena grain of
  its own that `/arenas/volcano/battles` never listed, and
  players whose ids differed only in case or trailing spaces
  shared one stats record (see "Ids are taken exactly" above).
- A start that failed on a later participant's unknown species
  left the earlier participants' monsters in the battle; a
  second start then fielded them twice. Every team is now
  looked up before the first monster is added.
- Docs cross-check: the two diagrams showed the stats report
  before the snapshot write, hot state that "ages out", a
  periodic auto-save and Orleans in `Shared.Infrastructure`.
  They now follow the code: snapshot first, an explicit clear,
  a state write every round, Orleans wiring in the `AppHost`.
  `CLAUDE.md` states the layering rule in words instead of an
  arrow chain, and the README architecture section marks only
  the frontend as planned.

#### Decided (process)
- ADR **0003** (still `Proposed`): the snapshot writer is the
  concrete `BattleSnapshotWriteRepository`, not an
  `IBattleSnapshotWriter` interface
  ([ADR-0010](docs/adr/0010-delegate-test-seams.md)). The failed
  write is tested against SQL Server through a primary-key
  collision, so the write is deliberately not idempotent.
- ADR **0011** — SQL Server required for the cold path, in
  tests too: no seed, SQLite or InMemory fallback, so the code
  path that ships is the one the tests run. **`Accepted`**
  from the start, since the code already commits to it.
- ADR **0003** (still `Proposed`) gains the hot-state
  lifetime: Table Storage holds live battles only; after a
  successful snapshot the battle grain clears its own and its
  monsters' state, a failed write keeps it for a replay, and
  `GET /battles/{id}` falls back to the cold store.
- ADR **0007** (turn-based interaction model) promoted
  `Proposed` → **`Accepted`**: the round loop, bot
  opponent, deadlines, both game modes and the SignalR
  deltas are built as it describes.
- ADR **0003** (still `Proposed`) names one crash window
  instead of three: the snapshot is lost when the silo dies
  after the battle completes but before the write, or when
  the write fails.
- ADR conventions: a factual correction that leaves the
  decision unchanged may be made in place on an `Accepted`
  ADR, recorded with its date in a `## Corrections` section.
  A changed decision still needs a new ADR. First uses:
  ADR **0005** (grain interfaces inherit a Guid or a string
  key marker) and ADR **0008** (`Shared.Infrastructure`
  holds no Orleans or Table Storage wiring; that lives in
  the `AppHost`).
- ADRs **0003** (no outbox), **0006** (NLog) and **0010**
  (delegate test seams) promoted `Proposed` → **`Accepted`**:
  the hot→cold bridge runs as 0003 describes, NLog has been
  wired since Phase 1, and the code holds no test-only
  interface. Before the freeze, 0003 gained the case of a
  failed stats report after a stored snapshot, 0006 lost its
  future tense and notes that retiring NLog in Phase 7 needs
  a superseding ADR, and 0010's example now stays inside one
  module. **0004** stays `Proposed` until the frontend exists.
- Cross-references corrected where they credited an ADR with
  more than it says: ADR **0004** → 0002, ADR **0007** → 0003
  and ADR **0011** → 0001/0008 (the latter two recorded under
  `## Corrections`). The Phase 3 definition of done no longer
  promises stats for "both players": bots and abandoned
  battles count for nobody.
- Phase 3 closed: a live battle keeps its state, round and
  deadline in Table Storage across a host restart; a finished
  one moves to SQL Server, counts in the stats of its human
  players and leaves no hot rows behind. The Catalog and the
  player stats live in SQL Server, one schema per module. The
  migration bundle moves to Phase 5, where the first deployment
  needs it.

### Phase 2 — Orleans embedded ✅

#### Added
- **Co-hosted Orleans silo** — `AppHost` now
  references `Microsoft.Orleans.Server` and calls
  `UseOrleans()` with `UseLocalhostClustering()`
  and in-memory grain storage. Web host and silo
  share one process, as ADR-0002 specifies.
  Azure Table Storage replaces both providers in
  Phase 3/5.
- `Battles/Grains/` — the Grains folder ADR-0009
  reserves for the Battles module, holding a
  boot-probe grain (`IPingGrain` in
  `Battles.Grains.Abstractions`, `PingGrain` in
  `Battles`). It carries no domain meaning and is
  retired once real grains exist.
- `SiloBootTests` — resolves `IClusterClient`
  from the host's own DI and calls a grain,
  which is the concrete proof of ADR-0002.
- Grain interfaces in `Battles.Grains.Abstractions`:
  `IArenaGrain` (one per arena, string key = Catalog
  arena id), `ILiveBattleGrain` (Guid battle id) and
  `IMonsterInstanceGrain` (`"{BattleId}:{Slot}"`),
  plus `[GenerateSerializer]` record DTOs such as
  `BattleSetup` and `BattleState`. Collections use
  `ImmutableArray<T>`, a concrete type Orleans has a
  codec for, rather than an `IReadOnlyList<T>` whose
  runtime type a collection expression leaves to the
  compiler. Interfaces only —
  implementations follow.
- `GrainContractSerializationTests` — round-trips
  the DTOs through the silo's own serializer.
- Grain implementations in `Battles/Grains/`, state
  via `IPersistentState<T>` on the in-memory
  provider (Phase 3 swaps in Table Storage):
  - `ArenaGrain` creates battles and lists the
    unfinished ones, pruning completed battles.
  - `LiveBattleGrain` runs `Created → InProgress →
    Completed`. A round resolves once every side
    still standing has acted; actions play fastest
    monster first, and the last side standing wins.
  - `MonsterInstanceGrain` owns the hit points.
  - Damage rule (`attack - defense`, at least 1) in
    `Battles/Domain/`.
- **Rule-based bot** (ADR-0007): `BotStrategy` in
  `Battles/Domain/` attacks the weakest standing
  opponent with the bot's strongest monster.
  `LiveBattleGrain` lets bots act inline once every
  human side has acted, so human vs bot needs no
  timer; a bot's id submitting an action is
  rejected. `Combatant` now mirrors hit points from
  each damage snapshot (the monster grain stays the
  authority) and `IsDefeated` is derived from them.
  `BotStrategyTests` unit-test the rule via
  `InternalsVisibleTo`.
- **Round loop** (ADR-0007): an Orleans **reminder**
  enforces the human turn deadline (60s) — the
  round resolves without humans who missed it; if
  no human acted at all, the battle ends abandoned
  (`Completed`, no winner). A **grain timer** plays
  battles with only bots left (500ms per round) and
  is re-armed on reactivation. Pacing comes from
  `Battles:Timing`; `BattleState` exposes the
  `TurnDeadline` for a client countdown. New pin
  `Microsoft.Orleans.Reminders`, plus
  `UseInMemoryReminderService()` in the AppHost.
- **Battles feature slices** (ADR-0009): `StartBattle`
  (`POST /arenas/{arenaId}/battles` — play vs bot, the
  bot mirrors the visitor's team), `SubmitAction`
  (`POST /battles/{id}/actions` — 400 for an illegal
  move, 409 once the battle is over or the side already
  acted) and `GetBattle` (`GET /battles/{id}`). The
  grain's `BattleState` now carries `CombatantSnapshot`s
  (slot, side, species, HP) so a client can pick targets;
  the HTTP responses map them to their own records.
  `BattleEndpointTests` play the slices over HTTP.
- **SignalR round deltas** (ADR-0007): the `WatchBattle`
  slice maps `BattleHub` at `/hubs/battles`. `Watch(battleId)`
  joins the battle's group and returns its current state;
  after every resolved round the battle grain pushes a
  `BattleEventDto` (round, status, winner, next deadline,
  strikes with damage and remaining HP) on `RoundResolved`.
  An abandoned battle sends one last delta without
  strikes. The grain publishes only after the round is
  stored, and reaches the hub context directly because
  the silo shares the web host's process (ADR-0002); a
  multi-silo setup needs a SignalR backplane (Phase 5).
  `BattleHubTests` watch battles over long polling; new
  test-only pin `Microsoft.AspNetCore.SignalR.Client`
  10.0.14 (10.0.12 pulls a vulnerable `SignalR.Common`).
- **Wait for a human** mode (ADR-0007): new status
  `WaitingForPlayers`. `POST /arenas/{arenaId}/battles`
  with `"opponent": "Human"` opens a battle through
  `IArenaGrain.OpenBattleAsync`; `GET /arenas/{arenaId}/battles`
  (`ListBattles`) lists open and running battles;
  `POST /battles/{id}/participants` lets a second human
  join and `POST /battles/{id}/bot` seats the bot instead,
  mirroring the host's team — both through
  `ILiveBattleGrain.JoinAsync` (`JoinBattle`). The hub sends
  the first state on `BattleStarted` to watchers of the open
  battle. A join deadline (`Battles:Timing:JoinDeadline`,
  5 minutes) abandons battles nobody joins, through the same
  reminder as the turn deadline.
- `ICatalogQueryService` in `Catalog.Contracts` —
  the first cross-module contract (ADR-0005),
  served from a hard-coded four-species seed until
  EF Core arrives in Phase 3.
- `BattleGrainTests` — a full 1 vs 1 battle through
  the silo plus the guard rails (double action,
  own target, unknown species, double start), and
  the round loop: bot vs bot to the end, missed
  deadline, abandoned battle. The fixture shortens
  the deadline to 2s and the bot interval to 20ms.
- `AppHostFixture` — one booted host shared by
  the host-level tests via an xUnit collection.
  The silo binds fixed localhost ports, so only
  one host may be up at a time.

#### Changed
- `BattleSummary` lists the joined `ParticipantIds` instead
  of a participant count, read from the battle's own state;
  `ArenaState` keeps only the battle ids.
- The battle grain looks up every species of a side before
  it takes a slot, so a failed join leaves the open battle
  untouched.
- The ADR-0009 architecture guard now also
  forbids `Domain` → `Grains`, alongside
  `Features` and `Infrastructure`.
- Orleans **9.0.0 → 10.3.1** across all seven
  `Microsoft.Orleans.*` pins. 9.0.0 resolved its
  `net8.0` build under our `net10.0` TFM; 10.x
  ships a native `net10.0` lib. Done before the
  first grain exists, while it is still a
  one-line edit.
- `Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`
  and `Microsoft.EntityFrameworkCore.*` pins
  **10.0.0 → 10.0.12**. Forced: Orleans 10.3.1
  requires `Microsoft.Extensions.Hosting` >= 10.0.5,
  so 10.0.0 tripped NU1109 (package downgrade).
  Whole group moved together to stay coherent.
- Tests now run via `dotnet run --project
  tests/OrleansMonsterArena.Tests` (CI and
  CONTRIBUTING) instead of `dotnet test`. xUnit v3
  test projects are executables; `dotnet test` on
  the .NET 10 SDK fails with xunit.v3 4.x unless
  opted into Microsoft.Testing.Platform.
- Test stack bumped: `xunit.v3` **3.2.2 → 4.0.1**,
  `xunit.runner.visualstudio` 3.1.5 → 4.0.0,
  `Microsoft.NET.Test.Sdk` 18.8.1 → 18.10.1,
  `coverlet.collector` 10.0.1 → 10.1.0. Applied by
  hand: Dependabot closed its grouped PR #9 as
  superseded after the `dotnet run` switch.
- OpenTelemetry core packages (`OpenTelemetry`,
  `Extensions.Hosting`, Console + OTLP exporters)
  **1.17.0 → 1.19.1**, catching up with the 1.19.0
  instrumentation packages from Dependabot PR #8.
- ADR-0007 now defines arenas and battles: an
  arena is a Catalog template (like a game map),
  each arena hosts n concurrent battles, each with
  n participants fielding n monsters.
  `IArenaGrain` is one grain per arena, acting as
  its battle browser. Phase 2 demo runs 1 vs 1.
- ADR-0007 and the roadmap now make the showcase
  playable solo: a rule-based bot opponent moves
  into Phase 2 (Phase 8 only swaps its strategy
  for Claude). Two modes — **play vs bot**
  (default) and **wait for a human** with a new
  `WaitingForPlayers` status, where the "play vs
  bot instead" offer shows immediately. Bot turns
  run inline, so human vs bot needs no timer.
- `Battles` references `Microsoft.Orleans.Runtime`
  (new pin, 10.3.1): it carries `IPersistentState<T>`,
  which `Microsoft.Orleans.Sdk` does not.
- `BattleState` gained `WinnerId`.

#### Removed
- The boot-probe grain (`IPingGrain` / `PingGrain`).
  `SiloBootTests` proves ADR-0002 with a call to
  `ILiveBattleGrain` instead.

#### Decided (process)
- ADRs **0001** (modular monolith) and **0002**
  (co-hosted silo) promoted `Proposed` →
  **`Accepted`**: one process now hosts every module,
  the Orleans silo and the SignalR hub, and web-side
  code calls grains in-process. ADR-0002 gained the
  SignalR backplane follow-up for Phase 5 before
  acceptance.
- Phase 2 closed: a battle — human vs bot, human vs
  human, bot vs bot — runs end to end through grains,
  playable over HTTP and watchable over SignalR. Grain
  state and reminders stay in memory until Phase 3.

### Phase 1 — Walking skeleton ✅

#### Added
- ADR-0010 — function-delegate test seams instead
  of test-only interfaces (pattern + DI/testing
  conventions; elevates the rule from ADR-0005
  and `CLAUDE.md`)
- `OrleansMonsterArena.slnx` with the full ADR
  project graph (11 projects under `src/` +
  `tests/`): three module projects with
  `Domain/Features/Infrastructure` folders and
  `<Module>Module.cs` entry points, per-module
  Contracts, `Battles.Grains.Abstractions`,
  `Shared.Kernel`, `Shared.Infrastructure`,
  `AppHost`, test project
- `Shared.Infrastructure`: NLog wiring
  (`AddArenaLogging`, ADR-0006) and OpenTelemetry
  tracing/metrics (`AddArenaTelemetry`, OTLP
  always + console exporter in Development)
- `AppHost`: composed host with `GET /health`
  and `nlog.config` (console target)
- xUnit v3 tests: `/health` smoke test +
  NetArchTest guard for the ADR-0009 layering
- `.github/workflows/ci.yml` — restore, build,
  test on push/PR to `main`

##### Pre-publication polish
- `global.json` — SDK pinned to 10.0.110
  (`rollForward: latestFeature`); CI now resolves
  the SDK via `global-json-file` instead of the
  floating `10.0.x`
- `.github/dependabot.yml` — weekly NuGet
  (grouped: Orleans / ASP.NET+EF / OpenTelemetry
  / testing) and GitHub Actions updates
- `SECURITY.md` — private vulnerability reporting
- `CONTRIBUTING.md` — showcase-project scope,
  build commands, pointer to `CLAUDE.md` + ADRs
- `ci.yml`: least-privilege
  `permissions: contents: read` and a
  `cancel-in-progress` concurrency group

#### Changed
- `Directory.Packages.props`: test stack moved to
  **xUnit v3** (`xunit.v3` 3.2.2 — the old `xunit`
  2.9.2 pin was the v2 line, contradicting the
  xUnit-v3 lock); runner + Test.Sdk bumped
- Removed the legacy standalone
  `Microsoft.AspNetCore.SignalR` 1.2.0 pin —
  SignalR ships in the .NET 10 shared framework
- Removed `Microsoft.EntityFrameworkCore.InMemory`
  pin — integration tests use Testcontainers
  (MsSql/Azurite) instead
- ROADMAP: new **Phase 6 — Auth & accounts**
  (closes the 5→7 numbering gap; JWT + Identity
  had pinned packages but no phase); **OTEL
  wiring pulled into Phase 1** so observability
  exists from the start and Phase 7 only swaps
  the backend (matches ADR-0006)
- ADRs 0005/0006/0008/0009: unified host naming
  to **`AppHost`** (was mixed with "Bootstrapper")
- ADR-0009: diagram follow-up note updated —
  `project-dependencies.html` is redrawn at
  module granularity
- OpenTelemetry pins 1.10.0 → 1.17.0 (1.10.0
  has known vulnerabilities, flagged by NU1902);
  `NetArchTest.Rules` 1.3.2 pin added

#### Fixed
- `nlog.config`: the `Microsoft.*` noise filter
  also swallowed `Microsoft.Hosting.Lifetime`, so
  the host booted with **no console output at all**
  — not even "Now listening on: ...". Lifetime
  logs are now allowed through ahead of the filter.
- `AddArenaTelemetry` documented a console exporter
  and `Shared.Infrastructure` referenced the
  package, but `AddConsoleExporter()` was never
  called. It is now wired for traces and metrics,
  gated on `IHostEnvironment.IsDevelopment()`.
- `.gitignore`: added `appsettings.Development.json`
  and `appsettings.Local.json` — only
  `appsettings.*.local.json` was covered, leaving
  the file most likely to hold real Azure SQL /
  Table Storage connection strings committable.
- `HealthEndpointTests`: pass
  `TestContext.Current.CancellationToken` to
  `GetAsync` (clears xUnit1051; build is now
  warning-free).

#### Decided (process)
- ADRs **0005** (inter-module services), **0008**
  (Shared layer split) and **0009** (per-module
  structure) promoted `Proposed` → **`Accepted`**:
  the Phase 1 solution graph builds exactly that
  structure and ADR-0009 is enforced by the
  NetArchTest guard. From here they follow the
  supersede workflow instead of in-place edits.
  ADR-0001 and ADR-0002 stay `Proposed` until a
  silo actually boots (Phase 2).
- Branch protection on `main` completed: ruleset
  active with the CI `build-test` status check as
  a required gate (admin bypass kept for the
  one-man workflow). The Phase 0 placeholder rule
  is superseded by it.
- Repository made **public** (2026-07-31), which
  is what activates rulesets on the free plan.
- Dependabot's opening batch merged: `actions/checkout`
  4 → 7, `actions/setup-dotnet` 4 → 6, testing group
  bump. `FluentAssertions` dropped — it was pinned
  but never referenced (and its licence changed at
  v8); assertions stay on plain xUnit `Assert`.

### Phase 0 — Foundations & ADRs ✅

#### Added
- Initial repo scaffolding: MIT `LICENSE`,
  `.gitignore` (.NET + Node + IDE), `README.md` stub
- `CLAUDE.md` — coding standards (.NET, English code
  + XML doc comments on public API)
- `mobile.md` — mobile-friendly output style guide
- Architecture plan (modular monolith + Orleans
  co-hosted, hot/cold path split)
- `.editorconfig` — enforces CLAUDE.md style
  (Allman, 4-space indent, file-scoped namespaces,
  naming rules, `var` when apparent, expression bodies)
- `Directory.Build.props` — solution-wide MSBuild
  defaults: `net10.0`, `LangVersion=latest`,
  `Nullable=enable`, `ImplicitUsings=enable`,
  `TreatWarningsAsErrors`, latest analyzers,
  `GenerateDocumentationFile` for public XML docs
  (relaxed for `*.Tests` projects)
- `Directory.Packages.props` — Central Package
  Management pinning Orleans 9, EF Core 10,
  ASP.NET Core 10, NLog, OpenTelemetry, xUnit +
  Testcontainers
- `docs/adr/` — ADR directory with MADR template,
  index `README.md`, and ADRs 0001–0009
  (modular monolith, Orleans co-hosted, no outbox,
  SvelteKit frontend, inter-module service interfaces
  via per-module Contracts, NLog logging, turn-based
  model, Shared layer split, per-module vertical-slice
  structure). All ADRs are `Proposed` while the
  project is pre-code.
- `docs/ROADMAP.md` — phased delivery plan with the
  Phase 1 walking-skeleton checklist
- `docs/diagrams/project-dependencies.html` — interactive
  project reference graph + battle data-flow walkthrough
  (ADR-0005, ADR-0008); redrawn at module granularity
  after ADR-0009
- `docs/diagrams/hot-cold-storage.html` — battle
  lifecycle across the hot and cold storage paths
  (ADR-0003, ADR-0007)

#### Changed
- ADR-0005 / ADR-0008 / ADR-0003: grain interfaces move out
  of the plain `<Module>.Contracts` projects into a separate
  `Battles.Grains.Abstractions` project that carries the
  `Microsoft.Orleans.Sdk` reference — keeps `*.Contracts`
  Orleans-free. `IBattleSnapshotWriter` clarified as a
  Battles-internal abstraction, not a cross-module contract.

#### Decided
- Stack: .NET 10, Orleans 9, SvelteKit + Svelte 5,
  Azure SQL Basic + Azure Table Storage, NLog,
  ASP.NET Core Minimal API + SignalR
- Interaction model: **turn-based** (fits actor model)
- Inter-module comms: **plain service interfaces in
  per-module `<Module>.Contracts` projects** (RiverBooks
  pattern) — no in-process bus / Mediator
- OpenAPI client: **Kiota** (NSwag rejected)
- Logging: **NLog** (not Serilog)

#### Pending
- Event-Storming notes (paper, photo in README)
- SvelteKit adapter choice — deferred to start of
  Phase 4 (1-day spike, see ADR-0004)

#### Decided (process)
- Branch protection on `main`: `protect-main` rule created
  (block force-push + deletion, no PR-gate). Not enforced
  while the repo is private on the free plan — takes effect
  when the repo goes public. CI status-check gate
  (`dotnet test`) to be added in Phase 1.
