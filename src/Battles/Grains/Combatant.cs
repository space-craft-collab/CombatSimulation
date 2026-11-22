using Battles.Domain;
using Catalog.Contracts;

namespace Battles.Grains;

/// <summary>
/// One monster slot as the battle stores it: owner, combat stats,
/// abilities and a mirror of its hit points. The monster grain stays
/// the authority on hit points; the battle copies them from each
/// snapshot so the rules can run without extra grain calls.
/// </summary>
[GenerateSerializer]
[Alias("battles.Combatant")]
internal sealed class Combatant
{
    [Id(0)]
    public required int Slot { get; init; }

    [Id(1)]
    public required string ParticipantId { get; init; }

    [Id(2)]
    public required int Attack { get; init; }

    [Id(3)]
    public required int Defense { get; init; }

    [Id(4)]
    public required int Speed { get; init; }

    [Id(5)]
    public required int HitPoints { get; set; }

    [Id(6)]
    public required bool IsBot { get; init; }

    [Id(7)]
    public required string SpeciesId { get; init; }

    [Id(8)]
    public required int MaxHitPoints { get; init; }

    // Not required: a battle stored before elements existed reads as
    // Neutral.
    [Id(9)]
    public Element Element { get; init; }

    // Empty in a battle stored before abilities existed; such a
    // monster knows the basic attack.
    [Id(10)]
    public List<CombatantAbility> Abilities { get; init; } = [];

    public bool IsDefeated => HitPoints <= 0;

    /// <summary>
    /// Stores a fighter that enters the battle.
    /// </summary>
    /// <param name="fighter">The monster as the combat rules see it.</param>
    /// <param name="isBot">Whether its side is a bot.</param>
    /// <param name="species">The Catalog species it was made from.</param>
    public static Combatant From(Fighter fighter, bool isBot, MonsterSpeciesDto species) =>
        new()
        {
            Slot = fighter.Slot,
            ParticipantId = fighter.ParticipantId,
            Attack = fighter.Attack,
            Defense = fighter.Defense,
            Speed = fighter.Speed,
            HitPoints = fighter.HitPoints,
            IsBot = isBot,
            SpeciesId = species.Id,
            MaxHitPoints = species.MaxHitPoints,
            Element = fighter.Element,
            Abilities = [.. fighter.Abilities.Select(CombatantAbility.From)],
        };

    /// <summary>
    /// The monster as the combat rules see it.
    /// </summary>
    public Fighter ToFighter()
    {
        var fighter = new Fighter
        {
            Slot = Slot,
            ParticipantId = ParticipantId,
            Element = Element,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            HitPoints = HitPoints,
        };

        return Abilities.Count == 0
            ? fighter
            : fighter with { Abilities = [.. Abilities.Select(ability => ability.ToFighterAbility())] };
    }

    /// <summary>
    /// Takes over what a round changed besides the hit points, which
    /// come from the monster grain.
    /// </summary>
    /// <param name="after">The monster after the round.</param>
    public void Take(Fighter after)
    {
        foreach (var ability in Abilities)
        {
            ability.CooldownLeft = after.Find(ability.AbilityId)?.CooldownLeft ?? 0;
        }
    }
}
