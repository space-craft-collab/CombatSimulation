namespace Battles.Grains;

/// <summary>
/// Persistent state of <see cref="ArenaGrain"/>.
/// </summary>
[GenerateSerializer]
internal sealed class ArenaState
{
    /// <summary>
    /// The battles not yet seen as completed.
    /// </summary>
    [Id(0)]
    public HashSet<Guid> Battles { get; set; } = [];
}
