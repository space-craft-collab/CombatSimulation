using Battles.Features.StartBattle;
using Catalog.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// One booted <c>AppHost</c> shared by every test that needs it.
/// The co-hosted silo (ADR-0002) binds fixed localhost ports, so
/// only one host may be up at a time.
/// </summary>
/// <param name="sql">The SQL Server the cold path runs on.</param>
public sealed class AppHostFixture(SqlServerFixture sql) : IAsyncDisposable
{
    /// <summary>
    /// The collection name referenced by <c>[Collection]</c>.
    /// </summary>
    public const string CollectionName = "AppHost";

    /// <summary>
    /// The human turn deadline, and the join deadline of open
    /// battles, the tests run with: short enough to wait for, long
    /// enough that no test races it by accident.
    /// </summary>
    public static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The one origin the test hosts admit as the frontend's.
    /// </summary>
    public const string FrontendOrigin = "http://frontend.test";

    /// <summary>
    /// The shared test host, with the round loop sped up: grain
    /// state and reminders in memory, the cold path on its own
    /// database. The Table Storage connection string is blanked so a
    /// developer's local <c>appsettings.Development.json</c> cannot
    /// switch these tests onto Azurite.
    /// </summary>
    public WebApplicationFactory<Program> Factory { get; } =
        CreateFactory(sql.ConnectionStringFor("AppHostTests"), string.Empty, TurnDeadline);

    /// <summary>
    /// Builds a test host with the round loop sped up and a bot that
    /// draws its team without chance (<see cref="FirstOfWhatIsLeft"/>).
    /// </summary>
    /// <param name="sql">The cold-path SQL Server connection string.</param>
    /// <param name="tableStorage">The Table Storage connection string;
    /// empty keeps grain storage and reminders in memory.</param>
    /// <param name="deadline">The turn and join deadline.</param>
    /// <returns>A host factory that boots on first use.</returns>
    public static WebApplicationFactory<Program> CreateFactory(string sql, string tableStorage, TimeSpan deadline) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseSetting($"ConnectionStrings:{ModuleDatabaseExtensions.ConnectionName}", sql)
                .UseSetting($"ConnectionStrings:{SiloStorageExtensions.TableStorageConnectionName}", tableStorage)
                .UseSetting("Cors:AllowedOrigins:0", FrontendOrigin)
                .UseSetting("Battles:Timing:TurnDeadline", deadline.ToString())
                .UseSetting("Battles:Timing:JoinDeadline", deadline.ToString())
                .UseSetting("Battles:Timing:BotRoundInterval", "00:00:00.020")
                .ConfigureTestServices(services => services.AddSingleton(provider => new BotOpponent(
                    provider.GetRequiredService<ICatalogQueryService>(), FirstOfWhatIsLeft))));

    /// <summary>
    /// The bot's draw in the test hosts: always the first species
    /// left, so against a visitor's team of <c>n</c> the bot fields
    /// the first <c>n</c> species by name and no test depends on
    /// chance.
    /// </summary>
    /// <param name="bound">The number of species left to draw from.</param>
    /// <returns>Always 0.</returns>
    public static int FirstOfWhatIsLeft(int bound) => 0;

    /// <summary>
    /// Shuts the host — and with it the silo — back down.
    /// </summary>
    /// <returns>A task that completes once the host has stopped.</returns>
    public async ValueTask DisposeAsync() => await Factory.DisposeAsync();
}

/// <summary>
/// Collection that serializes the host-booting tests around a
/// single <see cref="AppHostFixture"/>.
/// </summary>
[CollectionDefinition(AppHostFixture.CollectionName)]
public sealed class AppHostCollectionDefinition : ICollectionFixture<AppHostFixture>;
