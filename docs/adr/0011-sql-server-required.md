# ADR-0011: Require SQL Server for the cold path, in tests too

- **Status:** Accepted (2026-11-19)
- **Date:** 2026-11-19
- **Deciders:** aha (solo maintainer)

## Context

Phase 3 moves the cold path (the Catalog, player stats, battle
snapshots) into SQL Server through EF Core: one `DbContext` per
module ([ADR-0001]), each with its own schema, on the EF Core
base in `Shared.Infrastructure` ([ADR-0008]). The Catalog is on
the critical path of every battle: starting one looks up the
species of each monster. Almost every host test starts a battle,
so a decision is needed on what the host does when no `Sql`
connection string is set. That covers the test suite and a bare
`dotnet run`.

The hot path already has a fallback. Without a Table Storage
connection string, grain state and reminders use Orleans' own
in-memory providers. Those are the framework's providers, not
hand-written fakes, and grain persistence is not what most tests
are about. The restart test covers Table Storage on Testcontainers
Azurite.

## Decision

**SQL Server is required. Without the `Sql` connection string
the host does not start, and there is no in-memory or seed
fallback for the cold path.**

Tests run against a real SQL Server. One Testcontainers SQL
Server serves the whole test run as an xUnit v3 assembly
fixture. Every booted host gets a database of its own on it and
migrates it on startup, exactly as a local Development run does.

## Consequences

- **Positive:**
  - There is one code path. Every read and write that ships is
    the one the tests run, through the same migrations and the
    same provider.
  - A broken model, a missing migration or a pending model change
    fails the required `build-test` check, because EF Core refuses
    to migrate. It no longer waits for a slow integration job.
  - There is no fake to maintain next to each repository, and no
    test-only abstraction ([ADR-0010]).
- **Negative:**
  - Docker is needed to run the tests locally and to run the host
    (`docker compose up -d`).
  - Every test run pays for one SQL Server container start, about
    10–15 s. The fast suite still finishes in about 25 s.
  - The CI runners pull the SQL Server image on every run.
- **Neutral / follow-ups:**
  - The hot path keeps its in-memory default. The asymmetry is
    deliberate, for the reasons given in the context above.
  - The test trait `Category=Containers` became misleading,
    since every test now uses a container. It is renamed
    `Category=Integration`. It marks the slow end-to-end
    scenarios that start containers of their own, and CI runs
    them in the non-required `integration` job.
  - Tests that write to the cold store keep their data apart by
    database (one per host), or by unique ids within one.

## Alternatives considered

- **Seed fallback** (the Catalog serves its seed from code, and
  stats and history are not stored) — rejected. It needs a second
  implementation of every read, and the real SQL path would be
  tested only in the slow integration job.
- **SQLite in memory** — rejected. It is a different provider, so
  the SQL Server migrations do not run on it. It would need
  `EnsureCreated`, and its behaviour differs, for example in
  types, collation and concurrency.
- **EF Core InMemory provider** — rejected. It is not relational,
  and Microsoft advises against it for testing. Queries that work
  there can fail on SQL Server.

## References

- [ADR-0001]: Modular monolith — no shared `DbContext`.
- [ADR-0008]: Shared layer split — the EF Core base lives in
  `Shared.Infrastructure`.
- [ADR-0010]: Delegate test seams — no test-only abstractions.
- Microsoft Learn, "Choosing a testing strategy" (EF Core) —
  testing against the production database system.

## Corrections

- **2026-11-21:** the context and the reference list credited
  "one schema per module" to [ADR-0001] and [ADR-0008]. ADR-0001
  only rules out a shared `DbContext`, and ADR-0008 does not
  mention schemas; the schema per module is stated here. The
  decision itself is unchanged.
- **2026-11-21:** the consequences call `integration` a
  non-required job. That was true when this ADR was accepted;
  since this date the ruleset on `main` requires `integration`
  as well as `build-test`. The decision itself is unchanged.

[ADR-0001]: 0001-modular-monolith.md
[ADR-0008]: 0008-shared-layer-split.md
[ADR-0010]: 0010-delegate-test-seams.md
