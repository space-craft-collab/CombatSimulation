using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the element triangle.
/// </summary>
public sealed class ElementChartTests
{
    [Theory]
    [InlineData(Element.Fire, Element.Nature)]
    [InlineData(Element.Nature, Element.Frost)]
    [InlineData(Element.Frost, Element.Fire)]
    public void Of_AttackBeatsTarget_IsStrong(Element attack, Element target)
    {
        // Act
        var effectiveness = ElementChart.Of(attack, target);

        // Assert
        Assert.Equal(Effectiveness.Strong, effectiveness);
    }

    [Theory]
    [InlineData(Element.Nature, Element.Fire)]
    [InlineData(Element.Frost, Element.Nature)]
    [InlineData(Element.Fire, Element.Frost)]
    public void Of_TargetBeatsAttack_IsWeak(Element attack, Element target)
    {
        // Act
        var effectiveness = ElementChart.Of(attack, target);

        // Assert
        Assert.Equal(Effectiveness.Weak, effectiveness);
    }

    [Theory]
    [InlineData(Element.Fire, Element.Fire)]
    [InlineData(Element.Neutral, Element.Fire)]
    [InlineData(Element.Frost, Element.Neutral)]
    [InlineData(Element.Neutral, Element.Neutral)]
    public void Of_SameOrNeutralElement_IsNormal(Element attack, Element target)
    {
        // Act
        var effectiveness = ElementChart.Of(attack, target);

        // Assert
        Assert.Equal(Effectiveness.Normal, effectiveness);
    }

    [Fact]
    public void Of_EveryElement_BeatsExactlyOneAndLosesToExactlyOne()
    {
        // Arrange
        Element[] elements = [Element.Fire, Element.Frost, Element.Nature];

        foreach (var attack in elements)
        {
            // Act
            var against = elements.Select(target => ElementChart.Of(attack, target)).ToList();

            // Assert
            Assert.Single(against, Effectiveness.Strong);
            Assert.Single(against, Effectiveness.Weak);
        }
    }
}
