# Project Roadmap

Phased delivery plan for Orleans Monster Arena. Each phase is
one line; the current phase is expanded into a checklist with a
definition of done. The ADRs in [`adr/`](adr/) are the
authoritative source for *architecture* — this file only
sequences the work.

Finished phases are not kept here. What they delivered is in
the [`CHANGELOG`](../CHANGELOG.md); their checklists are in
[this file's history][full-roadmap].

## Phases at a glance

- **Phase 0 — Foundations & ADRs** ✅ done
  Repo scaffolding, coding standards, build/package config,
  ADRs 0001–0009, architecture diagrams.
- **Phase 1 — Walking skeleton** ✅ done
  Compilable, runnable, CI-green solution that establishes the
  full project graph. No domain logic yet.
- **Phase 2 — Orleans embedded** ✅ done
  Co-hosted silo ([ADR-0002](adr/0002-orleans-cohosted.md)),
  first grains (`IArenaGrain`, `ILiveBattleGrain`,
  `IMonsterInstanceGrain`), rule-based bot opponent and
  turn-based round loop ([ADR-0007](adr/0007-turn-based.md)),
  SignalR round deltas.
- **Phase 3 — Persistence (hot/cold)** ✅ done
  Azure Table Storage grain state + EF Core cold store, hot→cold
  snapshot bridge ([ADR-0003](adr/0003-no-outbox.md)).
  Completes the MVP.
- **Phase 4 — Frontend** ⬅ next
  SvelteKit + Svelte 5 runes, Kiota client; adapter spike
  ([ADR-0004](adr/0004-sveltekit-frontend.md)).
- **Phase 5 — Scale-out**
  Multi-silo on Azure Container Apps, cluster membership over
  Table Storage, a SignalR backplane so round deltas reach
  watchers connected to another silo, a migration bundle for
  the cold-path schemas.
- **Phase 6 — Auth & accounts**
  JWT bearer auth + ASP.NET Core Identity (cold-path EF store),
  player accounts, protected SvelteKit routes
  ([ADR-0004](adr/0004-sveltekit-frontend.md)).
- **Phase 7 — Observability**
  OTEL (wired since Phase 1) → Grafana stack
  (Alloy/Tempo/Loki/Prometheus); retire NLog.
- **Phase 8 — AI opponents**
  Per-turn Claude API call inside the round budget replaces
  the Phase 2 rule-based bot strategy
  ([ADR-0007](adr/0007-turn-based.md)).

Phases 5+ are indicative and will be detailed when reached.

## Phase 4 — Frontend

Not started. Its checklist is written when the phase begins;
the first item is the one-day SvelteKit adapter spike
([ADR-0004](adr/0004-sveltekit-frontend.md)).

## Carried over

Deferred by the finished phases and still open.

**Phase 5 — Scale-out**

- Cluster membership on Table Storage and several silos.
  Clustering stays `UseLocalhostClustering()` until then.
- A SignalR backplane
  ([ADR-0002](adr/0002-orleans-cohosted.md)).
- Azure resources (Storage account, Azure SQL) and the first
  deployment. So far everything runs against the local
  emulators.
- The migration bundle. Until then the host applies migrations
  on startup, in Development only.

**Phase 6 — Auth & accounts**

- Player accounts and auth; player ids stay client-chosen
  strings until then.

**No phase yet**

- A sweep for hot rows a crash left behind between the snapshot
  write and the clear ([ADR-0003](adr/0003-no-outbox.md)).
- Arena rules such as "fire monsters only"
  ([ADR-0007](adr/0007-turn-based.md)): species carry no
  element or type yet, so a rule would have nothing to check.
  They arrive together with the species attribute they need.
- Event-storming notes (paper, photo in the README), pending
  since Phase 0.

[full-roadmap]: https://github.com/space-craft-collab/CombatSimulation/blob/54da1cd2d0c3e3772e18a4766cfff9ca939b3087/docs/ROADMAP.md
