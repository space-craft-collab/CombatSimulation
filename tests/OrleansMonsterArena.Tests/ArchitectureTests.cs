using Battles;
using Catalog;
using NetArchTest.Rules;
using Players;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Enforces the intra-module layering convention from ADR-0009:
/// folders are not compiler-checked, so this test is the guard.
/// </summary>
public sealed class ArchitectureTests
{
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
                $"{moduleName}.Infrastructure")
            .GetResult();

        // Assert
        var failing = string.Join(", ", result.FailingTypeNames ?? []);
        Assert.True(
            result.IsSuccessful,
            $"Domain types depending on outer layers: {failing}");
    }
}
