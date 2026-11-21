using Battles.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace Battles.Infrastructure;

/// <summary>
/// The Battles module's own database context, in the
/// <see cref="SchemaName"/> schema: the cold store of completed
/// battles (ADR-0003).
/// </summary>
/// <param name="options">The context options.</param>
internal sealed class BattlesDbContext(DbContextOptions<BattlesDbContext> options)
    : ModuleDbContext(options, SchemaName)
{
    /// <summary>
    /// The Battles module's SQL schema.
    /// </summary>
    public const string SchemaName = "battles";

    /// <summary>
    /// The snapshots of completed battles.
    /// </summary>
    public DbSet<BattleSnapshot> Snapshots => Set<BattleSnapshot>();
}
