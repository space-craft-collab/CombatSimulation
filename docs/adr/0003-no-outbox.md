# ADR-0003: No transactional outbox for hot→cold handoff

- **Status:** Proposed
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

When a battle completes, the `LiveBattleGrain` must

1. write a snapshot to Azure SQL (cold path via EF Core),
2. clear its hot state, and
3. deactivate itself.

The grain's state lives in Azure Table Storage (hot path); the
snapshot lives in Azure SQL — two storage systems, so there is
no single transaction across them. A textbook fix would be a
**transactional outbox**: write `BattleCompleted` to an outbox
table inside the cold-path SQL transaction, then ship it via a
worker. That guarantees at-least-once delivery even if the silo
crashes mid-handoff.

Azure Table Storage has no time-to-live: a row stays until
someone deletes it, and Orleans does not delete grain state on
deactivation. Without an explicit clear, every battle would
leave its battle row and one row per monster instance behind
for good.

The repo is a showcase, not a payments system. The "lost"
event would mean a completed battle whose snapshot is missing
from the cold store — recoverable from logs / grain state
history, not a money problem.

## Decision

**Skip the outbox.** The grain writes the snapshot
synchronously on completion, reports the result to
`IPlayerStatsService`, then clears its hot state and
deactivates (see below). If the silo crashes between the
cold-path write and grain deactivation, the snapshot is
at-most-once.

This trade-off is documented in this ADR so reviewers see it
was a conscious choice, not an oversight.

**The writer is a concrete class, not an interface.** The grain
injects `BattleSnapshotWriteRepository` from the module's
`Infrastructure/` folder directly. There is one implementation
and no second one in sight, and the grain tests run against a
real SQL Server anyway ([ADR-0011]), so an
`IBattleSnapshotWriter` would be the test-only interface
[ADR-0010] rules out. The one path a stub would serve, a failed
write, is tested against SQL Server too: the test stores a
snapshot under the battle's id first, so the grain's insert
fails on the primary key. That is why the write is
deliberately **not idempotent** — a duplicate id is an error,
not a no-op; with no replay job there is no second legitimate
write to absorb.

**Hot state lives only as long as the battle.** Table Storage
is the store for *live* battles; the cold store owns finished
ones. The completion sequence is:

1. Write the snapshot to the cold store, then report a decided
   battle to `IPlayerStatsService` (bots are not counted; a
   human's win or loss against a bot is).
2. On success of both, clear the hot state — the battle
   grain's own and its monster-instance grains'
   (`ClearStateAsync`).
3. Deactivate.

If step 1 throws, the rest is skipped and the failure is logged:
the final result then stays in the hot state, which is what a
replay would read. A cleared grain reads as `Created`, the same
as an id that never existed, so:

- the arena's battle list drops `Created` entries as well as
  `Completed` ones, and
- `GET /battles/{id}` falls back to the cold store for a
  completed battle; it returns 404 only when neither store
  knows the id.

## Consequences

- **Positive:**
  - One fewer table, one fewer worker, one fewer thing to test.
  - Hot→cold path is a single straight-line call — easy to
    read in a code review.
- **Negative:**
  - A silo crash between the snapshot write and
    `DeactivateOnIdle()` can lose the cold-path write. The
    grain state still has the final result, so a manual replay
    job could recover it — not built.
  - Not suitable as-is for a workload where missing the cold
    write has business consequences.
  - A crash between the snapshot write and the clear leaves the
    battle's hot rows behind. They are harmless — the cold
    store already has the result — but nothing sweeps them up;
    a cleanup job is not built.
  - Reading one battle now spans two stores (hot first, cold as
    the fallback).
- **Neutral / follow-ups:**
  - If the project ever evolves into something where lost
    snapshots matter, add an outbox in the `Battles` module
    only — no other module is affected.

## Alternatives considered

- **Transactional outbox** — rejected: full implementation
  (outbox table, dispatcher, dedup, retry policy, idempotency
  keys) costs days for a benefit the showcase doesn't need.
- **Write snapshot first, then grain state** — rejected: shifts
  the failure window but doesn't remove it; harder to reason
  about than the straight-line version.
- **Keep the hot state forever** — rejected: Table Storage has
  no TTL, so the battle and monster rows would grow without
  bound and duplicate what the cold store already holds.
- **Clear after a retention period** (a per-battle reminder,
  e.g. one hour after completion) — rejected: one more reminder
  per battle for a window the cold-store fallback already
  covers.

## References

- Chris Richardson, "Pattern: Transactional outbox"
- [ADR-0005]: Service interfaces — the snapshot writer is
  Battles-internal (the grain calls it within the module); the
  cross-module call is `IPlayerStatsService`.
- [ADR-0010]: Delegate test seams — why the writer has no
  interface.
- [ADR-0011]: SQL Server required — the tests write to a real
  cold store, which makes the failure path testable without a
  stub.

[ADR-0005]: 0005-inter-module-services.md
[ADR-0010]: 0010-delegate-test-seams.md
[ADR-0011]: 0011-sql-server-required.md
