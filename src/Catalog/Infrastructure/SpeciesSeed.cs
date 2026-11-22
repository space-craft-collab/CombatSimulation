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
        new("ember-fox", "Ember Fox", Element.Fire, 44, 21, 4, 14),
        new("lava-crab", "Lava Crab", Element.Fire, 56, 16, 7, 5),
        new("frost-owl", "Frost Owl", Element.Frost, 39, 21, 7, 14),
        new("moss-golem", "Moss Golem", Element.Nature, 68, 14, 8, 2),
        new("glacier-yak", "Glacier Yak", Element.Frost, 62, 13, 8, 4),
        new("thorn-lynx", "Thorn Lynx", Element.Nature, 43, 21, 3, 12),
    ];
}
