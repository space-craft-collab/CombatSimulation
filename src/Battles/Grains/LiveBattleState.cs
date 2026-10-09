using Battles.Grains.Abstractions;

namespace Battles.Grains;

/// <summary>
/// Persistent state of <see cref="LiveBattleGrain"/>.
/// </summary>
[GenerateSerializer]
internal sealed class LiveBattleState
{
    [Id(0)]
    public string ArenaId { get; set; } = "";

    [Id(1)]
    public BattleStatus Status { get; set; } = BattleStatus.Created;

    [Id(2)]
    public int Round { get; set; }

    [Id(3)]
    public List<Combatant> Combatants { get; set; } = [];

    /// <summary>
    /// The actions submitted for the current round, keyed by participant.
    /// </summary>
    [Id(4)]
    public Dictionary<string, BattleAction> PendingActions { get; set; } = [];

    [Id(5)]
    public string? WinnerId { get; set; }
}
