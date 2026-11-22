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
        new("flame-dash", "Flame Dash", Element.Fire, 150, 2),
        new("magma-claw", "Magma Claw", Element.Fire, 140, 2),
        new("ice-shard", "Ice Shard", Element.Frost, 150, 2),
        new("root-slam", "Root Slam", Element.Nature, 150, 3),
        new("ember-bite", "Ember Bite", Element.Fire, 80, 3, AbilityEffect.Burn, 4, 3),
        new("shell-up", "Shell Up", Element.Neutral, 0, 4, AbilityEffect.Guard, 50, 3),
        new("chill-wind", "Chill Wind", Element.Frost, 80, 3, AbilityEffect.Slow, 50, 3),
        new("regrow", "Regrow", Element.Nature, 0, 4, AbilityEffect.Heal, 30),
    ];

    public static IReadOnlyList<SpeciesAbility> BySpecies { get; } =
    [
        .. Knows("ember-fox", Strike, "flame-dash", "ember-bite"),
        .. Knows("lava-crab", Strike, "magma-claw", "shell-up"),
        .. Knows("frost-owl", Strike, "ice-shard", "chill-wind"),
        .. Knows("moss-golem", Strike, "root-slam", "regrow"),
    ];

    private static IEnumerable<SpeciesAbility> Knows(string speciesId, params string[] abilityIds) =>
        abilityIds.Select((abilityId, position) => new SpeciesAbility(speciesId, abilityId, position));
}
