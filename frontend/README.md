# Frontend

The SvelteKit frontend of Orleans Monster Arena
([ADR-0004](../docs/adr/0004-sveltekit-frontend.md)): Svelte 5
runes, TypeScript, Tailwind CSS, `adapter-node`.

Node and npm are not installed on the host. Every command runs
in the container that `compose.yaml` describes.

## Run it

The AppHost has to be up first, see the
[repository README](../README.md).

```bash
docker compose run --rm frontend npm ci   # once, and after a lockfile change
docker compose up                         # dev server on http://localhost:5173
```

## Check it

```bash
docker compose run --rm frontend npm run lint
docker compose run --rm frontend npm run check
docker compose run --rm frontend npm test
docker compose run --rm e2e               # Playwright; needs the dev server and the AppHost
```

`../scripts/play-start.sh` starts the dev server and the AppHost,
with SQL Server and Azurite behind it. The CI job `e2e` runs the
tests the same way: that script, then `docker compose run --rm e2e`.
On CI a failed test is retried once (and reported as flaky if the
retry passes); its trace lands in `test-results/`.

## Configuration

| Variable           | Meaning                                                                  |
| ------------------ | ------------------------------------------------------------------------ |
| `PUBLIC_API_URL`   | Where the browser reaches the AppHost: the HTTP API and the SignalR hub. |
| `API_URL_INTERNAL` | Where this server reaches the AppHost while rendering, if that differs.  |

Both are read when the server starts (`src/env.ts`), so one
image serves every environment. The AppHost must list the
frontend's origin under `Cors:AllowedOrigins`.

## Layout

- `src/lib/api/` — the Kiota client, generated from
  `docs/api/openapi.json` by `scripts/generate-api-client.sh`.
  Never edited by hand.
- `src/lib/arena/` — the hand-written layer over it: the client
  factory, strict types, the SignalR hub's messages, and the
  logic behind the screens (`battle.ts`, `battle-log.ts`,
  `lobby.ts`, `verdict.ts`, `dashboard.ts` and more, pure and
  unit-tested).
- `src/lib/components/` — shared components, among them the
  monsters as inline SVG.
- `src/routes/` — the screens: arenas, team picker with the
  open battles, live battle, player dashboard.
- `e2e/` — Playwright tests.

## Dependencies

Adding or bumping a package follows the rules in
[`CONTRIBUTING.md`](../CONTRIBUTING.md#frontend-dependencies-npm):
exact versions, a week's cooldown, no install scripts, and a
malware check of the lockfile before anything is installed.
