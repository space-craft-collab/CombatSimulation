using Battles.Domain;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// How a simulated side decides: its id and the battle go in, its
/// move for the round comes out.
/// </summary>
/// <param name="participantId">The side that decides.</param>
/// <param name="fighters">Every fighter of the battle.</param>
/// <returns>The side's move.</returns>
internal delegate RoundAction Policy(string participantId, IReadOnlyList<Fighter> fighters);
