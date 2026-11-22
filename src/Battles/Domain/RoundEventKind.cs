namespace Battles.Domain;

/// <summary>
/// What kind of thing happened in a round.
/// </summary>
internal enum RoundEventKind
{
    /// <summary>
    /// A monster used an ability.
    /// </summary>
    Ability,

    /// <summary>
    /// A burn took hit points at the end of the round.
    /// </summary>
    Burn,
}
