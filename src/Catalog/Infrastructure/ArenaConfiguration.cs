using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

/// <summary>
/// Maps <see cref="Arena"/> to <c>catalog.Arenas</c> and seeds it
/// from <see cref="ArenaSeed"/>.
/// </summary>
internal sealed class ArenaConfiguration : IEntityTypeConfiguration<Arena>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Arena> builder)
    {
        builder.ToTable("Arenas");
        builder.HasKey(arena => arena.Id);
        builder.Property(arena => arena.Id).HasMaxLength(64).UseCollation(IdCollation.Ordinal);
        builder.Property(arena => arena.Name).HasMaxLength(100);
        builder.HasData(ArenaSeed.All);
    }
}
