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
- **Phase 1 — Walking skeleton** ✅ done
  Compilable, runnable, CI-green solution that establishes the
  full project graph. No domain logic yet.
- **Phase 2 — Orleans embedded** ⬅ current
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

## Phase 2 — Orleans embedded

**Goal:** the silo runs inside the web host and the Battles
module drives a full turn-based battle through grains. Web-side
code reaches grains through the local `IClusterClient`; round
progress leaves the process as SignalR deltas. Still no
persistence — grain state is in memory until Phase 3.

### Checklist

- [x] `AppHost` references `Microsoft.Orleans.Server` and calls
      `UseOrleans()` — localhost clustering plus in-memory grain
      storage ([ADR-0002](adr/0002-orleans-cohosted.md))
- [x] `Battles/Grains/` folder created
      ([ADR-0009](adr/0009-module-internal-structure.md)) with a
      boot-probe grain (`IPingGrain` / `PingGrain`)
- [x] Smoke test: web-side `IClusterClient`, resolved from the
      host's DI, answers a grain call in the same process — the
      concrete proof of ADR-0002
- [x] Architecture guard extended: a module's `Domain` must not
      depend on `Grains` either
- [ ] Grain interfaces `IArenaGrain`, `ILiveBattleGrain`,
      `IMonsterInstanceGrain` in `Battles.Grains.Abstractions`
      ([ADR-0005](adr/0005-inter-module-services.md))
- [ ] Grain implementations in `Battles/Grains/`, with the
      `Created → InProgress → Completed` lifecycle from
      [ADR-0007](adr/0007-turn-based.md)
- [ ] Round loop: an Orleans **Reminder** for the 60s player-turn
      deadline, a grain **Timer** for the bot-vs-bot loop
      ([ADR-0007](adr/0007-turn-based.md)). Needs a new
      `Microsoft.Orleans.Reminders` pin plus
      `UseInMemoryReminderService()` — `Microsoft.Orleans.Server`
      does not carry reminders, and the Table Storage provider
      only arrives in Phase 3.
- [ ] Battles feature slices that start a battle and submit a
      turn; `IClusterClient` stays inside `Battles`, other modules
      never see Orleans types
      ([ADR-0005](adr/0005-inter-module-services.md))
- [ ] SignalR hub streaming `BattleEventDto` round deltas
      ([ADR-0007](adr/0007-turn-based.md))
- [ ] Retire the boot-probe grain once the real grains carry the
      smoke test
- [ ] **Decision:** promote [ADR-0001](adr/0001-modular-monolith.md)
      and [ADR-0002](adr/0002-orleans-cohosted.md) to `Accepted`
      once the silo and real grains are in place — the open item
      carried over from Phase 1
- [ ] CHANGELOG Phase 2 completion entry (README status flips at
      phase *start* and already shows Phase 2)

### Explicitly deferred

- Azure Table Storage grain state and reminders, EF Core cold
  store → Phase 3. Phase 2 stays on in-memory providers.
- Frontend → Phase 4; SignalR is exercised from tests until then.
- Auth (JWT bearer + Identity) → Phase 6.
- Multi-silo clustering → Phase 5. `UseLocalhostClustering()` is
  deliberately single-silo.

### Definition of done

`dotnet build` and `dotnet test` are green locally and in CI; the
host boots a silo; a battle runs end to end through grains and
emits round deltas over SignalR; README and CHANGELOG are
updated.

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
- [x] Add the `dotnet test` status-check gate to branch
      protection once CI is green — ruleset active on `main`
      (required check `build-test`, admin bypass)
- [x] CHANGELOG Phase 1 completion entry (README status flips
      at phase *start* and already shows Phase 1)
- [x] **Decision:** promote structural ADRs to `Accepted` now
      that code commits to them — **0005**, **0008** and
      **0009** are now `Accepted`: the solution graph builds
      exactly that structure and ADR-0009 is enforced by the
      NetArchTest guard. **0001** and **0002** stay `Proposed`
      — no silo runs yet, so the code only half commits to
      co-hosting; revisit at the end of Phase 2.

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
