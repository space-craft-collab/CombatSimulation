using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

/// <summary>
/// Maps <see cref="Ability"/> to <c>catalog.Abilities</c> and seeds
/// it from <see cref="AbilitySeed"/>.
/// </summary>
internal sealed class AbilityConfiguration : IEntityTypeConfiguration<Ability>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ability> builder)
    {
        builder.ToTable("Abilities");
        builder.HasKey(ability => ability.Id);
        builder.Property(ability => ability.Id).HasMaxLength(64).UseCollation(IdCollation.Ordinal);
        builder.Property(ability => ability.Name).HasMaxLength(100);

        // Stored by name, so the table reads without the enums at hand.
        builder.Property(ability => ability.Element).HasConversion<string>().HasMaxLength(16);
        builder.Property(ability => ability.Effect).HasConversion<string>().HasMaxLength(16);
        builder.HasData(AbilitySeed.All);
    }
}
