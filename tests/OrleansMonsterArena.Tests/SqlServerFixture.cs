using Microsoft.Data.SqlClient;
using OrleansMonsterArena.Tests;
using Testcontainers.MsSql;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The one SQL Server container of a test run: the cold path is not
/// optional, so every booted host needs it. Hosts get a database of
/// their own on it, so their data never mixes.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Same image as compose.yaml, so tests and local runs agree.
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>
    /// The connection string of a database on the container. The
    /// database itself is created by the host's startup migration.
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <returns>The connection string.</returns>
    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }
            .ConnectionString;

    /// <summary>
    /// Starts the container.
    /// </summary>
    /// <returns>A task that completes once SQL Server accepts connections.</returns>
    public async ValueTask InitializeAsync() => await _container.StartAsync();

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    /// <returns>A task that completes once the container is gone.</returns>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
