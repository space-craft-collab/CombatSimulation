# ADR-0012: Combat rules — abilities with cooldowns, no dice

- **Status:** Proposed
- **Date:** 2026-11-22
- **Deciders:** aha (solo maintainer)

## Context

Up to Phase 4 a monster had one ability, `strike`, and a hit
dealt attack minus defense. That was enough to prove the round
loop ([ADR-0007]), but not to play: every round was the same
button. The first balance report (`docs/balance.md`, in the
history) put numbers on it. One on one the four species formed
a strict ladder, Lava Crab against Moss Golem took 60 rounds at
one damage per hit, teams of three often did not finish within
100 rounds, and speed almost never decided anything. The bot
mirrored the visitor's team, so no two different teams ever met.

Three forces shape the rules:

- The repository is a showcase of Orleans, not of game design.
  The rules must stay small enough to read in one sitting.
- A visitor plays for a few minutes. A battle has to offer a
  real choice every round and end after about ten of them.
- Phase 8 replaces the bot rule with a Claude call per turn
  ([ADR-0007]). A model plans far better over rules it can
  compute exactly.

## Decision

### Where the rules live

The rules of a round are a pure function in `Battles/Domain`
(`RoundResolver`): the fighters and the round's actions go in,
the events and the fighters afterwards come out. It knows no
grain, no storage and no clock. `LiveBattleGrain` keeps the
state, the deadlines and the broadcast, and applies every
change in hit points to the monster grain, which stays their
authority. The balance simulator plays on the same function.

### No randomness

Nothing in a battle is rolled: no critical hits, no misses, no
damage spread. The same actions always give the same round.
Variety comes from the choices, not from dice.

### Damage

`damage = attack × power × 10 / (10 + defense) × element`,
rounded to the nearest whole point and never below 1.

- Defense scales a hit down instead of being subtracted:
  defense 10 halves it, and no amount of defense stops it.
- `power` is the ability's strength in percent of the attack;
  the basic attack has 100.
- `element` is ×1.5 when the ability's element beats the
  target's, ×0.75 when it loses to it, ×1 otherwise.
- The arithmetic is done in whole numbers.

### Elements

Fire beats Nature, Nature beats Frost, Frost beats Fire. Every
species has one of the three; every ability has one of them or
is neutral. Only the ability's element counts for a hit, not
the attacker's: the basic `strike` is neutral for everyone.

### Abilities and cooldowns

A species knows three abilities: the basic `strike`, an
elemental attack and a utility. Abilities are Catalog data
(power, element, cooldown, effect), not code; the Battles
module copies them into the battle when it starts, so a
running battle never changes under a Catalog edit.

An ability with a cooldown of *n* cannot be used in the *n*
rounds after the one it was used in. Cooldowns count per
monster and run down every round, whether the monster acts or
not. There is no shared resource such as energy or mana.

One side still makes one move per round ([ADR-0007]): it picks
one of its monsters, one of that monster's ready abilities,
and a target.

### Effects

An ability can carry one effect:

- **Burn** — the target loses a fixed number of hit points at
  the end of each round.
- **Guard** — the monster takes a percentage less damage from
  hits.
- **Slow** — the target's speed drops by a percentage, so it
  acts later in the round.
- **Heal** — the monster restores a percentage of its full hit
  points at once.

Burn and slow ride on an attack and land on its target; guard
and heal are used by a monster on itself. An effect lasts a
number of rounds, the one it was applied in included. Effects
of the same kind do not stack; applying one again replaces it.

### Order of a round

1. The actions, fastest monster first (ties go to the lower
   slot). An action whose actor or target fell earlier in the
   round is dropped and its cooldown is not spent.
2. Burn, in slot order.
3. Durations and cooldowns run down by one.

The battle ends the moment only one side is left standing,
also in the middle of step 2.

### Balance is measured

`docs/balance.md` is written by a simulator that plays the bot
against itself on the pure rules. Tests hold the guard rails:
no species wins more than 60 % or less than 40 % of its
battles, a battle lasts 5 to 12 rounds on average and never
more than 30, and every ability is used. A change to a number
shows its effect on every matchup in the diff of the report.

## Consequences

- **Positive:**
  - A round is a decision: which monster, which ability, which
    target, and whether to spend a cooldown now.
  - The rules are unit-tested without a silo and simulated by
    the thousand in milliseconds.
  - Deterministic rounds keep the tests exact and let the
    Phase 8 opponent look ahead.
  - Balance changes are reviewed as numbers, not as opinions.
- **Negative:**
  - No dice means no lucky comeback; a lost position stays
    lost. Acceptable for battles of ten rounds.
  - The battle state grows by cooldowns and effects, all of it
    stored format (`CONTRIBUTING.md`).
  - Two players who only guard and heal could keep a battle
    going for a long time. No rule ends such a battle; the
    turn deadline of [ADR-0007] still ends one nobody plays.
- **Neutral / follow-ups:**
  - Arena rules such as "fire monsters only" ([ADR-0007]) now
    have the species attribute they need.
  - New effects are new code in the resolver; new abilities
    with the existing effects are Catalog rows.

## Alternatives considered

- **Critical hits and misses** — rejected: seeded randomness
  in the grain, the simulator and every test, and a harder
  planning problem for the Phase 8 opponent, for a thrill the
  cooldowns already give.
- **An energy pool instead of cooldowns** — rejected: more to
  show in the UI and more to tune; a cooldown is one counter
  per ability and understood at a glance.
- **Keeping attack minus defense** — rejected: it floors at
  one damage as soon as defense reaches attack, which is what
  produced the 60-round battle.
- **`attack / (attack + defense)` as the scale** — rejected:
  it flattens the difference between a strong and a weak
  attacker; with the defense-only scale damage stays linear in
  attack.
- **Abilities as code in the Battles module** — rejected: the
  Catalog already owns the reference data of a species, and
  the frontend reads the abilities from the same place.
- **Every monster acts every round** — rejected: it would
  redo the action model of [ADR-0007] and triple what a
  visitor has to decide per round on a phone.

## References

- [ADR-0003]: the round boundary stays a consistent state
- [ADR-0007]: the turn-based round loop these rules run in
- `docs/balance.md`: the current balance report
- `CONTRIBUTING.md`: how to refresh the report

[ADR-0003]: 0003-no-outbox.md
[ADR-0007]: 0007-turn-based.md
