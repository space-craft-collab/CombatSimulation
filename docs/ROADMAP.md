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
- **Phase 4.5 — Combat depth** ✅ done
  Elements, three abilities per monster with cooldowns, status
  effects, six species, a bot with a team of its own, and a
  balance simulator whose guard rails hold the numbers
  ([ADR-0012](adr/0012-combat-rules.md)). Inserted while
  Phase 4 was under way, so the rest of the frontend is built
  on the final battle model; numbered 4.5 so the later phases
  keep the numbers the ADRs refer to.
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

- [x] `frontend/` scaffolded inside a container (SvelteKit 3,
      Svelte 5 runes, TypeScript, Tailwind CSS): a multi-stage
      `Dockerfile` on a pinned Node 24 LTS image and a dev
      service in `frontend/compose.yaml`. The container runs as
      uid 1000 and shares the host's network, so it reaches the
      AppHost on `localhost` and that host need not listen
      beyond loopback; no Node or npm on the host
- [x] npm rules in place from the first install
      (`CONTRIBUTING.md`): exact versions, a committed
      lockfile, `npm ci`, no install scripts, a week's
      cooldown for new releases, and
      `scripts/check-malware.mjs`, which checks the lockfile
      against the GitHub Advisory Database before an install
- [x] **Decision:** the adapter spike — **`adapter-node`**
      ([ADR-0004](adr/0004-sveltekit-frontend.md)). The
      browser calls the API and the hub directly (CORS); the
      SvelteKit server reaches the same API under an internal
      URL while it renders. Proven in a real browser against
      the dev server and the production image. The frontend
      will be a second Container App, not a static site

**Backend for the frontend**

- [x] OpenAPI document served at `/openapi/v1.json`
      (`Microsoft.AspNetCore.OpenApi`) and committed as
      `docs/api/openapi.json`; a test fails when the two
      differ. The typed results already described the success
      responses; added were the 400/409 problem responses, a
      tag per module, and strict JSON numbers (the web default
      typed every integer as "integer or string")
- [x] `GET /species` in Catalog, for the team picker
- [x] CORS for the frontend's origins (`Cors:AllowedOrigins`),
      without credentials
- [x] Kiota as a pinned `dotnet` tool; the TypeScript client
      is generated from `docs/api/openapi.json` by
      `scripts/generate-api-client.sh` and checked in under
      `frontend/src/lib/api`, because the Node image has no
      .NET SDK. CI regenerates it and fails when it differs
- [x] `launchSettings.json` for the AppHost: a plain
      `dotnet run` started it as Production, where neither
      `appsettings.Development.json` nor the migrations apply

**Solo path — first**

- [x] Nickname prompt on the first visit, kept in
      `localStorage`; it is the player id the API takes
      (no auth before Phase 6). Checked against the API's
      rules for a player id before it is used
- [x] Arena browser (`GET /arenas`)
- [x] Team picker: one to three species, then start a
      battle; **vs bot** is the default
- [x] Live battle view: `Watch` on `/hubs/battles`,
      `RoundResolved` deltas applied to rune state, choosing
      an actor and a target, the turn deadline as a
      countdown, the result at the end. A reload picks the
      battle up again from `GET /battles/{id}`, a finished
      one from the cold store. The hub's message types are
      written by hand; Kiota covers HTTP only. Someone who
      is not a participant watches without controls

**Combat depth**

- [x] Phase 4.5: abilities, cooldowns, elements, effects and
      the bot's own team in the battle view and the team
      picker (see the [`CHANGELOG`](../CHANGELOG.md))

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

- [x] Tailwind CSS; monsters as inline SVG, no image assets
- [x] Arena images, first: one per arena (volcano, glacier,
      moss grove), 16:9 WebP around 1600 px with a calm
      centre. Used as the tile on the start page, a banner
      on the team picker, and a dimmed backdrop behind the
      battle view (`ArenaBackdrop`). Arenas differed only
      by name before
- [ ] Monster portraits, once the arena images hold up:
      one per species, in the same style, WebP around
      512 px under `src/lib/assets`. `MonsterSprite` keeps
      `data-species`, the CSS for defeated monsters, and the
      SVG blob as the fallback for a species without a picture
- [x] Badges, health bars, icons and effects stay SVG/CSS.
      Pictures are AI-generated (Nano Banana), and the
      README says so
- [x] **Decision:** amend
      [ADR-0004](adr/0004-sveltekit-frontend.md): arena
      pictures ship with the frontend, mapped by arena id
- [x] Usable on a phone-width screen: the solo path was
      checked at 390 px; a battle of three against three
      fits one screen with the strike button in reach

**Tests and CI**

- [x] Vitest for the logic: applying round deltas to the
      battle state, telling old and missing rounds apart,
      the outcome, the countdown, the nickname rules
- [x] One Playwright smoke test: the solo path against a
      running host, from the nickname to the result,
      including a reload in mid-battle and one after it
      (`docker compose run --rm e2e`)
- [x] CI job `frontend`: malware check, install, lint, type
      check, Vitest and build, all in the container image
- [ ] Playwright in CI, once the smoke test exists
- [ ] **Decision:** whether the frontend job and the
      Playwright test become required checks on `main`
- [x] Dependabot watches `frontend/` (npm, with the same
      week's cooldown) and its `Dockerfile`

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
