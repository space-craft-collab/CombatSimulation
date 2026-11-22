# Contributing

This is a solo **showcase** project built to
demonstrate Microsoft Orleans and modular-monolith
patterns. It is not looking for feature
contributions, but issues and discussion are
very welcome.

## Good things to open an issue about

- A bug in the code or docs
- An architecture decision you think is wrong —
  see [`docs/adr/`](docs/adr/) and argue with it
- A question about why something is built this way

## If you do want to send a PR

Keep it small and focused. Larger changes are
likely to be declined simply because they cut
across the roadmap.

## Build and test

```bash
dotnet restore
dotnet build --configuration Release
dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests
```

Tests use xUnit v3, whose test projects are
self-hosting executables — run them with
`dotnet run`, not `dotnet test`.

The tests need a running Docker daemon: one SQL
Server container (Testcontainers) serves the whole
run, since the host does not start without its
cold-path database. Tests with the trait
`Category=Integration` are slow end-to-end scenarios
that start further containers of their own. To skip
them, or to run only them, as the CI jobs do:

```bash
dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests -- -trait- "Category=Integration"
dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests -- -trait "Category=Integration"
```

The container images are pinned in `compose.yaml`;
the tests read them from there, so a version is
changed in that one place.

Coverage is measured by wrapping the run in
`dotnet-coverage` (coverlet's collector only works
under `dotnet test`); `reportgenerator` turns the
result into HTML under `coverage/report`:

```bash
dotnet tool restore
dotnet dotnet-coverage collect \
  --settings tests/coverage.settings.xml \
  --output coverage/fast.cobertura.xml --output-format cobertura \
  -- dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests
dotnet reportgenerator -reports:coverage/fast.cobertura.xml \
  -targetdir:coverage/report -reporttypes:Html
```

The SDK version is pinned in `global.json`.

The frontend has its own checks, all run in a
container; see [Frontend](#frontend).

## Ground rules for code

Everything is written down in
[`CLAUDE.md`](CLAUDE.md) — it is the coding
standard, not just an AI prompt. Highlights:

- .NET 10, `LangVersion=latest`, nullable enabled
- File-scoped namespaces, Allman braces, 4 spaces
- XML doc comments on all public types and members
- English code and comments
- One type per file
- xUnit v3, `Method_Scenario_ExpectedResult` names
- No test-only interfaces — inject a `Func<>` or
  `Action<>` instead (ADR-0010)
- Separate read and write repositories (CQRS-style)

Warnings are errors. `dotnet build` must be clean
before you push.

## Grain state is a stored format

Grain state is persisted in Table Storage with the
Orleans binary serializer, which knows members only
by their `[Id]` number. Existing data must stay
readable, so these values are **frozen**:

- **`[Id(n)]` numbers** on `[GenerateSerializer]`
  types: never renumber, never reuse. A new member
  takes the next free number; a removed member's
  number stays retired (leave a comment).
- **Enum values** stored in state (`BattleStatus`)
  carry explicit numbers. Never renumber; add new
  members at the end with the next number.
- **`[Alias("...")]`** on every serializable type
  keeps its identity independent of the CLR name, so
  a type may be renamed or moved, but the alias
  string never changes.
- **`[GrainType("...")]`** on every grain class
  keys its stored state and reminders. Never change
  it, even when the class is renamed.

`StoredFormatTests` pins the enum values and grain
types.

## Database migrations

Each module owns a `DbContext` in its own SQL
schema, with its migrations in
`Infrastructure/Migrations` and its own history
table. The host applies pending migrations on
startup in Development. To add one (the `dotnet-ef`
version is pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Catalog \
  --startup-project src/Catalog --output-dir Infrastructure/Migrations
```

Never edit a migration that is already on `main`;
add a new one instead.

Id columns (arena, species, player and participant
ids) take `.UseCollation(IdCollation.Ordinal)`, so
SQL Server compares them exactly, like the grain
keys. Changing the collation of a key column needs
the primary key dropped and re-added around the
`AlterColumn`; EF does not generate that.

## API contract and client

The host serves its OpenAPI document at
`/openapi/v1.json`. A copy is committed as
`docs/api/openapi.json`, and the frontend's Kiota
client under `frontend/src/lib/api` is generated from
that copy
([ADR-0004](docs/adr/0004-sveltekit-frontend.md)).
`OpenApiDocumentTests` fails when an endpoint change
makes the two differ. To refresh both:

```bash
UPDATE_OPENAPI=1 dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests \
  -- -class "OrleansMonsterArena.Tests.OpenApiDocumentTests"
scripts/generate-api-client.sh
```

Commit the document and the client together; CI
regenerates the client and fails when it differs.
The generated files are never edited by hand. The
SignalR hub is not part of the document: its
messages are typed by hand in
`frontend/src/lib/arena/battle-hub.ts`.

## Balance report

`docs/balance.md` is written by a simulation that plays
the bot against itself on the pure combat rules
(`Battles/Domain`), with the species the Catalog seeds:
every one-on-one and every team of three against every
other. `BalanceReportTests` fails when a change to the
rules or to the seed gives other numbers than the
committed report. To refresh it:

```bash
UPDATE_BALANCE=1 dotnet run --configuration Release --project tests/OrleansMonsterArena.Tests \
  -- -class "OrleansMonsterArena.Tests.Balance.BalanceReportTests"
```

Commit the report with the change that caused it, and
read the diff: it shows what the change did to every
matchup.

## Frontend

Node and npm run in containers only; nothing is
installed on the host
([ADR-0004](docs/adr/0004-sveltekit-frontend.md)).
The commands are in
[`frontend/README.md`](frontend/README.md); all of
them go through `frontend/compose.yaml`:

```bash
cd frontend
docker compose run --rm frontend npm ci
docker compose up
```

## Frontend dependencies (npm)

`frontend/.npmrc` enforces three rules:

- Every package is pinned to an exact version (no
  `^` or `~` ranges), and `package-lock.json` is
  committed. Images and CI install with `npm ci`.
- A version must be a week old before npm resolves
  it (`min-release-age`). Malware in a fresh release
  is usually found within days.
- No package runs a script while it is installed
  (`ignore-scripts`).

Before a package is added or bumped, the lockfile is
checked for known malware, and only then installed:

```bash
cd frontend
docker compose run --rm frontend npm install --package-lock-only <package>
docker compose run --rm frontend node scripts/check-malware.mjs
docker compose run --rm frontend npm ci
```

`check-malware.mjs` asks the GitHub Advisory Database
about every package in the lockfile, the transitive
ones included, and exits with 1 on a hit. CI runs it
before it installs anything.

For a new direct dependency, also look at its npm
page: the exact name (typos are a common trap) and
who publishes it. A Dependabot PR for npm gets the
same check before it is merged.

The Playwright image in `frontend/compose.yaml` must
have the version of `@playwright/test`; bump the two
together.

## Architecture changes

Anything that changes structure needs an ADR in
[`docs/adr/`](docs/adr/), using
[`0000-template.md`](docs/adr/0000-template.md).
ADRs start as `Proposed` and are edited in place;
once `Accepted`, a changed decision requires a
superseding ADR. A factual correction that leaves the
decision unchanged goes into the ADR's `## Corrections`
section (see the ADR conventions).
