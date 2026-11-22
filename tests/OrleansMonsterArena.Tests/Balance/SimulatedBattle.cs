namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// How one simulated battle went.
/// </summary>
/// <param name="WinnerId">The side left standing; <see langword="null"/> if the
/// battle was still undecided at the round limit.</param>
/// <param name="Rounds">The rounds played.</param>
/// <param name="Uses">Every ability that landed.</param>
internal sealed record SimulatedBattle(string? WinnerId, int Rounds, IReadOnlyList<AbilityUse> Uses);
