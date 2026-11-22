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
- **Phase 4 — Frontend** ⏸ paused
  SvelteKit + Svelte 5 runes, Kiota client; adapter spike
  ([ADR-0004](adr/0004-sveltekit-frontend.md)). The solo path
  is playable; the rest resumes after Phase 4.5.
- **Phase 4.5 — Combat depth** ⬅ current
  Elements, three abilities per monster with cooldowns, status
  effects, more species, a bot with a team of its own, and a
  balance simulator that guards the numbers
  ([ADR-0012](adr/0012-combat-rules.md)). Numbered 4.5 so the
  later phases keep the numbers the ADRs refer to.
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

## Phase 4.5 — Combat depth

**Goal:** a battle is a string of decisions instead of the
same button every round. Monsters differ by element and by
their abilities, the bot fields a team of its own, and a
simulator shows that no species or ability dominates.
Inserted before the rest of Phase 4, so waiting for a human
and the player dashboard are built once, on the final battle
model.

**Where it started:** one ability (`strike`), damage as
attack minus defense. In a one-on-one the four species formed
a strict ladder (Moss Golem > Lava Crab > Ember Fox > Frost
Owl), Lava Crab against Moss Golem ran about 60 rounds at one
damage per hit, speed almost never mattered, and the bot
mirrored the visitor's team.

### Checklist

**Rules**

- [x] The round logic leaves `LiveBattleGrain` for a pure
      class in `Battles/Domain`: fighters and actions in,
      events and the new state out. No new behaviour; the
      grain keeps the state, the deadlines and the monster
      grains
- [x] Elements on the species (Fire > Nature > Frost > Fire:
      ×1.5 with the advantage, ×0.75 against it) and a damage
      rule in which defense scales damage down instead of
      being subtracted, so no matchup ends at one damage per
      hit
      (the triangle is in place; it starts to matter with the
      first elemental ability)
- [ ] Abilities as Catalog data, three per species: the
      basic `strike`, an elemental attack and a utility.
      Cooldowns per monster; an action is checked for
      "belongs to the species" and "ready"
- [ ] Status effects: burn, guard, slow, heal. The round
      delta carries general round events instead of strikes,
      and the monster grain can restore hit points
- [ ] Two more species, so every element has a fast and a
      sturdy monster
- [ ] **Decision:** the combat rules as
      [ADR-0012](adr/0012-combat-rules.md): no randomness,
      cooldowns instead of an energy pool

**Bot**

- [ ] The bot picks a team of its own instead of mirroring
      the visitor's
- [ ] It chooses abilities by a rule: finish a monster it can
      fell, use the element advantage, heal when low

**Balance**

- [x] A simulator plays bot against bot on the pure rules,
      without Orleans: every one-on-one and every team of
      three against every other
- [x] Its first report records the starting point above
- [ ] Guard rails as tests: every species wins between 40 %
      and 60 % of its battles; no one-on-one is worse than
      30/70 unless the element decides it; a battle lasts 5
      to 12 rounds on average and never more than 30; every
      ability is used in at least 10 % of the battles its
      monster fights
- [ ] Stats and abilities tuned until the guard rails hold

**Frontend**

- [ ] Ability buttons with their cooldowns; element badges;
      status effects on the monster cards; the battle log
      names the ability, the element advantage and the
      effects
- [ ] The team picker shows each species' element and
      abilities
- [ ] Drawings for the new species

**Wrap-up**

- [ ] [ADR-0007](adr/0007-turn-based.md) brought in line: the
      bot rule and what an action is
- [ ] OpenAPI document and Kiota client regenerated
- [ ] CHANGELOG Phase 4.5 completion entry

### Definition of done

A visitor fields a team, meets a different team, and chooses
between three abilities per monster every round; the balance
guard rails are green in CI; the frontend shows abilities,
cooldowns, elements and effects; ADR, README and CHANGELOG
are updated.

## Phase 4 — Frontend

**Paused:** resumes with "Then" below once Phase 4.5 is
done.

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
  ([ADR-0007](adr/0007-turn-based.md)): species carry no
  element or type yet, so a rule would have nothing to check.
  They arrive together with the species attribute they need.
- Event-storming notes (paper, photo in the README), pending
  since Phase 0.

[full-roadmap]: https://github.com/space-craft-collab/CombatSimulation/blob/54da1cd2d0c3e3772e18a4766cfff9ca939b3087/docs/ROADMAP.md
