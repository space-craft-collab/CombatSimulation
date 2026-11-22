namespace Battles.Domain;

/// <summary>
/// A bot's choice for one round: which own monster uses which
/// ability on which monster.
/// </summary>
internal sealed record BotMove(int ActorSlot, int TargetSlot, string AbilityId = AbilitySpec.StrikeId);
