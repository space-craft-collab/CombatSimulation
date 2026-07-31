# Project Roadmap

Phased delivery plan for Orleans Monster Arena. Each phase is
one line; the current phase is expanded into a checklist with a
definition of done. The ADRs in [`adr/`](adr/) are the
authoritative source for *architecture* — this file only
sequences the work.

## Phases at a glance

- **Phase 0 — Foundations & ADRs** ✅ done
  Repo scaffolding, coding standards, build/package config,
  ADRs 0001–0009, architecture diagrams.
- **Phase 1 — Walking skeleton** ⬅ current
  Compilable, runnable, CI-green solution that establishes the
  full project graph. No domain logic yet.
- **Phase 2 — Orleans embedded**
  Co-hosted silo ([ADR-0002](adr/0002-orleans-cohosted.md)),
  first grains (`IArenaGrain`, `ILiveBattleGrain`,
  `IMonsterInstanceGrain`), turn-based round loop
  ([ADR-0007](adr/0007-turn-based.md)), SignalR round deltas.
- **Phase 3 — Persistence (hot/cold)**
  Azure Table Storage grain state + EF Core cold store, hot→cold
  snapshot bridge ([ADR-0003](adr/0003-no-outbox.md)).
  Completes the MVP.
- **Phase 4 — Frontend**
  SvelteKit + Svelte 5 runes, Kiota client; adapter spike
  ([ADR-0004](adr/0004-sveltekit-frontend.md)).
- **Phase 5 — Scale-out**
  Multi-silo on Azure Container Apps, cluster membership over
  Table Storage.
- **Phase 6 — Auth & accounts**
  JWT bearer auth + ASP.NET Core Identity (cold-path EF store),
  player accounts, protected SvelteKit routes
  ([ADR-0004](adr/0004-sveltekit-frontend.md)).
- **Phase 7 — Observability**
  OTEL (wired since Phase 1) → Grafana stack
  (Alloy/Tempo/Loki/Prometheus); retire NLog.
- **Phase 8 — AI opponents**
  Per-turn Claude API call inside the round budget
  ([ADR-0007](adr/0007-turn-based.md)).

Phases 5+ are indicative and will be detailed when reached.

## Phase 1 — Walking skeleton

**Goal:** one solution that compiles under
`TreatWarningsAsErrors`, boots the host, answers a health probe,
and passes CI — with every project from the ADR graph present
and wired with the correct references. No business logic; this
is the structural skeleton that later phases fill in.

### Project graph

Per module (`Catalog`, `Battles`, `Players`):

- `<Module>` — a **single project**
  ([ADR-0009](adr/0009-module-internal-structure.md)) with
  `Domain/`, `Features/`, `Infrastructure/` folders. References
  `Shared.Kernel`, `Shared.Infrastructure`, consumed
  `<Module>.Contracts`, and `<FrameworkReference
  Include="Microsoft.AspNetCore.App" />` for feature-local Minimal
  API endpoints. Intra-module layering is enforced by an
  architecture test, not csproj edges.
- `<Module>.Contracts` → `Shared.Kernel` only (Orleans-free)

Battles-only:

- `Battles.Grains.Abstractions` → `Microsoft.Orleans.Sdk`
  (consumed by `Battles.*` and the host only)

Shared:

- `Shared.Kernel` → no infra packages
- `Shared.Infrastructure` → EF Core / Orleans / Azure / NLog

Host + tests:

- `AppHost` (ASP.NET Core composition root) → each module
  project, `Battles.Grains.Abstractions`, `Shared.Infrastructure`
- `*.Tests` (xUnit v3)

This is the [ADR-0005](adr/0005-inter-module-services.md) /
[ADR-0008](adr/0008-shared-layer-split.md) dependency
direction. No module references another module's implementation
assembly; consumers see only `<Module>.Contracts`. The internal
`Domain`/`Features`/`Infrastructure` folder layout per module
follows [ADR-0009](adr/0009-module-internal-structure.md).

### Checklist

- [x] `OrleansMonsterArena.slnx` (modern solution format) with
      the project graph above, projects under `src/` and `tests/`
- [x] References wired per ADR-0005 / ADR-0008 / ADR-0009; no
      forbidden edges (no module → another module's implementation)
- [x] Each module is one project with `Domain/`, `Features/`,
      `Infrastructure/` folders + `<Module>Module.cs` registration
      entry; ASP.NET via `FrameworkReference`
      ([ADR-0009](adr/0009-module-internal-structure.md))
- [x] Architecture test (NetArchTest): per module, `Domain` does
      not depend on `Features`/`Infrastructure`; runs in CI
      ([ADR-0009](adr/0009-module-internal-structure.md))
- [x] `AppHost` boots; `GET /health` returns 200
- [x] Minimal NLog wiring in `Shared.Infrastructure`, consumed
      by `AppHost` ([ADR-0006](adr/0006-nlog-logging.md))
- [x] OTEL traces + metrics wiring (ASP.NET Core + runtime
      instrumentation, OTLP + console exporters) in
      `Shared.Infrastructure`, consumed by `AppHost`
      ([ADR-0006](adr/0006-nlog-logging.md)) — observability
      from the start; Phase 7 only swaps the backend
- [x] One xUnit smoke test: host returns 200 on `/health`
- [x] `.github/workflows/ci.yml` — restore + build + test on
      push / PR to `main`
- [ ] Add the `dotnet test` status-check gate to branch
      protection once CI is green
- [ ] CHANGELOG Phase 1 completion entry (README status flips
      at phase *start* and already shows Phase 1)
- [ ] **Decision:** promote structural ADRs
      (0001/0002/0005/0008/0009) to `Accepted` now that code
      commits to them?

### Explicitly deferred

- Orleans wiring and grains → Phase 2 (the host is plain
  ASP.NET Core in Phase 1)
- Any persistence — Table Storage, EF Core, migrations →
  Phase 3
- SignalR endpoints → Phase 2. (The legacy standalone
  `Microsoft.AspNetCore.SignalR 1.2.0` pin has been removed —
  in .NET 10 SignalR ships in the shared framework, so no
  package is needed.)
- Auth (JWT bearer + Identity) → Phase 6
- Domain logic and DTOs beyond placeholders → Phases 2–6

### Definition of done

`dotnet build` and `dotnet test` are green locally and in CI;
the host runs and answers `/health`; the full project graph
exists with ADR-correct references; README and CHANGELOG are
updated.
