# Changelog

All notable changes to this project will be documented
in this file.

The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 1 — Walking skeleton (in progress)

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
