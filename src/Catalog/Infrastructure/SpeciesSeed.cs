using Catalog.Contracts;
using Catalog.Domain;

namespace Catalog.Infrastructure;

/// <summary>
/// The species the Catalog ships with. They reach SQL through the
/// migrations (<c>HasData</c>), so a change here needs a new
/// migration to take effect.
/// </summary>
internal static class SpeciesSeed
{
    public static IReadOnlyList<MonsterSpecies> All { get; } =
    [
        new("ember-fox", "Ember Fox", Element.Fire, 40, 14, 4, 12),
        new("lava-crab", "Lava Crab", Element.Fire, 60, 10, 8, 5),
        new("frost-owl", "Frost Owl", Element.Frost, 35, 12, 5, 14),
        new("moss-golem", "Moss Golem", Element.Nature, 80, 8, 10, 3),
    ];
}
