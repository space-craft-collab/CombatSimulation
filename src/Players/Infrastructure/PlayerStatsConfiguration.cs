using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Players.Contracts;
using Players.Domain;
using Shared.Infrastructure.Persistence;

namespace Players.Infrastructure;

/// <summary>
/// Maps <see cref="PlayerStats"/> to <c>players.PlayerStats</c>.
/// </summary>
internal sealed class PlayerStatsConfiguration : IEntityTypeConfiguration<PlayerStats>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlayerStats> builder)
    {
        builder.ToTable("PlayerStats");
        builder.HasKey(stats => stats.PlayerId);
        builder.Property(stats => stats.PlayerId).HasMaxLength(PlayerIds.MaxLength).UseCollation(IdCollation.Ordinal);
    }
}
