# ADR-0004: SvelteKit + Svelte 5 for the frontend

- **Status:** Proposed
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

Use **SvelteKit with Svelte 5 runes** as a separate deployable.
The exact adapter is **not locked yet** — `adapter-auto` /
`-node` is the leading candidate because the app needs JWT auth,
SignalR, and SSR for protected routes. The lock-in happens at
the start of Phase 4 after a 1-day spike.

Settled at the start of Phase 4 (2026-11-21):

- **Toolchain in containers only.** Node and npm are not
  installed on the host; scaffolding, the dev server, builds
  and tests all run in Docker, which also pins the Node
  version for this project.
- **npm dependencies are pinned to exact versions**, and each
  package is checked for known malware before it is
  installed. The steps are in `CONTRIBUTING.md`.
- **API client: Kiota, with no fallback generator.**
- **Styling: Tailwind CSS.** Monsters are drawn as inline
  SVG; the repository carries no image assets.
- **Player identity until Phase 6:** a nickname the visitor
  types on the first visit, kept in `localStorage`. It is
  the client-chosen player id the API already takes, so
  anyone can use anyone's name until auth exists.
- **Tests:** Vitest for logic, and one Playwright smoke test
  that plays a battle against the bot.

## Consequences

- **Positive:**
  - Smallest bundle of the three options → cold-start on Azure
    Static Web Apps stays low.
  - Runes give a clean reactive model for the SignalR stream
    that powers `LiveBattleView`.
  - Demonstrates polyglot stack experience on the CV — useful
    contrast against the .NET backend.
- **Negative:**
  - Smaller hiring pool than React in the DACH market. Not a
    problem for a solo showcase, would matter for a team.
  - Svelte 5 runes are still fresh — third-party components may
    lag behind. Mitigated by writing the few components we need
    by hand.
- **Neutral / follow-ups:**
  - **Open:** SvelteKit adapter (`auto` vs `node` vs `static`)
    — decided in Phase 4 after the spike.
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
    messages (`BattleEventDto`, `StrikeDto`) are typed by
    hand in the frontend.

## Alternatives considered

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
- SvelteKit adapter docs (to be linked once the adapter is
  locked in Phase 4).

[ADR-0002]: 0002-orleans-cohosted.md
