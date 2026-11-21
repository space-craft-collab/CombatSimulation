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

The SDK version is pinned in `global.json`.

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

## Architecture changes

Anything that changes structure needs an ADR in
[`docs/adr/`](docs/adr/), using
[`0000-template.md`](docs/adr/0000-template.md).
ADRs start as `Proposed` and are edited in place;
once `Accepted`, a changed decision requires a
superseding ADR. A factual correction that leaves the
decision unchanged goes into the ADR's `## Corrections`
section (see the ADR conventions).
