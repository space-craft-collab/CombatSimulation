using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// One booted <c>AppHost</c> shared by every test that needs it.
/// The co-hosted silo (ADR-0002) binds fixed localhost ports, so
/// only one host may be up at a time.
/// </summary>
public sealed class AppHostFixture : IAsyncDisposable
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
    /// The in-memory test host, with the round loop sped up.
    /// </summary>
    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseSetting("Battles:Timing:TurnDeadline", TurnDeadline.ToString())
            .UseSetting("Battles:Timing:JoinDeadline", TurnDeadline.ToString())
            .UseSetting("Battles:Timing:BotRoundInterval", "00:00:00.020"));

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
