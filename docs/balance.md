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
| Ember Fox | 52 % | 52 % |
| Frost Owl | 49 % | 45 % |
| Glacier Yak | 47 % | 44 % |
| Lava Crab | 52 % | 51 % |
| Moss Golem | 47 % | 51 % |
| Thorn Lynx | 53 % | 56 % |

## One on one

Wins of the row species against the column species.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 0 % | 10 % | 50 % | 100 % | 100 % |
| **Frost Owl** | 100 % | – | 45 % | 100 % | 0 % | 0 % |
| **Glacier Yak** | 90 % | 55 % | – | 90 % | 0 % | 0 % |
| **Lava Crab** | 50 % | 0 % | 10 % | – | 100 % | 100 % |
| **Moss Golem** | 0 % | 100 % | 100 % | 0 % | – | 35 % |
| **Thorn Lynx** | 0 % | 100 % | 100 % | 0 % | 65 % | – |

Average length in rounds.

| | Ember Fox | Frost Owl | Glacier Yak | Lava Crab | Moss Golem | Thorn Lynx |
|---|---:|---:|---:|---:|---:|---:|
| **Ember Fox** | – | 2.0 | 4.1 | 4.1 | 4.1 | 2.0 |
| **Frost Owl** | 2.0 | – | 4.4 | 4.3 | 4.1 | 1.1 |
| **Glacier Yak** | 4.1 | 4.4 | – | 9.6 | 9.6 | 3.0 |
| **Lava Crab** | 4.1 | 4.3 | 9.6 | – | 8.2 | 3.4 |
| **Moss Golem** | 4.1 | 4.1 | 9.6 | 8.2 | – | 4.1 |
| **Thorn Lynx** | 2.0 | 1.1 | 3.0 | 3.4 | 4.1 | – |

- Battles: 600
- Average length: 4.5 rounds
- Longest battle: 12 rounds
- Undecided after 100 rounds: 0

## Teams of 3

- Battles: 7600
- Average length: 13.0 rounds
- Longest battle: 26 rounds
- Undecided after 100 rounds: 0

## Ability use

Share of a species' appearances in which it used the
ability at least once, over all battles above.

| Species | Ability | Used in |
|---|---|---:|
| Ember Fox | `strike` | 61 % |
| Ember Fox | `flame-dash` | 77 % |
| Ember Fox | `ember-bite` | 58 % |
| Frost Owl | `strike` | 45 % |
| Frost Owl | `ice-shard` | 63 % |
| Frost Owl | `chill-wind` | 24 % |
| Glacier Yak | `strike` | 85 % |
| Glacier Yak | `avalanche` | 94 % |
| Glacier Yak | `thick-hide` | 65 % |
| Lava Crab | `strike` | 76 % |
| Lava Crab | `magma-claw` | 94 % |
| Lava Crab | `shell-up` | 64 % |
| Moss Golem | `strike` | 80 % |
| Moss Golem | `root-slam` | 96 % |
| Moss Golem | `regrow` | 66 % |
| Thorn Lynx | `strike` | 13 % |
| Thorn Lynx | `vine-whip` | 29 % |
| Thorn Lynx | `pounce` | 77 % |
