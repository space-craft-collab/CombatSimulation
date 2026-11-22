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
| Ember Fox | 42 % | 45 % |
| Frost Owl | 17 % | 43 % |
| Glacier Yak | 60 % | 52 % |
| Lava Crab | 74 % | 53 % |
| Moss Golem | 68 % | 61 % |
| Thorn Lynx | 40 % | 47 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 18 % | 0 % | 0 % | 93 % | 100 % |
| **Frost Owl** | 83 % | – | 0 % | 0 % | 0 % | 0 % |
| **Glacier Yak** | 100 % | 100 % | – | 100 % | 0 % | 0 % |
| **Lava Crab** | 100 % | 100 % | 0 % | – | 68 % | 100 % |
| **Moss Golem** | 8 % | 100 % | 100 % | 33 % | – | 100 % |
| **Thorn Lynx** | 0 % | 100 % | 100 % | 0 % | 0 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 3.8 | 5.2 | 5.4 | 7.4 | 3.1 |
| **Frost Owl** | 3.8 | – | 5.5 | 5.2 | 5.6 | 2.0 |
| **Glacier Yak** | 5.2 | 5.5 | – | 10.1 | 15.1 | 6.2 |
| **Lava Crab** | 5.4 | 5.2 | 10.1 | – | 15.5 | 5.5 |
| **Moss Golem** | 7.4 | 5.6 | 15.1 | 15.5 | – | 8.5 |
| **Thorn Lynx** | 3.1 | 2.0 | 6.2 | 5.5 | 8.5 | – |

- Battles: 600
- Average length: 6.9 rounds
- Longest battle: 20 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 7600
- Average length: 25.5 rounds
- Longest battle: 69 rounds
- Undecided after 100 rounds: 0

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 97 % |
| Ember Fox | `flame-dash` | 100 % |
| Ember Fox | `ember-bite` | 47 % |
| Frost Owl | `strike` | 37 % |
| Frost Owl | `ice-shard` | 41 % |
| Frost Owl | `chill-wind` | 24 % |
| Glacier Yak | `strike` | 98 % |
| Glacier Yak | `avalanche` | 95 % |
| Glacier Yak | `thick-hide` | 51 % |
| Lava Crab | `strike` | 99 % |
| Lava Crab | `magma-claw` | 97 % |
| Lava Crab | `shell-up` | 35 % |
| Moss Golem | `strike` | 97 % |
| Moss Golem | `root-slam` | 97 % |
| Moss Golem | `regrow` | 70 % |
| Thorn Lynx | `strike` | 86 % |
| Thorn Lynx | `vine-whip` | 72 % |
| Thorn Lynx | `pounce` | 96 % |
