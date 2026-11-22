using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// The element triangle: Fire beats Nature, Nature beats Frost,
/// Frost beats Fire. A neutral attack, a neutral target and two
/// equal elements are all <see cref="Effectiveness.Normal"/>.
/// </summary>
internal static class ElementChart
{
    /// <summary>
    /// How an attack of one element stands against a target of another.
    /// </summary>
    /// <param name="attack">The attack's element.</param>
    /// <param name="target">The target's element.</param>
    /// <returns>Whether the attack has the advantage, the target, or neither.</returns>
    public static Effectiveness Of(Element attack, Element target) =>
        (attack, target) switch
        {
            (Element.Fire, Element.Nature) or (Element.Nature, Element.Frost) or (Element.Frost, Element.Fire)
                => Effectiveness.Strong,
            (Element.Nature, Element.Fire) or (Element.Frost, Element.Nature) or (Element.Fire, Element.Frost)
                => Effectiveness.Weak,
            _ => Effectiveness.Normal,
        };
}
