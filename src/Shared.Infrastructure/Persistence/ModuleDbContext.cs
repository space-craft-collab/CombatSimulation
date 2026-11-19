using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// Base for a module's own <see cref="DbContext"/>: every module owns
/// one SQL schema and maps only its own tables into it (ADR-0001).
/// </summary>
/// <param name="options">The options built by
/// <see cref="ModuleDatabaseExtensions.UseModuleSqlServer"/>.</param>
/// <param name="schema">The module's SQL schema, e.g. <c>"catalog"</c>.</param>
public abstract class ModuleDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    /// <summary>
    /// The module's SQL schema.
    /// </summary>
    public string Schema { get; } = schema;

    /// <summary>
    /// Puts every table into the module's schema and applies the
    /// entity configurations of the module's assembly.
    /// </summary>
    /// <param name="modelBuilder">The model being built.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
