using Testcontainers.Azurite;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// One Azurite container per test class: the Table Storage that
/// grain state and reminders persist to.
/// </summary>
public sealed class AzuriteFixture : IAsyncLifetime
{
    // Same image as compose.yaml, so tests and local runs agree.
    private readonly AzuriteContainer _container =
        new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest").Build();

    /// <summary>
    /// The connection string of the running container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the container.
    /// </summary>
    /// <returns>A task that completes once Azurite accepts requests.</returns>
    public async ValueTask InitializeAsync() => await _container.StartAsync();

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    /// <returns>A task that completes once the container is gone.</returns>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
