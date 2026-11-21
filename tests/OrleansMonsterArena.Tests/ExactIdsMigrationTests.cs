using Battles;
using Battles.Infrastructure;
using Catalog;
using Catalog.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Players;
using Players.Contracts;
using Players.Domain;
using Players.Infrastructure;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The <c>ExactIds</c> migrations on databases that already hold
/// rows. Every other test migrates an empty database in one go;
/// here each module stops one migration earlier, gets rows under
/// the old case-insensitive collation, and is then brought up to
/// date the way the host does it on startup.
/// </summary>
/// <param name="sql">The SQL Server the databases live on.</param>
public sealed class ExactIdsMigrationTests(SqlServerFixture sql)
{
    private const int HistoryLimit = 50;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrate_PlayersWithExistingStats_KeepsThemAndCountsOtherCaseSeparately()
    {
        // Arrange
        await using var modules = CreateModules("ExactIdsPlayers");
        await MigrateToAsync<PlayersDbContext>(modules, "InitialPlayers");
        await using var context = await CreateContextAsync<PlayersDbContext>(modules);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('Alice', 3, 1)", Cancellation);

        // Act
        await modules.MigrateModuleDatabasesAsync(Cancellation);

        // Assert: before, a win of "alice" was added to Alice's record.
        var stats = modules.GetRequiredService<PlayerStatsReadRepository>();
        await modules.GetRequiredService<IPlayerStatsService>()
            .RecordResultAsync(new BattleResultDto("alice", []), Cancellation);
        Assert.Equal(new PlayerStats("Alice", 3, 1), await stats.FindAsync("Alice", Cancellation));
        Assert.Equal(new PlayerStats("alice", 1, 0), await stats.FindAsync("alice", Cancellation));

        // Assert: the primary key is back.
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('Alice', 0, 0)", Cancellation));
    }

    [Fact]
    public async Task Migrate_BattlesWithExistingSnapshot_KeepsItAndListsItForExactIdOnly()
    {
        // Arrange: under the old collation "alice" finds Alice's battle.
        await using var modules = CreateModules("ExactIdsBattles");
        await MigrateToAsync<BattlesDbContext>(modules, "InitialBattles");
        var battleId = Guid.NewGuid();
        await using var context = await CreateContextAsync<BattlesDbContext>(modules);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO battles.Battles (BattleId, ArenaId, Round, WinnerId, CompletedAt)
            VALUES ({battleId}, 'volcano', 4, 'Alice', SYSDATETIMEOFFSET());
            INSERT INTO battles.BattleCombatants
                (BattleId, Slot, ParticipantId, IsBot, SpeciesId, HitPoints, MaxHitPoints)
            VALUES ({battleId}, 0, 'Alice', 0, 'ember-fox', 10, 40),
                   ({battleId}, 1, 'bot', 1, 'ember-fox', 0, 40);
            """,
            Cancellation);
        var snapshots = modules.GetRequiredService<BattleSnapshotReadRepository>();
        Assert.Single(await snapshots.ListForParticipantAsync("alice", HistoryLimit, Cancellation));

        // Act
        await modules.MigrateModuleDatabasesAsync(Cancellation);

        // Assert
        var kept = Assert.Single(await snapshots.ListForParticipantAsync("Alice", HistoryLimit, Cancellation));
        Assert.Equal(battleId, kept.BattleId);
        Assert.Equal("Alice", kept.WinnerId);
        Assert.Equal(["Alice", "bot"], kept.Combatants.OrderBy(entry => entry.Slot).Select(entry => entry.ParticipantId));
        Assert.Empty(await snapshots.ListForParticipantAsync("alice", HistoryLimit, Cancellation));
    }

    [Fact]
    public async Task Migrate_CatalogWithSeededRows_KeepsThemAndTakesOtherCaseAsNewId()
    {
        // Arrange: the seeded species and arenas are the existing rows.
        await using var modules = CreateModules("ExactIdsCatalog");
        await MigrateToAsync<CatalogDbContext>(modules, "AddArenas");

        // Act
        await modules.MigrateModuleDatabasesAsync(Cancellation);

        // Assert
        await using var context = await CreateContextAsync<CatalogDbContext>(modules);
        Assert.Equal(
            ["glacier", "moss-grove", "volcano"],
            await context.Arenas.Select(arena => arena.Id).OrderBy(id => id).ToListAsync(Cancellation));
        Assert.Equal(
            ["ember-fox", "frost-owl", "lava-crab", "moss-golem"],
            await context.Species.Select(species => species.Id).OrderBy(id => id).ToListAsync(Cancellation));

        // Assert: both primary keys are back and compare exactly —
        // another case is a new id, the same id a duplicate.
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO catalog.Arenas (Id, Name) VALUES ('Volcano', 'Upper Volcano')", Cancellation);
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO catalog.Species (Id, Name, MaxHitPoints, Attack, Defense, Speed)
            VALUES ('Ember-Fox', 'Elder Ember Fox', 50, 15, 5, 11)
            """,
            Cancellation);
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO catalog.Arenas (Id, Name) VALUES ('volcano', 'Twin Volcano')", Cancellation));
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO catalog.Species (Id, Name, MaxHitPoints, Attack, Defense, Speed)
            VALUES ('ember-fox', 'Twin Ember Fox', 40, 14, 4, 12)
            """,
            Cancellation));
    }

    // The three modules on a database of their own, without a host:
    // nothing here needs the silo or the web side.
    private ServiceProvider CreateModules(string database)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{ModuleDatabaseExtensions.ConnectionName}"] = sql.ConnectionStringFor(database),
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddCatalogModule()
            .AddBattlesModule()
            .AddPlayersModule()
            .BuildServiceProvider();
    }

    private static Task<TContext> CreateContextAsync<TContext>(IServiceProvider modules)
        where TContext : DbContext =>
        modules.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContextAsync(Cancellation);

    // Brings one module's schema up to the named migration and no
    // further; the database is created on the way.
    private static async Task MigrateToAsync<TContext>(IServiceProvider modules, string migration)
        where TContext : DbContext
    {
        await using var context = await CreateContextAsync<TContext>(modules);
        await context.GetService<IMigrator>().MigrateAsync(migration, Cancellation);
    }
}
