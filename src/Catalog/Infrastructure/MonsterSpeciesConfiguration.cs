using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure;

/// <summary>
/// Maps <see cref="MonsterSpecies"/> to <c>catalog.Species</c> and
/// seeds it from <see cref="SpeciesSeed"/>.
/// </summary>
internal sealed class MonsterSpeciesConfiguration : IEntityTypeConfiguration<MonsterSpecies>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MonsterSpecies> builder)
    {
        builder.ToTable("Species");
        builder.HasKey(species => species.Id);
        builder.Property(species => species.Id).HasMaxLength(64);
        builder.Property(species => species.Name).HasMaxLength(100);
        builder.HasData(SpeciesSeed.All);
    }
}
