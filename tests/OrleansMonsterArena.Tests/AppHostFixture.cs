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
    /// The in-memory test host.
    /// </summary>
    public WebApplicationFactory<Program> Factory { get; } = new();

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
