namespace Battles.Grains;

/// <summary>
/// Persistent state of <see cref="ArenaGrain"/>.
/// </summary>
[GenerateSerializer]
internal sealed class ArenaState
{
    /// <summary>
    /// The battles not yet seen as completed, with their participant counts.
    /// </summary>
    [Id(0)]
    public Dictionary<Guid, int> Battles { get; set; } = [];
}
