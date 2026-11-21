using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Infrastructure.Persistence;

namespace Players.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build the Players model without booting the
/// host. Adding a migration never connects; commands that do take
/// the real connection string through <c>--connection</c>.
/// </summary>
internal sealed class PlayersDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlayersDbContext>
{
    private const string Placeholder = "Server=localhost,1433;Database=OrleansMonsterArena";

    /// <inheritdoc />
    public PlayersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlayersDbContext>();
        options.UseModuleSqlServer(Placeholder, PlayersDbContext.SchemaName);

        return new PlayersDbContext(options.Options);
    }
}
