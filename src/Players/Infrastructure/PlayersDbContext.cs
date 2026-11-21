using Microsoft.EntityFrameworkCore;
using Players.Domain;
using Shared.Infrastructure.Persistence;

namespace Players.Infrastructure;

/// <summary>
/// The Players module's own database context, in the
/// <see cref="SchemaName"/> schema.
/// </summary>
/// <param name="options">The context options.</param>
internal sealed class PlayersDbContext(DbContextOptions<PlayersDbContext> options)
    : ModuleDbContext(options, SchemaName)
{
    /// <summary>
    /// The Players module's SQL schema.
    /// </summary>
    public const string SchemaName = "players";

    /// <summary>
    /// The per-player win/loss records.
    /// </summary>
    public DbSet<PlayerStats> Stats => Set<PlayerStats>();
}
