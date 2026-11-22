# Balance report

Written by `BalanceReportTests` from a simulation on the
pure combat rules; never edited by hand. After a change
to the rules or to the Catalog seed, refresh it:

```bash
UPDATE_BALANCE=1 dotnet run --project tests/OrleansMonsterArena.Tests \
  -- -class "OrleansMonsterArena.Tests.Balance.BalanceReportTests"
```

**How it is measured:** every pair of different species
fights one on one, and every team of 3 different
species fights every other such team. Each matchup is
played 20 times in each seat order. Both sides follow
the bot rule and replace 20 % of their moves with
a random legal one; the random source is seeded per
battle, so a rerun gives the same numbers.

## Species

Share of its battles a species won.

| Species | One on one | In a team |
|---|---:|---:|
| Ember Fox | 37 % | 44 % |
| Frost Owl | 28 % | 51 % |
| Lava Crab | 89 % | 55 % |
| Moss Golem | 47 % | 49 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 18 % | 0 % | 93 % |
| **Frost Owl** | 83 % | – | 0 % | 0 % |
| **Lava Crab** | 100 % | 100 % | – | 68 % |
| **Moss Golem** | 8 % | 100 % | 33 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 3.8 | 5.4 | 7.4 |
| **Frost Owl** | 3.8 | – | 5.2 | 5.6 |
| **Lava Crab** | 5.4 | 5.2 | – | 15.5 |
| **Moss Golem** | 7.4 | 5.6 | 15.5 | – |

- Battles: 240
- Average length: 7.1 rounds
- Longest battle: 20 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 240
- Average length: 28.4 rounds
- Longest battle: 52 rounds
- Undecided after 100 rounds: 0

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 99 % |
| Ember Fox | `flame-dash` | 100 % |
| Ember Fox | `ember-bite` | 38 % |
| Frost Owl | `strike` | 57 % |
| Frost Owl | `ice-shard` | 58 % |
| Frost Owl | `chill-wind` | 43 % |
| Lava Crab | `strike` | 100 % |
| Lava Crab | `magma-claw` | 94 % |
| Lava Crab | `shell-up` | 39 % |
| Moss Golem | `strike` | 100 % |
| Moss Golem | `root-slam` | 95 % |
| Moss Golem | `regrow` | 69 % |
