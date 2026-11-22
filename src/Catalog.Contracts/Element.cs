namespace Catalog.Contracts;

/// <summary>
/// The element of a species or of an ability. Fire beats Nature,
/// Nature beats Frost, Frost beats Fire; <see cref="Neutral"/> has
/// neither an advantage nor a weakness. The numbers are stored in
/// grain state and must not change.
/// </summary>
public enum Element
{
    /// <summary>
    /// No element.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// Strong against Nature, weak against Frost.
    /// </summary>
    Fire = 1,

    /// <summary>
    /// Strong against Fire, weak against Nature.
    /// </summary>
    Frost = 2,

    /// <summary>
    /// Strong against Frost, weak against Fire.
    /// </summary>
    Nature = 3,
}
