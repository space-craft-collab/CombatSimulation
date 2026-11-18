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

**Skip the outbox.** The grain calls `IBattleSnapshotWriter`
synchronously on completion, then clears its hot state and
deactivates (see below). If the silo crashes between the
cold-path write and grain deactivation, the snapshot is
at-most-once.

This trade-off is documented in this ADR so reviewers see it
was a conscious choice, not an oversight.

**Hot state lives only as long as the battle.** Table Storage
is the store for *live* battles; the cold store owns finished
ones. The completion sequence is:

1. Write the snapshot to the cold store.
2. On success, clear the hot state — the battle grain's own
   and its monster-instance grains' (`ClearStateAsync`).
3. Deactivate.

If step 1 throws, step 2 is skipped and the failure is logged:
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
  - A silo crash between `WriteSnapshotAsync` and
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
- [ADR-0005]: Service interfaces — `IBattleSnapshotWriter` is a
  Battles-internal abstraction (the grain calls it within the
  module), not a cross-module contract.

[ADR-0005]: 0005-inter-module-services.md
