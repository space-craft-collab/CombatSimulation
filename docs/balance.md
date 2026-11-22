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
| Ember Fox | 33 % | 36 % |
| Frost Owl | 0 % | 34 % |
| Lava Crab | 67 % | 42 % |
| Moss Golem | 100 % | 54 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 100 % | 0 % | 0 % |
| **Frost Owl** | 0 % | – | 0 % | 0 % |
| **Lava Crab** | 100 % | 100 % | – | 0 % |
| **Moss Golem** | 100 % | 100 % | 100 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Lava Crab | Moss Golem |
|---|---:|---:|---:|---:|
| **Ember Fox** | – | 4.0 | 7.0 | 10.0 |
| **Frost Owl** | 4.0 | – | 7.0 | 12.0 |
| **Lava Crab** | 7.0 | 7.0 | – | 60.0 |
| **Moss Golem** | 10.0 | 12.0 | 60.0 | – |

- Battles: 240
- Average length: 16.7 rounds
- Longest battle: 60 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 240
- Average length: 79.3 rounds
- Longest battle: 100 rounds
- Undecided after 100 rounds: 40

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 100 % |
| Frost Owl | `strike` | 69 % |
| Lava Crab | `strike` | 100 % |
| Moss Golem | `strike` | 100 % |
