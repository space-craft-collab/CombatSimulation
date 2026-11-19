using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build the Catalog model without booting the
/// host. Adding a migration never connects; commands that do take
/// the real connection string through <c>--connection</c>.
/// </summary>
internal sealed class CatalogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string Placeholder = "Server=localhost,1433;Database=OrleansMonsterArena";

    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        options.UseModuleSqlServer(Placeholder, CatalogDbContext.SchemaName);

        return new CatalogDbContext(options.Options);
    }
}
