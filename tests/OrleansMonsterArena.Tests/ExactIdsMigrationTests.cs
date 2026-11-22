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
/// date the way the host does it on startup. The way back is
/// covered too: <c>Down</c> keeps the rows, and where ids have
/// come to differ in case only it fails without changing anything.
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
        await InsertBattleWonByAliceAsync(modules, battleId);
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

        // Assert: up to date, the Catalog holds every seeded species.
        await using var context = await CreateContextAsync<CatalogDbContext>(modules);
        Assert.Equal(
            ["glacier", "moss-grove", "volcano"],
            await context.Arenas.Select(arena => arena.Id).OrderBy(id => id).ToListAsync(Cancellation));
        Assert.Equal(
            SpeciesSeed.All.Select(species => species.Id).Order(StringComparer.Ordinal),
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

    [Fact]
    public async Task MigrateDown_PlayersWithoutCaseTwins_KeepsStatsAndIgnoresCaseAgain()
    {
        // Arrange
        await using var modules = CreateModules("ExactIdsDownPlayers");
        await modules.MigrateModuleDatabasesAsync(Cancellation);
        await using var context = await CreateContextAsync<PlayersDbContext>(modules);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('Alice', 3, 1)", Cancellation);

        // Act
        await MigrateToAsync<PlayersDbContext>(modules, "InitialPlayers");

        // Assert
        Assert.Equal([new PlayerStats("Alice", 3, 1)], await context.Stats.AsNoTracking().ToListAsync(Cancellation));

        // Assert: the primary key is back and takes "alice" for Alice.
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('alice', 0, 0)", Cancellation));
    }

    [Fact]
    public async Task MigrateDown_PlayersWithCaseTwins_FailsAndChangesNothing()
    {
        // Arrange: two players the old collation cannot tell apart.
        await using var modules = CreateModules("ExactIdsDownPlayersTwins");
        await modules.MigrateModuleDatabasesAsync(Cancellation);
        await using var context = await CreateContextAsync<PlayersDbContext>(modules);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('Alice', 3, 1), ('alice', 1, 0)",
            Cancellation);

        // Act: the old primary key cannot be built over both rows.
        await Assert.ThrowsAsync<SqlException>(() => MigrateToAsync<PlayersDbContext>(modules, "InitialPlayers"));

        // Assert: the migration is still applied and both rows are there.
        Assert.EndsWith("_ExactIds", (await context.Database.GetAppliedMigrationsAsync(Cancellation)).Last());
        var stats = await context.Stats.AsNoTracking().ToListAsync(Cancellation);
        Assert.Equal(
            [new PlayerStats("Alice", 3, 1), new PlayerStats("alice", 1, 0)],
            stats.OrderBy(entry => entry.PlayerId, StringComparer.Ordinal));

        // Assert: the primary key is still there and still exact.
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('ALICE', 0, 0)", Cancellation);
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO players.PlayerStats (PlayerId, Wins, Losses) VALUES ('Alice', 0, 0)", Cancellation));
    }

    [Fact]
    public async Task MigrateDown_BattlesWithSnapshot_KeepsItAndListsItForOtherCaseAgain()
    {
        // Arrange
        await using var modules = CreateModules("ExactIdsDownBattles");
        await modules.MigrateModuleDatabasesAsync(Cancellation);
        var battleId = Guid.NewGuid();
        await InsertBattleWonByAliceAsync(modules, battleId);
        var snapshots = modules.GetRequiredService<BattleSnapshotReadRepository>();
        Assert.Empty(await snapshots.ListForParticipantAsync("alice", HistoryLimit, Cancellation));

        // Act
        await MigrateToAsync<BattlesDbContext>(modules, "InitialBattles");

        // Assert: no key hangs on these columns, so twins cannot fail here.
        var kept = Assert.Single(await snapshots.ListForParticipantAsync("alice", HistoryLimit, Cancellation));
        Assert.Equal(battleId, kept.BattleId);
        Assert.Equal("Alice", kept.WinnerId);
        Assert.Equal(["Alice", "bot"], kept.Combatants.OrderBy(entry => entry.Slot).Select(entry => entry.ParticipantId));
    }

    [Fact]
    public async Task MigrateDown_CatalogWithoutCaseTwins_KeepsRowsAndIgnoresCaseAgain()
    {
        // Arrange: the seeded species and arenas are the existing rows.
        await using var modules = CreateModules("ExactIdsDownCatalog");
        await modules.MigrateModuleDatabasesAsync(Cancellation);

        // Act
        await MigrateToAsync<CatalogDbContext>(modules, "AddArenas");

        // Assert
        await using var context = await CreateContextAsync<CatalogDbContext>(modules);
        Assert.Equal(
            ["glacier", "moss-grove", "volcano"],
            await context.Arenas.Select(arena => arena.Id).OrderBy(id => id).ToListAsync(Cancellation));
        Assert.Equal(
            ["ember-fox", "frost-owl", "lava-crab", "moss-golem"],
            await context.Species.Select(species => species.Id).OrderBy(id => id).ToListAsync(Cancellation));

        // Assert: both primary keys are back and take another case
        // for the same id.
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO catalog.Arenas (Id, Name) VALUES ('Volcano', 'Upper Volcano')", Cancellation));
        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO catalog.Species (Id, Name, MaxHitPoints, Attack, Defense, Speed)
            VALUES ('Ember-Fox', 'Elder Ember Fox', 50, 15, 5, 11)
            """,
            Cancellation));
    }

    [Fact]
    public async Task MigrateDown_CatalogWithCaseTwins_FailsAndChangesNothing()
    {
        // Arrange: an arena the old collation cannot tell from a seeded one.
        await using var modules = CreateModules("ExactIdsDownCatalogTwins");
        await modules.MigrateModuleDatabasesAsync(Cancellation);
        await using var context = await CreateContextAsync<CatalogDbContext>(modules);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO catalog.Arenas (Id, Name) VALUES ('Volcano', 'Upper Volcano')", Cancellation);

        // Act: the species columns are changed first, then the old
        // arena key cannot be built.
        await Assert.ThrowsAsync<SqlException>(() => MigrateToAsync<CatalogDbContext>(modules, "AddArenas"));

        // Assert: the migration is still applied and every row is there.
        Assert.EndsWith("_ExactIds", (await context.Database.GetAppliedMigrationsAsync(Cancellation)).Last());
        var arenas = await context.Arenas.Select(arena => arena.Id).ToListAsync(Cancellation);
        Assert.Equal(["Volcano", "glacier", "moss-grove", "volcano"], arenas.Order(StringComparer.Ordinal));
        Assert.Equal(4, await context.Species.CountAsync(Cancellation));

        // Assert: both primary keys are still there and still exact,
        // the species one included.
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO catalog.Arenas (Id, Name) VALUES ('VOLCANO', 'Loud Volcano')", Cancellation);
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

    // A completed battle Alice won against a bot.
    private static async Task InsertBattleWonByAliceAsync(IServiceProvider modules, Guid battleId)
    {
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
    }

    // Brings one module's schema to the named migration, up or
    // down, and no further; the database is created on the way.
    private static async Task MigrateToAsync<TContext>(IServiceProvider modules, string migration)
        where TContext : DbContext
    {
        await using var context = await CreateContextAsync<TContext>(modules);
        await context.GetService<IMigrator>().MigrateAsync(migration, Cancellation);
    }
}
