# ADR-0013: Bot difficulty — an Easy bot is weakened on entry

- **Status:** Accepted (2026-11-22)
- **Date:** 2026-11-22
- **Deciders:** aha (solo maintainer)

## Context

The audience of [ADR-0007] arrives alone and plays against the
bot for a few minutes. The bot of [ADR-0012] makes the best move
it can see every round, and the species are tuned so that every
one of them wins about half its battles. A visitor who has never
played meets an opponent that plays well from the first round,
and loses about as often as they win.

A visitor should be able to ask for an easier battle, and win
it more often, without the game becoming a different one.
Two things are fixed:

- The combat rules and the tuned numbers are held by the
  balance guard rails of [ADR-0012]. An easier battle must not
  change either, or the balance work would have to be redone.
- The Phase 8 opponent replaces the bot's rule
  ([ADR-0007]). A difficulty that lives inside the rule would
  have to be rebuilt then.

## Decision

A battle against the bot has a **difficulty**, `Normal` or
`Easy`. Normal is the default and plays exactly as before.

- **Easy weakens the bot's monsters once, as they enter the
  battle:** full hit points and attack at **75 %** of the
  species' values, in whole numbers rounded half up and never
  below 1 (`EasyHandicap` in `Battles/Domain`). Defense,
  speed, element and abilities stay as they are. The monster
  enters at its weakened full health, and heals count against
  it.
- **Nothing else changes.** The round rules (`RoundResolver`,
  `DamageRule`, `MoveRule`, `ElementChart`), the bot's rule
  (`BotStrategy`) and the Catalog's numbers never see a
  difficulty. The battle stores the weakened stats like any
  others, and its monster grains get the same ones.
- **Only a bot side has a difficulty.** The bot's
  `ParticipantSetup` carries it; the grain weakens a side only
  when it is a bot on Easy, whatever a human side says. A
  battle between humans has no difficulty. A host waiting for
  a human chooses it when they let the bot take the seat
  instead.
- **The battle remembers it:** the live state, every battle
  response and the cold store's snapshot carry the
  difficulty, so a client shows it during and after the
  battle.
- **A win is a win.** The player stats count a win on Easy
  like any other, and the battle history does not mark it.
- **Easy lies outside the balance guard rails** on purpose.
  The simulator of [ADR-0012] measures the rules on the
  species' own numbers; Easy is a deliberate unfair advantage
  for the human and is not measured.

## Consequences

- **Positive:**
  - A visitor who wants an easier battle gets one with one
    tap, and the rules a battle is played by are the same.
  - The handicap is one pure rule applied in one place, so it
    is tested on its own and cannot leak into a Normal battle.
  - The balance report and its guard rails stay valid without
    a rerun; the Phase 8 opponent inherits Easy unchanged,
    since the handicap is in the monsters, not in the rule.
- **Negative:**
  - Nobody measures how much easier Easy is. 75 % is a
    judgement, not a number the simulator found.
  - The difficulty is stored format: a grain-contract member,
    a value in the battle state and a column in the cold store
    (`CONTRIBUTING.md`). The Domain keeps its own copy of the
    enum for the cold store (`BattleDifficulty`): the
    architecture test of [ADR-0009] keeps the Domain free of
    everything under `Battles.Grains`, the grain contracts
    included.
- **Neutral / follow-ups:**
  - A harder difficulty would be another member and another
    handicap, or a change to the rule; the second would need
    the guard rails of [ADR-0012].

## Alternatives considered

- **A weaker bot rule** (more random moves, a worse choice
  of target) — rejected: it lives in the rule the Phase 8
  opponent replaces, and an erratic bot reads as broken, not
  as easy.
- **Stronger monsters for the human** — rejected: the same
  effect seen from the other side, but the visitor's own
  numbers would no longer be the ones the team picker shows.
- **Scaling damage in `DamageRule`** — rejected: it puts a
  difficulty into the rules the guard rails hold, and every
  hit would need to know who dealt it.
- **A difficulty for battles between humans** — rejected:
  there is no bot to weaken, and weakening one human would be
  a handicap match, a different feature.

## References

- [ADR-0007]: the game modes and the bot opponent
- [ADR-0009]: the layering test that keeps the grain
  contracts' enum out of the Domain
- [ADR-0012]: the combat rules and the balance guard rails
  Easy stays outside of
- `docs/balance.md`: the balance report

[ADR-0007]: 0007-turn-based.md
[ADR-0009]: 0009-module-internal-structure.md
[ADR-0012]: 0012-combat-rules.md
