namespace Battles.Features.StartBattle;

/// <summary>
/// Who a visitor wants to fight (ADR-0007 game modes).
/// </summary>
internal enum Opponent
{
    /// <summary>The rule-based bot; the battle starts at once.</summary>
    Bot,

    /// <summary>Another human; the battle waits until one joins.</summary>
    Human,
}
