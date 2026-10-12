namespace Battles.Domain;

/// <summary>
/// A bot's choice for one round: which own monster attacks which
/// opponent.
/// </summary>
internal sealed record BotMove(int ActorSlot, int TargetSlot);
