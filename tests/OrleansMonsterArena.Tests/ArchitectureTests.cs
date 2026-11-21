using Battles;
using Catalog;
using Catalog.Contracts;
using NetArchTest.Rules;
using Players;
using Players.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Enforces the intra-module layering convention from ADR-0009:
/// folders are not compiler-checked, so this test is the guard.
/// It also guards the module boundaries from ADR-0005: a module
/// reaches another one only through its <c>.Contracts</c> assembly,
/// and those stay free of framework dependencies.
/// </summary>
public sealed class ArchitectureTests
{
    // The module assemblies; a reference to any of them from another
    // module bypasses that module's contracts.
    private static readonly string[] ModuleAssemblies = ["Catalog", "Battles", "Players"];

    // Frameworks a contract assembly must not drag into its callers.
    private static readonly string[] FrameworkPrefixes =
    [
        "Orleans",
        "Microsoft.Orleans",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
    ];

    [Theory]
    [InlineData(typeof(CatalogModule))]
    [InlineData(typeof(BattlesModule))]
    [InlineData(typeof(PlayersModule))]
    public void Module_InAnyModule_ReferencesNoOtherModuleImplementation(Type moduleMarker)
    {
        // Arrange
        var module = moduleMarker.Assembly;

        // Act
        var referenced = module.GetReferencedAssemblies().Select(assembly => assembly.Name);

        // Assert
        Assert.Empty(referenced.Intersect(ModuleAssemblies.Except([module.GetName().Name])));
    }

    [Theory]
    [InlineData(typeof(ICatalogQueryService))]
    [InlineData(typeof(IPlayerStatsService))]
    public void Contracts_InAnyModule_ReferenceNoFramework(Type contractMarker)
    {
        // Act
        var referenced = contractMarker.Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name!);

        // Assert
        Assert.DoesNotContain(
            referenced,
            name => FrameworkPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(typeof(CatalogModule), "Catalog")]
    [InlineData(typeof(BattlesModule), "Battles")]
    [InlineData(typeof(PlayersModule), "Players")]
    public void Domain_InAnyModule_HasTypesToCheck(Type moduleMarker, string moduleName)
    {
        // Act: the layering rule below passes on an empty set, so a
        // renamed Domain namespace would switch it off unnoticed.
        var domainTypes = Types
            .InAssembly(moduleMarker.Assembly)
            .That()
            .ResideInNamespace($"{moduleName}.Domain")
            .GetTypes();

        // Assert
        Assert.NotEmpty(domainTypes);
    }

    [Theory]
    [InlineData(typeof(CatalogModule), "Catalog")]
    [InlineData(typeof(BattlesModule), "Battles")]
    [InlineData(typeof(PlayersModule), "Players")]
    public void Domain_InAnyModule_DoesNotDependOnFeaturesOrInfrastructure(
        Type moduleMarker,
        string moduleName)
    {
        // Arrange
        var domainTypes = Types
            .InAssembly(moduleMarker.Assembly)
            .That()
            .ResideInNamespace($"{moduleName}.Domain");

        // Act
        var result = domainTypes
            .ShouldNot()
            .HaveDependencyOnAny(
                $"{moduleName}.Features",
                $"{moduleName}.Infrastructure",
                $"{moduleName}.Grains")
            .GetResult();

        // Assert
        var failing = string.Join(", ", result.FailingTypeNames ?? []);
        Assert.True(
            result.IsSuccessful,
            $"Domain types depending on outer layers: {failing}");
    }
}
