# Project Roadmap

Phased delivery plan for Orleans Monster Arena. Each phase is
one line; the current phase is expanded into a checklist with a
definition of done. The ADRs in [`adr/`](adr/) are the
authoritative source for *architecture* — this file only
sequences the work.

Finished phases are not kept here. What they delivered is in
the [`CHANGELOG`](../CHANGELOG.md); their checklists are in
this file's history ([Phases 1–3][full-roadmap],
[Phase 4.5][roadmap-4.5], [Phase 4][roadmap-4]).

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
- **Phase 4 — Frontend** ✅ done
  SvelteKit + Svelte 5 runes on `adapter-node`, with a Kiota
  client generated from the committed OpenAPI document
  ([ADR-0004](adr/0004-sveltekit-frontend.md)). A visitor
  picks a nickname, an arena and a team, and fights the bot
  (Normal or Easy, [ADR-0013](adr/0013-bot-difficulty.md))
  or waits for a human, round by round over SignalR; a
  dashboard shows each player's record. Vitest and
  Playwright run in CI.
- **Phase 4.5 — Combat depth** ✅ done
  Elements, three abilities per monster with cooldowns, status
  effects, six species, a bot with a team of its own, and a
  balance simulator whose guard rails hold the numbers
  ([ADR-0012](adr/0012-combat-rules.md)). Inserted while
  Phase 4 was under way, so the rest of the frontend is built
  on the final battle model; numbered 4.5 so the later phases
  keep the numbers the ADRs refer to.
- **Phase 5 — Scale-out** ⬅ next
  Multi-silo on Azure Container Apps, cluster membership over
  Table Storage, a SignalR backplane so round deltas reach
  watchers connected to another silo, a migration bundle for
  the cold-path schemas.
- **Phase 6 — Auth & accounts**
  JWT bearer auth + ASP.NET Core Identity (cold-path EF store),
  player accounts, protected SvelteKit routes
  ([ADR-0004](adr/0004-sveltekit-frontend.md)). No login wall:
  guest play (nickname, vs bot) keeps working without an
  account; accounts add to it, they never gate it.
- **Phase 7 — Observability**
  OTEL (wired since Phase 1) → Grafana stack
  (Alloy/Tempo/Loki/Prometheus); retire NLog.
- **Phase 8 — AI opponents**
  Per-turn Claude API call inside the round budget replaces
  the Phase 2 rule-based bot strategy
  ([ADR-0007](adr/0007-turn-based.md)).

Phases 5+ are indicative and will be detailed when reached.
No phase is current: Phase 4 is closed, and Phase 5 gets its
checklist when it starts.

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
- Deploying the frontend, a second Container App
  ([ADR-0004](adr/0004-sveltekit-frontend.md)), with the
  backend's first deployment.

**Phase 6 — Auth & accounts**

- Player accounts, auth and protected frontend routes;
  player ids stay client-chosen strings until then, so a
  nickname is not protected.

**No phase yet**

- The `frontend` and `e2e` jobs as required checks on `main`,
  decided in Phase 4. The ruleset still requires only
  `build-test` and `integration`; `e2e` is added once it has
  run green on `main`.
- Monster portraits: one per species in the style of the
  arena pictures, with the inline SVG as the fallback.
  Deferred: the drawings hold up for now.
- A sweep for hot rows a crash left behind between the snapshot
  write and the clear ([ADR-0003](adr/0003-no-outbox.md)).
- Arena rules such as "fire monsters only"
  ([ADR-0007](adr/0007-turn-based.md)). Species carry an
  element since Phase 4.5, so a rule now has something to
  check; the rule itself is still open.
- An arena bonus for its own element (fire in the volcano,
  and so on), so arenas differ in play as well as in
  looks. A rules change: it needs
  [ADR-0012](adr/0012-combat-rules.md) and the balance
  guard rails.
- A rule that ends a battle two players keep alive by only
  guarding and healing
  ([ADR-0012](adr/0012-combat-rules.md)). The turn deadline
  ends a battle nobody plays, not one played without end.
- Event-storming notes (paper, photo in the README), pending
  since Phase 0.

[full-roadmap]: https://github.com/space-craft-collab/CombatSimulation/blob/54da1cd2d0c3e3772e18a4766cfff9ca939b3087/docs/ROADMAP.md
[roadmap-4.5]: https://github.com/space-craft-collab/CombatSimulation/blob/9cb41b8d7615ea00348d290a291b887d9ad4d0ee/docs/ROADMAP.md
[roadmap-4]: https://github.com/space-craft-collab/CombatSimulation/blob/419df4f54797e449d592fd1b94c7803d41d7f904/docs/ROADMAP.md
