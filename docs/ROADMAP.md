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
- **Phase 4 — Frontend** ⬅ current
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

**Goal:** a visitor opens the site, types a nickname, picks
an arena and a team, and plays a battle against the bot in
the browser, with every round arriving live over SignalR.
The frontend is a separate SvelteKit app; it reaches the
host through a Kiota-generated client
([ADR-0004](adr/0004-sveltekit-frontend.md)).

### Checklist

**Toolchain and spike**

- [ ] `frontend/` scaffolded inside a container (SvelteKit,
      Svelte 5 runes, TypeScript): a multi-stage `Dockerfile`
      and a dev compose service on a pinned Node LTS image.
      The container runs as uid 1000 and reaches the host
      through `host.docker.internal`; no Node or npm on the
      host
- [ ] npm rules in place from the first install: exact
      versions (`save-exact`), a committed lockfile, `npm ci`
      in images and CI, and the malware check before every
      new package (`CONTRIBUTING.md`)
- [ ] **Decision:** the one-day adapter spike —
      `adapter-node` (the favourite, since the app runs in a
      container anyway) against `static` and `auto`. It also
      settles how the browser reaches the API and the hub
      (directly, with CORS, or through the SvelteKit server)
      and where the frontend will be hosted. The result goes
      into ADR-0004

**Backend for the frontend**

- [ ] OpenAPI document served by the host
      (`Microsoft.AspNetCore.OpenApi`, pinned but not
      referenced yet); the endpoints declare their response
      and error types, which they do not today
- [ ] `GET /species` in Catalog: the team picker needs the
      list, and no endpoint returns it yet
- [ ] CORS for the frontend's origin, if the spike has the
      browser call the API directly
- [ ] Kiota as a pinned `dotnet` tool; the generated
      TypeScript client is checked in under `frontend/`,
      because the Node image has no .NET SDK to generate it.
      CI regenerates it and fails when it differs

**Solo path — first**

- [ ] Nickname prompt on the first visit, kept in
      `localStorage`; it is the player id the API takes
      (no auth before Phase 6)
- [ ] Arena browser (`GET /arenas`)
- [ ] Team picker: one to three species, then start a
      battle; **vs bot** is the default
- [ ] Live battle view: `Watch` on `/hubs/battles`,
      `RoundResolved` deltas applied to rune state, choosing
      an actor and a target, the turn deadline as a
      countdown, the result at the end. A reload picks the
      battle up again from `GET /battles/{id}`. The hub's
      message types are written by hand; Kiota covers HTTP
      only

**Then**

- [ ] **Wait for a human:** open a battle, list an arena's
      open battles, join one; the "play vs bot instead" offer
      shows immediately while waiting
      ([ADR-0007](adr/0007-turn-based.md)), and
      `BattleStarted` moves the waiting host into the battle
- [ ] Player dashboard: stats and the last battles
      (`GET /players/{id}`, `GET /players/{id}/battles`); a
      player without a decided battle sees an empty state,
      not an error

**Look**

- [ ] Tailwind CSS; monsters as inline SVG, no image assets
- [ ] Usable on a phone-width screen

**Tests and CI**

- [ ] Vitest for the logic, above all applying round deltas
      to the battle state
- [ ] One Playwright smoke test: the solo path against a
      running host, from nickname to result
- [ ] CI job for the frontend (type check, lint, Vitest,
      build), all in the container image
- [ ] **Decision:** whether the frontend job and the
      Playwright test become required checks on `main`
- [ ] Dependabot watches `frontend/` (npm) and its
      `Dockerfile`

**Wrap-up**

- [ ] README: quickstart including the frontend, and a
      screenshot of a battle
- [ ] **Decision:** promote
      [ADR-0004](adr/0004-sveltekit-frontend.md) to `Accepted`
      once the frontend is built on the locked adapter
- [ ] CHANGELOG Phase 4 completion entry

### Explicitly deferred

- Deploying the frontend → Phase 5, with the backend's first
  deployment. The spike only names the target.
- Login, accounts and protected routes → Phase 6. Until then
  a nickname is not protected.

### Definition of done

The frontend builds and its tests are green locally (in
containers) and in CI; with `docker compose up` and
`dotnet run`, a visitor plays a full battle against the bot
in the browser and sees each round live; waiting for a human
and the player dashboard work; README and CHANGELOG are
updated.

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
