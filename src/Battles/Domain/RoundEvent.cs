using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// One thing that happened while a round resolved: an ability that
/// was used and what it did, or a burn that took its toll.
/// </summary>
internal sealed record RoundEvent
{
    /// <summary>
    /// What kind of thing happened.
    /// </summary>
    public required RoundEventKind Kind { get; init; }

    /// <summary>
    /// The slot of the monster that acted; for a burn, the burning
    /// monster itself.
    /// </summary>
    public required int ActorSlot { get; init; }

    /// <summary>
    /// The slot of the monster it happened to; the actor's own slot
    /// for a guard, a heal and a burn.
    /// </summary>
    public required int TargetSlot { get; init; }

    /// <summary>
    /// The ability that was used; <see langword="null"/> for a burn.
    /// </summary>
    public string? AbilityId { get; init; }

    /// <summary>
    /// The hit points the target actually lost.
    /// </summary>
    public int Damage { get; init; }

    /// <summary>
    /// The hit points the target actually got back.
    /// </summary>
    public int Healing { get; init; }

    /// <summary>
    /// The target's hit points afterwards.
    /// </summary>
    public required int TargetHitPoints { get; init; }

    /// <summary>
    /// How the ability's element stood against the target's.
    /// </summary>
    public Effectiveness Effectiveness { get; init; } = Effectiveness.Normal;

    /// <summary>
    /// The effect that came into play: the one the ability put on
    /// its target, <see cref="AbilityEffect.Heal"/> for a heal,
    /// <see cref="AbilityEffect.Burn"/> for a burn's damage, and
    /// <see cref="AbilityEffect.None"/> for a plain hit or when the
    /// target fell before the effect could take hold.
    /// </summary>
    public AbilityEffect Effect { get; init; } = AbilityEffect.None;
}
