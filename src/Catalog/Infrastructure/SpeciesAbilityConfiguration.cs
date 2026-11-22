using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

/// <summary>
/// Maps <see cref="SpeciesAbility"/> to
/// <c>catalog.SpeciesAbilities</c> and seeds it from
/// <see cref="AbilitySeed"/>.
/// </summary>
internal sealed class SpeciesAbilityConfiguration : IEntityTypeConfiguration<SpeciesAbility>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SpeciesAbility> builder)
    {
        builder.ToTable("SpeciesAbilities");
        builder.HasKey(link => new { link.SpeciesId, link.AbilityId });
        builder.Property(link => link.SpeciesId).HasMaxLength(64).UseCollation(IdCollation.Ordinal);
        builder.Property(link => link.AbilityId).HasMaxLength(64).UseCollation(IdCollation.Ordinal);
        builder.HasOne<MonsterSpecies>().WithMany().HasForeignKey(link => link.SpeciesId);
        builder.HasOne<Ability>().WithMany().HasForeignKey(link => link.AbilityId);
        builder.HasData(AbilitySeed.BySpecies);
    }
}
