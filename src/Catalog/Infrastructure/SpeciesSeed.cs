using System.Collections.Frozen;
using Catalog.Contracts;

namespace Catalog.Infrastructure;

/// <summary>
/// Hard-coded species for Phase 2. Phase 3 replaces this with an
/// EF Core read repository over Azure SQL.
/// </summary>
internal static class SpeciesSeed
{
    public static FrozenDictionary<string, MonsterSpeciesDto> All { get; } = new MonsterSpeciesDto[]
    {
        new("ember-fox", "Ember Fox", 40, 14, 4, 12),
        new("lava-crab", "Lava Crab", 60, 10, 8, 5),
        new("frost-owl", "Frost Owl", 35, 12, 5, 14),
        new("moss-golem", "Moss Golem", 80, 8, 10, 3),
    }.ToFrozenDictionary(species => species.Id);
}
