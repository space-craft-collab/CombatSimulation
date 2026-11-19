namespace Shared.Infrastructure.Persistence;

/// <summary>
/// One module database's pending migrations, as registered by
/// <see cref="ModuleDatabaseExtensions.AddModuleDatabase{TContext}"/>.
/// </summary>
/// <param name="Name">The context name, for logging.</param>
/// <param name="ApplyAsync">Applies the pending migrations.</param>
public sealed record ModuleMigration(string Name, Func<IServiceProvider, CancellationToken, Task> ApplyAsync);
