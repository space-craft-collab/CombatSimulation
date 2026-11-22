using Catalog.Contracts;
using Catalog.Domain;

namespace Catalog.Infrastructure;

/// <summary>
/// The abilities the Catalog ships with and which species knows
/// which. They reach SQL through the migrations (<c>HasData</c>), so
/// a change here needs a new migration to take effect, and a fresh
/// balance report (CONTRIBUTING.md).
/// </summary>
internal static class AbilitySeed
{
    /// <summary>
    /// The basic attack every species knows.
    /// </summary>
    public const string Strike = "strike";

    public static IReadOnlyList<Ability> All { get; } =
    [
        new(Strike, "Strike", Element.Neutral, 100, 0),
        new("flame-dash", "Flame Dash", Element.Fire, 160, 2),
        new("magma-claw", "Magma Claw", Element.Fire, 160, 2),
        new("ice-shard", "Ice Shard", Element.Frost, 160, 2),
        new("root-slam", "Root Slam", Element.Nature, 150, 2),
        new("ember-bite", "Ember Bite", Element.Fire, 80, 3, AbilityEffect.Burn, 5, 3),
        new("shell-up", "Shell Up", Element.Neutral, 0, 3, AbilityEffect.Guard, 60, 3),
        new("chill-wind", "Chill Wind", Element.Frost, 70, 3, AbilityEffect.Slow, 40, 2),
        new("regrow", "Regrow", Element.Nature, 0, 3, AbilityEffect.Heal, 20),
        new("avalanche", "Avalanche", Element.Frost, 180, 2),
        new("thick-hide", "Thick Hide", Element.Neutral, 0, 3, AbilityEffect.Guard, 30, 4),
        new("vine-whip", "Vine Whip", Element.Nature, 140, 1),
        new("pounce", "Pounce", Element.Nature, 220, 3),
    ];

    public static IReadOnlyList<SpeciesAbility> BySpecies { get; } =
    [
        .. Knows("ember-fox", Strike, "flame-dash", "ember-bite"),
        .. Knows("lava-crab", Strike, "magma-claw", "shell-up"),
        .. Knows("frost-owl", Strike, "ice-shard", "chill-wind"),
        .. Knows("moss-golem", Strike, "root-slam", "regrow"),
        .. Knows("glacier-yak", Strike, "avalanche", "thick-hide"),
        .. Knows("thorn-lynx", Strike, "vine-whip", "pounce"),
    ];

    private static IEnumerable<SpeciesAbility> Knows(string speciesId, params string[] abilityIds) =>
        abilityIds.Select((abilityId, position) => new SpeciesAbility(speciesId, abilityId, position));
}
