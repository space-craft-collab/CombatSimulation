using Battles.Grains.Abstractions;

namespace Battles.Grains;

/// <summary>
/// Persistent state of <see cref="MonsterInstanceGrain"/>.
/// </summary>
[GenerateSerializer]
internal sealed class MonsterInstanceState
{
    /// <summary>
    /// The entry stats; <see langword="null"/> until initialized.
    /// </summary>
    [Id(0)]
    public MonsterStats? Stats { get; set; }

    [Id(1)]
    public int HitPoints { get; set; }
}
