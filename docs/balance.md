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
| Ember Fox | 56 % | 38 % |
| Frost Owl | 2 % | 33 % |
| Glacier Yak | 60 % | 37 % |
| Lava Crab | 57 % | 35 % |
| Moss Golem | 85 % | 49 % |
| Thorn Lynx | 40 % | 38 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 90 % | 0 % | 13 % | 75 % | 100 % |
| **Frost Owl** | 10 % | – | 0 % | 0 % | 0 % | 0 % |
| **Glacier Yak** | 100 % | 100 % | – | 100 % | 0 % | 0 % |
| **Lava Crab** | 88 % | 100 % | 0 % | – | 0 % | 100 % |
| **Moss Golem** | 25 % | 100 % | 100 % | 100 % | – | 100 % |
| **Thorn Lynx** | 0 % | 100 % | 100 % | 0 % | 0 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 3.1 | 5.2 | 5.4 | 7.0 | 3.1 |
| **Frost Owl** | 3.1 | – | 5.5 | 5.9 | 5.6 | 2.0 |
| **Glacier Yak** | 5.2 | 5.5 | – | 13.3 | 18.6 | 6.9 |
| **Lava Crab** | 5.4 | 5.9 | 13.3 | – | 23.7 | 5.5 |
| **Moss Golem** | 7.0 | 5.6 | 18.6 | 23.7 | – | 9.0 |
| **Thorn Lynx** | 3.1 | 2.0 | 6.9 | 5.5 | 9.0 | – |

- Battles: 600
- Average length: 8.0 rounds
- Longest battle: 26 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 7600
- Average length: 42.8 rounds
- Longest battle: 100 rounds
- Undecided after 100 rounds: 1779

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 74 % |
| Ember Fox | `flame-dash` | 77 % |
| Ember Fox | `ember-bite` | 87 % |
| Frost Owl | `strike` | 44 % |
| Frost Owl | `ice-shard` | 79 % |
| Frost Owl | `chill-wind` | 46 % |
| Glacier Yak | `strike` | 98 % |
| Glacier Yak | `avalanche` | 100 % |
| Glacier Yak | `thick-hide` | 87 % |
| Lava Crab | `strike` | 96 % |
| Lava Crab | `magma-claw` | 97 % |
| Lava Crab | `shell-up` | 79 % |
| Moss Golem | `strike` | 98 % |
| Moss Golem | `root-slam` | 99 % |
| Moss Golem | `regrow` | 96 % |
| Thorn Lynx | `strike` | 50 % |
| Thorn Lynx | `vine-whip` | 54 % |
| Thorn Lynx | `pounce` | 89 % |
