using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for how the bot draws its team.
/// </summary>
public sealed class BotTeamTests
{
    private static readonly string[] Species = ["a", "b", "c", "d"];

    [Fact]
    public void Pick_FewerThanThereAreSpecies_DrawsNoSpeciesTwice()
    {
        // Arrange: always the first of what is left.
        static int First(int bound) => 0;

        // Act
        var team = BotTeam.Pick(Species, 3, First);

        // Assert
        Assert.Equal(["a", "b", "c"], team);
    }

    [Fact]
    public void Pick_DrawFromTheEnd_TakesWhatTheDrawPointsAt()
    {
        // Arrange: always the last of what is left.
        static int Last(int bound) => bound - 1;

        // Act
        var team = BotTeam.Pick(Species, 2, Last);

        // Assert
        Assert.Equal(["d", "c"], team);
    }

    [Fact]
    public void Pick_MoreThanThereAreSpecies_StartsOverOnceAllAreTaken()
    {
        // Arrange
        static int First(int bound) => 0;

        // Act
        var team = BotTeam.Pick(["a", "b"], 3, First);

        // Assert
        Assert.Equal(["a", "b", "a"], team);
    }

    [Fact]
    public void Pick_AnyDraw_IsOnlyAskedForIndexesInRange()
    {
        // Arrange
        List<int> bounds = [];
        int Record(int bound)
        {
            bounds.Add(bound);

            return 0;
        }

        // Act
        BotTeam.Pick(Species, 4, Record);

        // Assert
        Assert.Equal([4, 3, 2, 1], bounds);
    }

    [Fact]
    public void Pick_NoSpecies_ThrowsArgumentException()
    {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => BotTeam.Pick([], 1, bound => 0));
    }

    [Fact]
    public void Pick_SizeZero_ThrowsArgumentOutOfRangeException()
    {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => BotTeam.Pick(Species, 0, bound => 0));
    }
}
