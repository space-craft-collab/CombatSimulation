using Testcontainers.Azurite;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// One Azurite container per test class: the Table Storage that
/// grain state and reminders persist to.
/// </summary>
public sealed class AzuriteFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ComposeImages.Azurite).Build();

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
    /// Stops the container before the test ends, as an outage of
    /// Table Storage.
    /// </summary>
    /// <returns>A task that completes once Azurite is down.</returns>
    public Task StopAsync() => _container.StopAsync(TestContext.Current.CancellationToken);

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    /// <returns>A task that completes once the container is gone.</returns>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
