namespace Battles.Domain;

/// <summary>
/// An ability of one monster in a battle, with its cooldown.
/// </summary>
/// <param name="Spec">The ability's numbers.</param>
/// <param name="CooldownLeft">The rounds until the monster can use it again; 0 when ready.</param>
internal sealed record FighterAbility(AbilitySpec Spec, int CooldownLeft = 0)
{
    /// <summary>
    /// Whether the monster can use the ability this round.
    /// </summary>
    public bool IsReady => CooldownLeft == 0;
}
