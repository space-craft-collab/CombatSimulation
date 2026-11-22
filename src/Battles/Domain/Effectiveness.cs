namespace Battles.Domain;

/// <summary>
/// How an attack's element stands against its target's.
/// </summary>
internal enum Effectiveness
{
    /// <summary>
    /// The target's element beats the attack's.
    /// </summary>
    Weak,

    /// <summary>
    /// Neither element has the advantage.
    /// </summary>
    Normal,

    /// <summary>
    /// The attack's element beats the target's.
    /// </summary>
    Strong,
}
