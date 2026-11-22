using Catalog.Contracts;
using Catalog.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves the Catalog reads from SQL Server: the startup migrations
/// created its schema and seeded the species and arenas.
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

        // Assert: the stats the species was seeded with.
        var seeded = SpeciesSeed.All.Single(entry => entry.Id == "lava-crab");
        Assert.NotNull(species);
        Assert.Equal(seeded.Id, species.Id);
        Assert.Equal(seeded.Name, species.Name);
        Assert.Equal(seeded.MaxHitPoints, species.MaxHitPoints);
        Assert.Equal(seeded.Attack, species.Attack);
        Assert.Equal(seeded.Defense, species.Defense);
        Assert.Equal(seeded.Speed, species.Speed);
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
    public async Task FindArenaAsync_SeededArena_ReturnsItFromSql()
    {
        // Arrange
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var arena = await catalog.FindArenaAsync(TestArenas.Volcano, Cancellation);

        // Assert
        Assert.Equal(new ArenaDto("volcano", "Volcano"), arena);
    }

    [Fact]
    public async Task FindArenaAsync_UnknownArena_ReturnsNull()
    {
        // Arrange
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var arena = await catalog.FindArenaAsync("no-such-arena", Cancellation);

        // Assert
        Assert.Null(arena);
    }

    [Theory]
    [InlineData("Volcano")]
    [InlineData("VOLCANO")]
    [InlineData("volcano ")]
    public async Task FindArenaAsync_IdInOtherCaseOrPadded_ReturnsNull(string arenaId)
    {
        // Arrange: ids match exactly, as they do as grain keys.
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var arena = await catalog.FindArenaAsync(arenaId, Cancellation);

        // Assert
        Assert.Null(arena);
    }

    [Theory]
    [InlineData("Lava-Crab")]
    [InlineData("lava-crab ")]
    public async Task FindSpeciesAsync_IdInOtherCaseOrPadded_ReturnsNull(string speciesId)
    {
        // Arrange
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();

        // Act
        var species = await catalog.FindSpeciesAsync(speciesId, Cancellation);

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
        Assert.Equal(["Arenas", "Species", "__EFMigrationsHistory"], tables.Order(StringComparer.Ordinal));
    }
}
