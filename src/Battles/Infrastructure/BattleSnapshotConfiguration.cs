using Battles.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Players.Contracts;
using Shared.Infrastructure.Persistence;

namespace Battles.Infrastructure;

/// <summary>
/// Maps <see cref="BattleSnapshot"/> to <c>battles.Battles</c> and
/// its combatants to <c>battles.BattleCombatants</c>, indexed by
/// participant for a player's battle history.
/// </summary>
internal sealed class BattleSnapshotConfiguration : IEntityTypeConfiguration<BattleSnapshot>
{
    // Catalog ids (arenas, species) are capped at 64 characters too.
    private const int CatalogIdMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BattleSnapshot> builder)
    {
        builder.ToTable("Battles");
        builder.HasKey(battle => battle.BattleId);

        // The key is the grain's id; a second write of the same battle
        // must fail rather than be generated anew (ADR-0003).
        builder.Property(battle => battle.BattleId).ValueGeneratedNever();
        builder.Property(battle => battle.ArenaId).HasMaxLength(CatalogIdMaxLength).UseCollation(IdCollation.Ordinal);
        builder.Property(battle => battle.WinnerId).HasMaxLength(PlayerIds.MaxLength).UseCollation(IdCollation.Ordinal);

        builder.OwnsMany(battle => battle.Combatants, combatant =>
        {
            combatant.ToTable("BattleCombatants");
            combatant.WithOwner().HasForeignKey("BattleId");
            combatant.HasKey("BattleId", nameof(SnapshotCombatant.Slot));
            combatant.Property(entry => entry.Slot).ValueGeneratedNever();
            combatant.Property(entry => entry.ParticipantId)
                .HasMaxLength(PlayerIds.MaxLength)
                .UseCollation(IdCollation.Ordinal);
            combatant.Property(entry => entry.SpeciesId)
                .HasMaxLength(CatalogIdMaxLength)
                .UseCollation(IdCollation.Ordinal);
            combatant.HasIndex(entry => entry.ParticipantId);
        });
    }
}
