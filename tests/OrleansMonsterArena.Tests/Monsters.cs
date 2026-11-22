using Battles.Domain;
using Catalog.Contracts;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Builds fighters with made-up numbers for the unit tests of the
/// combat rules, so those tests do not move when the Catalog seed is
/// retuned.
/// </summary>
internal static class Monsters
{
    /// <summary>
    /// A fighter with round numbers: 40 hit points (also its full
    /// health unless given), attack 10,
    /// defense 0 (so a basic attack deals exactly the attack), speed
    /// 10, neutral, knowing only the basic attack.
    /// </summary>
    public static Fighter Create(
        int slot,
        string participantId,
        int hitPoints = 40,
        int attack = 10,
        int defense = 0,
        int speed = 10,
        Element element = Element.Neutral,
        int? maxHitPoints = null,
        params AbilitySpec[] abilities) =>
        new()
        {
            Slot = slot,
            ParticipantId = participantId,
            Element = element,
            Attack = attack,
            Defense = defense,
            Speed = speed,
            MaxHitPoints = maxHitPoints ?? hitPoints,
            HitPoints = hitPoints,
            Abilities = [new FighterAbility(AbilitySpec.Strike), .. abilities.Select(spec => new FighterAbility(spec))],
        };

    /// <summary>
    /// The basic attack of one monster on another.
    /// </summary>
    public static RoundAction Strike(string participantId, int actorSlot, int targetSlot) =>
        new(participantId, actorSlot, targetSlot, AbilitySpec.StrikeId);

    /// <summary>
    /// The fighter with one of its abilities on cooldown.
    /// </summary>
    public static Fighter WithCooldown(this Fighter fighter, string abilityId, int roundsLeft) =>
        fighter with
        {
            Abilities =
            [
                .. fighter.Abilities.Select(ability =>
                    ability.Spec.Id == abilityId ? ability with { CooldownLeft = roundsLeft } : ability),
            ],
        };
}
