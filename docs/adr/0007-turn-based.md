# ADR-0007: Turn-based interaction model

- **Status:** Accepted
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

The domain is monster battles. The interaction model is open:

1. **Turn-based** — each round is one message; players (or bots)
   submit actions, the grain resolves a round, broadcasts a
   delta, waits for the next input.
2. **Real-time** — a fixed tick rate, server-authoritative
   simulation pushed to clients many times per second.

Orleans grains are single-threaded by design. Each grain
processes one message at a time, in FIFO order. That model maps
cleanly onto turn-based: "one round = one message". Real-time
would require either driving a tight tick loop from a grain
timer (which then becomes the only thing the grain ever does),
or moving the simulation out of grains entirely — at which
point the Orleans story collapses.

The repository is explicitly a **showcase of Orleans**, not a
showcase of netcode.

## Decision

The combat model is **turn-based**:

- `LiveBattleGrain` states: `Created` → `InProgress` →
  `Completed`.
- In `InProgress`, the grain consumes one action per actor per
  round and emits a `BattleEventDto` round delta over SignalR.
- For player turns, an Orleans **Reminder** drives the
  "60-second deadline → auto-resolve" path (1-minute Reminder
  minimum is acceptable for this UX).
- For bot-vs-bot fast battles, a Grain **Timer** drives the
  loop at sub-second cadence (no persistence required between
  ticks because the next tick re-enters the same grain).

## Consequences

- **Positive:**
  - Single-threaded grain model and turn-based gameplay fit
    each other — the simplest possible mental model.
  - The Phase 8 AI layer gets a **per-turn** latency budget
    (seconds) instead of a per-frame one (ms). That makes a
    Claude API call inside a turn realistic.
  - State at every round boundary is consistent and snapshot-
    able — fits the hot→cold handoff in [ADR-0003].
- **Negative:**
  - No real-time "feel" — the live battle view is animated
    deltas, not a continuous simulation. Acceptable for a
    showcase, sub-optimal for an action game.
- **Neutral / follow-ups:**
  - Reminder vs Timer is a runtime decision based on the
    battle's current state; not a configuration knob.
  - If the project later needs real-time, the grain layer
    is the wrong tool — that would be a separate service.

## Alternatives considered

- **Real-time tick loop in a grain** — rejected: would
  monopolise the grain and hide the actor-model story under
  a netcode story.
- **Real-time in a non-grain service, Orleans only for
  matchmaking** — rejected: makes Orleans a side character in
  its own showcase.

## References

- [ADR-0001]: Modular monolith
- [ADR-0002]: Orleans co-hosted
- [ADR-0003]: No outbox — references per-round consistency
- Orleans docs: Reminders vs Timers

[ADR-0001]: 0001-modular-monolith.md
[ADR-0002]: 0002-orleans-cohosted.md
[ADR-0003]: 0003-no-outbox.md
