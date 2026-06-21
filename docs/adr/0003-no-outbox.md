# ADR-0003: No transactional outbox for hot→cold handoff

- **Status:** Proposed
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

When a battle completes, the `LiveBattleGrain` must

1. write a snapshot to Azure SQL (cold path via EF Core), and
2. deactivate itself.

The grain's state lives in Azure Table Storage (hot path); the
snapshot lives in Azure SQL — two storage systems, so there is
no single transaction across them. A textbook fix would be a
**transactional outbox**: write `BattleCompleted` to an outbox
table inside the cold-path SQL transaction, then ship it via a
worker. That guarantees at-least-once delivery even if the silo
crashes mid-handoff.

The repo is a showcase, not a payments system. The "lost"
event would mean a completed battle whose snapshot is missing
from the cold store — recoverable from logs / grain state
history, not a money problem.

## Decision

**Skip the outbox.** The grain calls `IBattleSnapshotWriter`
synchronously on completion, then deactivates. If the silo
crashes between the cold-path write and grain deactivation, the
snapshot is at-most-once.

This trade-off is documented in this ADR so reviewers see it
was a conscious choice, not an oversight.

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

## References

- Chris Richardson, "Pattern: Transactional outbox"
- [ADR-0005]: Service interfaces — `IBattleSnapshotWriter` is a
  Battles-internal abstraction (the grain calls it within the
  module), not a cross-module contract.

[ADR-0005]: 0005-inter-module-services.md
