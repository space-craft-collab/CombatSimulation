using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// One monster in a battle as the combat rules see it: owner, combat
/// stats, abilities, the effects on it and current hit points. Zero
/// hit points means defeated.
/// </summary>
internal sealed record Fighter
{
    private static readonly IReadOnlyList<FighterAbility> BasicAttackOnly = [new FighterAbility(AbilitySpec.Strike)];

    /// <summary>
    /// The slot, numbered across the whole battle.
    /// </summary>
    public required int Slot { get; init; }

    /// <summary>
    /// The side fielding the monster.
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The monster's element.
    /// </summary>
    public Element Element { get; init; }

    /// <summary>
    /// Offensive strength.
    /// </summary>
    public required int Attack { get; init; }

    /// <summary>
    /// Damage reduction.
    /// </summary>
    public required int Defense { get; init; }

    /// <summary>
    /// Decides turn order within a round.
    /// </summary>
    public required int Speed { get; init; }

    /// <summary>
    /// Hit points at full health.
    /// </summary>
    public required int MaxHitPoints { get; init; }

    /// <summary>
    /// Remaining hit points.
    /// </summary>
    public required int HitPoints { get; init; }

    /// <summary>
    /// The abilities the monster knows, with their cooldowns. A
    /// monster that names none knows the basic attack.
    /// </summary>
    public IReadOnlyList<FighterAbility> Abilities { get; init; } = BasicAttackOnly;

    /// <summary>
    /// The effects on the monster, at most one of a kind.
    /// </summary>
    public IReadOnlyList<ActiveEffect> Effects { get; init; } = [];

    /// <summary>
    /// Whether the monster is out of the fight.
    /// </summary>
    public bool IsDefeated => HitPoints <= 0;

    /// <summary>
    /// The speed the monster acts with: its speed, less a slow.
    /// </summary>
    public int EffectiveSpeed => Speed * (100 - ValueOf(AbilityEffect.Slow)) / 100;

    /// <summary>
    /// The share of a hit the monster's guard takes off, in percent;
    /// 0 without a guard.
    /// </summary>
    public int GuardPercent => ValueOf(AbilityEffect.Guard);

    /// <summary>
    /// Puts a fresh monster of a species into a slot: full hit
    /// points, every ability ready.
    /// </summary>
    /// <param name="slot">The slot it takes.</param>
    /// <param name="participantId">The side fielding it.</param>
    /// <param name="species">The Catalog species.</param>
    /// <returns>The monster as it enters the battle.</returns>
    public static Fighter Enter(int slot, string participantId, MonsterSpeciesDto species)
    {
        ArgumentNullException.ThrowIfNull(participantId);
        ArgumentNullException.ThrowIfNull(species);

        return new Fighter
        {
            Slot = slot,
            ParticipantId = participantId,
            Element = species.Element,
            Attack = species.Attack,
            Defense = species.Defense,
            Speed = species.Speed,
            MaxHitPoints = species.MaxHitPoints,
            HitPoints = species.MaxHitPoints,
            Abilities = species.Abilities.Count == 0
                ? BasicAttackOnly
                : [.. species.Abilities.Select(ability => new FighterAbility(AbilitySpec.From(ability)))],
        };
    }

    /// <summary>
    /// Looks up one of the monster's abilities.
    /// </summary>
    /// <param name="abilityId">The ability id.</param>
    /// <returns>The ability, or <see langword="null"/> if the monster does not know it.</returns>
    public FighterAbility? Find(string abilityId) =>
        Abilities.FirstOrDefault(ability => ability.Spec.Id == abilityId);

    /// <summary>
    /// Whether an effect of the kind lies on the monster.
    /// </summary>
    /// <param name="kind">The kind of effect.</param>
    /// <returns><see langword="true"/> if it does.</returns>
    public bool Has(AbilityEffect kind) => Effects.Any(effect => effect.Kind == kind);

    /// <summary>
    /// The monster with an effect put on it. An effect of the same
    /// kind is replaced, not stacked.
    /// </summary>
    /// <param name="effect">The new effect.</param>
    /// <returns>The monster under the effect.</returns>
    public Fighter With(ActiveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        return this with { Effects = [.. Effects.Where(other => other.Kind != effect.Kind), effect] };
    }

    private int ValueOf(AbilityEffect kind) =>
        Effects.FirstOrDefault(effect => effect.Kind == kind)?.Value ?? 0;
}
