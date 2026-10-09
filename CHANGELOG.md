# Changelog

All notable changes to this project will be documented
in this file.

The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 2 — Orleans embedded (in progress)

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
  Bots do not act on their own yet; the grain
  Timer for that comes with the round loop.
- `ICatalogQueryService` in `Catalog.Contracts` —
  the first cross-module contract (ADR-0005),
  served from a hard-coded four-species seed until
  EF Core arrives in Phase 3.
- `BattleGrainTests` — a full 1 vs 1 battle through
  the silo plus the guard rails (double action,
  own target, unknown species, double start).
- `AppHostFixture` — one booted host shared by
  the host-level tests via an xUnit collection.
  The silo binds fixed localhost ports, so only
  one host may be up at a time.

#### Changed
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
- `Battles` references `Microsoft.Orleans.Runtime`
  (new pin, 10.3.1): it carries `IPersistentState<T>`,
  which `Microsoft.Orleans.Sdk` does not.
- `BattleState` gained `WinnerId`.

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
