namespace Battles.Domain;

/// <summary>
/// What a round did: the events in the order they happened and every
/// fighter as it stands afterwards.
/// </summary>
/// <param name="Fighters">Every fighter after the round, in slot order.</param>
/// <param name="Events">What happened, in resolution order.</param>
internal sealed record RoundResult(IReadOnlyList<Fighter> Fighters, IReadOnlyList<RoundEvent> Events);
