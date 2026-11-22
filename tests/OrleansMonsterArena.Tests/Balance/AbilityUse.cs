namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// One ability that landed in a simulated battle.
/// </summary>
/// <param name="Slot">The slot of the monster that used it.</param>
/// <param name="AbilityId">The ability.</param>
internal sealed record AbilityUse(int Slot, string AbilityId);
