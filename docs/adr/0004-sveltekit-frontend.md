# ADR-0004: SvelteKit + Svelte 5 for the frontend

- **Status:** Accepted (2026-11-23)
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

The showcase needs a visible demo (Phase 4): live battle view,
arena browser, player dashboard. Three realistic options:

1. **SvelteKit + Svelte 5 runes** — separate deployable, modern
   reactive model, small bundle.
2. **React + Vite** — largest ecosystem, deepest hiring pool,
   verbose for this surface area.
3. **Blazor (Server / WebAssembly)** — keeps the stack .NET-only,
   weaker visual story, harder to host cheaply.

The frontend is a means-to-end, not a UI competition — but it
should look polished enough that a recruiter spends 30 seconds
on the live demo before reading the README.

## Decision

Use **SvelteKit with Svelte 5 runes** as a separate deployable,
built with **`adapter-node`**: the frontend is a Node server in
its own container. The adapter was locked by the spike at the
start of Phase 4 (2026-11-21, see "Spike result"); the reasons
are server-side rendering now and, in Phase 6, a server that can
keep a session away from the browser.

How the three parts talk:

- The **browser** calls the AppHost directly, the HTTP API and
  the SignalR hub alike. The AppHost admits the frontend's
  origin through CORS (`Cors:AllowedOrigins`), without
  credentials.
- The **SvelteKit server** calls the same API while it renders
  a page. `load` functions use the public API URL everywhere;
  on the server a `handleFetch` hook redirects those calls to
  an internal URL where the two differ.
- Both URLs are read when the server starts (`PUBLIC_API_URL`,
  `API_URL_INTERNAL`), so one image serves every environment.

Settled at the start of Phase 4 (2026-11-21):

- **Toolchain in containers only.** Node and npm are not
  installed on the host; scaffolding, the dev server, builds
  and tests all run in Docker, which also pins the Node
  version for this project (24 LTS). The dev container shares
  the host's network, so it reaches the AppHost on
  `localhost` without that host listening beyond loopback.
- **npm dependencies are pinned to exact versions**, a
  release must be a week old, no package runs an install
  script, and the lockfile is checked for known malware
  before anything is installed. The steps are in
  `CONTRIBUTING.md`.
- **API client: Kiota, with no fallback generator.** The
  generated client is committed, because Kiota is a `dotnet`
  tool and the Node image has no .NET SDK; CI generates it
  again and fails when the two differ.
- **Styling: Tailwind CSS.** Monsters are drawn as inline
  SVG, and the layout is built for a phone-width screen
  first. Amended on 2026-11-22: each arena has a picture,
  AI-generated (Nano Banana) and stored as WebP under
  `frontend/src/lib/assets`. The pictures ship with the
  frontend and are mapped by arena id; the API does not
  serve them, and an arena without one stays plain.
  Badges, health bars and icons stay SVG/CSS. A portrait
  per species was planned to follow and deferred on
  2026-11-23: the inline-SVG monsters hold up next to the
  arena pictures.
- **Player identity until Phase 6:** a nickname the visitor
  types on the first visit, kept in `localStorage`. It is
  the client-chosen player id the API already takes, so
  anyone can use anyone's name until auth exists.
- **Tests:** Vitest for logic, and one Playwright smoke test
  that plays a battle against the bot.

Settled while the frontend was built (2026-11-23):

- **A nickname is part of an address.** The dashboard lives
  at `/players/{nickname}`, as the API's
  `GET /players/{playerId}` does. The API's routing leaves an
  escaped slash escaped, and browsers resolve a segment of
  `.` or `..` away, so the frontend refuses those nicknames
  when one is chosen. The API still takes them.
- **An arena's open battles are polled.** The hub pushes
  only to the watchers of one battle, so the team picker
  reads the list again every few seconds while the page is
  visible. A waiting host needs no polling: `BattleStarted`
  on the hub moves them into the battle.
- **Playwright covers each path, in CI too.** The smoke test
  grew into tests for every path a visitor takes: a battle
  against the bot, two players in an open battle, the
  dashboard. They run against the whole app, no mocked API,
  started as a developer starts it (`scripts/play-start.sh`),
  locally and in the CI job `e2e`.

## Consequences

- **Positive:**
  - Smallest bundle of the three options.
  - Runes give a clean reactive model for the SignalR stream
    that powers the live battle (`LiveBattle`).
  - Demonstrates polyglot stack experience on the CV — useful
    contrast against the .NET backend.
- **Negative:**
  - Smaller hiring pool than React in the DACH market. Not a
    problem for a solo showcase, would matter for a team.
  - Svelte 5 runes are still fresh — third-party components may
    lag behind. Mitigated by writing the few components we need
    by hand.
- **Neutral / follow-ups:**
  - `adapter-node` means a second container to host (Azure
    Container Apps, Phase 5) instead of a static site. A
    static build would be cheaper to host, but leaves no
    server to render pages or, later, to hold a session.
  - Kiota's TypeScript support is in **preview**: the
    generator says so, and its runtime packages
    (`@microsoft/kiota-*`) have only `1.0.0-preview` releases.
    They are pinned exactly, and a breaking change stays
    inside `frontend/src/lib/arena`.
  - OpenAPI → TS client: **Kiota**, maintained by Microsoft
    like the rest of the backend stack. Two generators were
    rejected. NSwag rests largely on a single maintainer and
    carries a long issue backlog, a risk for a repository
    meant to stay current. `@hey-api/openapi-ts` was the
    planned fallback for the case that Kiota's TypeScript
    output is too verbose; it was dropped on 2026-11-21
    because it is a community project with no large
    organisation behind it. Verbose output is hidden behind
    a thin hand-written wrapper instead.
  - Kiota covers the HTTP API only. The SignalR hub's
    messages (`RoundResolved`, `BattleStarted`) are typed by
    hand in the frontend.

## Spike result

Run on 2026-11-21 with SvelteKit 3, in a real browser
(Playwright, Chromium) against the dev server and against the
production image.

- `adapter-node` builds in the container and runs as a 357 MB
  image; the page arrives rendered with the Catalog's arenas
  and species.
- The browser starts a battle over HTTP, follows it on the hub
  and receives a round delta, all cross-origin.
- With a public API URL the server cannot reach, the page
  renders only when `API_URL_INTERNAL` is set: the redirect in
  `handleFetch` works.
- Kiota's client needed three things to live with SvelteKit,
  all in `frontend/src/lib/arena`:
  - SvelteKit hides the headers of responses fetched in
    `load`; `content-type` has to be let through.
  - Kiota's adapter looks at a response's `body` before it
    reads it, which the response SvelteKit observes during
    rendering allows only once; the client is handed a plain
    copy.
  - Kiota types every property as optional, whatever the
    OpenAPI document requires; the generated models are
    mapped onto strict types in one place.
- The client registers the JSON serializer only, instead of
  Kiota's default four.

## Alternatives considered

- **`adapter-static`** — rejected: the cheapest to host, but
  every page would be assembled in the browser, and Phase 6
  would have to keep its token there.

- **React + Vite** — rejected: more boilerplate for the few
  screens we need, no advantage in this scope.
- **Blazor Server** — rejected: ties the frontend lifecycle to
  the backend silo (already co-hosted with Orleans — see
  [ADR-0002]) and weakens the "polyglot stack" signal.
- **Blazor WASM** — rejected: bundle size, no clear advantage
  over SvelteKit for this surface area.

## References

- [ADR-0002]: Orleans co-hosted — the single backend process
  (web host + silo) this frontend talks to.
- [SvelteKit `adapter-node`](https://svelte.dev/docs/kit/adapter-node).
- [Kiota](https://learn.microsoft.com/openapi/kiota/overview).

[ADR-0002]: 0002-orleans-cohosted.md
