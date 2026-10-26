# ADR-0007: Turn-based interaction model

- **Status:** Proposed
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

Its audience — a recruiter or a business stakeholder — usually
arrives **alone** and wants to play for a few minutes. A battle
that needs a second human is unplayable for them, so a bot
opponent is part of the core loop, not a late extra.

## Decision

The combat model is **turn-based**:

- `LiveBattleGrain` states: `Created` → `WaitingForPlayers`
  → `InProgress` → `Completed`. `WaitingForPlayers` is only
  entered by battles that wait for a human opponent.
- In `InProgress`, the grain consumes one action per actor per
  round and emits a `BattleEventDto` round delta over SignalR.
- **Bot turns run inline.** Once every human side has acted,
  the grain asks each bot for its action and resolves the
  round in the same call. Human vs bot therefore needs no
  timer at all.
- For player turns, an Orleans **Reminder** drives the
  "60-second deadline → auto-resolve" path (1-minute Reminder
  minimum is acceptable for this UX). It also ends battles a
  visitor simply walked away from.
  - The deadline is the reminder's **due time**, re-armed
    each round; the 1-minute minimum only bounds its retry
    period.
  - Humans who miss the deadline lose that round's move;
    the round resolves with the actions that arrived.
  - If **no human** acted before the deadline, the battle
    is abandoned: `Completed` without a winner.
- For bot-vs-bot fast battles, a Grain **Timer** drives the
  loop at sub-second cadence (no persistence required between
  ticks because the next tick re-enters the same grain).

### Game modes

A visitor picks one of two modes:

- **Play vs bot** — the default and most prominent path. The
  battle is created with a bot participant and starts
  `InProgress` immediately.
- **Wait for a human** — the battle is created with one human
  and sits in `WaitingForPlayers`, listed by its arena. A
  second human joining moves it to `InProgress`. While
  waiting, the **"play vs bot instead"** offer is shown
  **immediately**, not after a timeout: accepting it adds a
  bot through the same join path and the battle starts.

### Bot strategy

The Phase 2 bot is a **simple rule-based strategy** in
`Battles/Domain/` (e.g. "attack the weakest standing
opponent"): a pure decision over the battle's current state,
easy to unit-test. Phase 8 swaps that decision for a Claude
API call inside the per-turn budget; the grain-side flow
stays the same.

### Arenas and battles

Think of a map in a multiplayer shooter: the map is a
template, and many matches run on it at once.

- **Arena** — the template (e.g. "Volcano"): name plus
  optional rules such as "fire monsters only". It is rarely
  changing reference data, so it lives in the **Catalog**
  module next to the monster species. No grain.
- **Battle** — one running match on an arena. An arena can
  host any number of battles at the same time. Each battle is
  one `ILiveBattleGrain` (Guid `BattleId`).
- **Participants** — a battle has n players (or bots), each
  fielding n monsters. Every monster in play is one
  `IMonsterInstanceGrain` (key `"{BattleId}:{Slot}"`). The
  model keeps participants as a list; the Phase 2 demo runs
  **1 vs 1** to keep the round logic simple.
- **`IArenaGrain`** — the server browser for one arena: one
  grain **per arena**, keyed by the Catalog arena id. It
  lists the arena's open and running battles and creates new
  ones. Load therefore spreads across arenas instead of
  funnelling through one global lobby grain.

## Consequences

- **Positive:**
  - Single-threaded grain model and turn-based gameplay fit
    each other — the simplest possible mental model.
  - A solo visitor can always play: a bot opponent exists
    from Phase 2 on, and waiting never blocks them.
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
