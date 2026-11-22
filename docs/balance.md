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
| Ember Fox | 0 % | 41 % |
| Frost Owl | 33 % | 54 % |
| Lava Crab | 100 % | 59 % |
| Moss Golem | 67 % | 46 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 0 % | 0 % | 0 % |
| **Frost Owl** | 100 % | – | 0 % | 0 % |
| **Lava Crab** | 100 % | 100 % | – | 100 % |
| **Moss Golem** | 100 % | 100 % | 0 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 4.0 | 5.0 | 7.0 |
| **Frost Owl** | 4.0 | – | 5.0 | 5.2 |
| **Lava Crab** | 5.0 | 5.0 | – | 12.0 |
| **Moss Golem** | 7.0 | 5.2 | 12.0 | – |

- Battles: 240
- Average length: 6.4 rounds
- Longest battle: 13 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 240
- Average length: 23.6 rounds
- Longest battle: 32 rounds
- Undecided after 100 rounds: 0

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 99 % |
| Ember Fox | `flame-dash` | 100 % |
| Frost Owl | `strike` | 61 % |
| Frost Owl | `ice-shard` | 60 % |
| Lava Crab | `strike` | 100 % |
| Lava Crab | `magma-claw` | 94 % |
| Moss Golem | `strike` | 99 % |
| Moss Golem | `root-slam` | 97 % |
