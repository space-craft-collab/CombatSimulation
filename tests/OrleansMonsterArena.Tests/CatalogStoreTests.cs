using Catalog.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves the Catalog reads from SQL Server: the startup migration
/// created its schema and seeded the species.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class CatalogStoreTests(AppHostFixture host)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FindSpeciesAsync_SeededSpecies_ReturnsStatsFromSql()
    {
        // Arrange
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var species = await catalog.FindSpeciesAsync("lava-crab", Cancellation);

        // Assert
        Assert.Equal(new MonsterSpeciesDto("lava-crab", "Lava Crab", 60, 10, 8, 5), species);
    }

    [Fact]
    public async Task FindSpeciesAsync_UnknownSpecies_ReturnsNull()
    {
        // Arrange
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var species = await catalog.FindSpeciesAsync("no-such-species", Cancellation);

        // Assert
        Assert.Null(species);
    }

    [Fact]
    public async Task Migrate_CatalogModule_KeepsTablesAndHistoryInOwnSchema()
    {
        // Arrange
        var connectionString = host.Factory.Services
            .GetRequiredService<IConfiguration>()
            .GetConnectionString(ModuleDatabaseExtensions.ConnectionName);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(Cancellation);
        await using var command = new SqlCommand(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'catalog'",
            connection);

        // Act
        List<string> tables = [];
        await using var reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        Assert.Equal(["Species", "__EFMigrationsHistory"], tables.Order(StringComparer.Ordinal));
    }
}
