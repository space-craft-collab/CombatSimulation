using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Infrastructure.Persistence;

namespace Battles.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build the Battles model without booting the
/// host. Adding a migration never connects; commands that do take
/// the real connection string through <c>--connection</c>.
/// </summary>
internal sealed class BattlesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BattlesDbContext>
{
    private const string Placeholder = "Server=localhost,1433;Database=OrleansMonsterArena";

    /// <inheritdoc />
    public BattlesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BattlesDbContext>();
        options.UseModuleSqlServer(Placeholder, BattlesDbContext.SchemaName);

        return new BattlesDbContext(options.Options);
    }
}
