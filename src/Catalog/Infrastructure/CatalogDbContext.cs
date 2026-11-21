using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

/// <summary>
/// The Catalog module's own database context, in the
/// <see cref="SchemaName"/> schema.
/// </summary>
/// <param name="options">The context options.</param>
internal sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options, SchemaName)
{
    /// <summary>
    /// The Catalog's SQL schema.
    /// </summary>
    public const string SchemaName = "catalog";

    /// <summary>
    /// The monster species.
    /// </summary>
    public DbSet<MonsterSpecies> Species => Set<MonsterSpecies>();

    /// <summary>
    /// The arenas battles run on.
    /// </summary>
    public DbSet<Arena> Arenas => Set<Arena>();
}
