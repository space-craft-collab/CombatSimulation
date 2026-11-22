using Battles.Grains.Abstractions;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Part of the shape a battle was stored in before difficulties
/// existed: a few of the members with the <c>[Id]</c> numbers 0 to 6,
/// and no 7. <see cref="GrainStateSerializationTests"/> writes it and
/// reads it back as today's <c>LiveBattleState</c>, the way a battle
/// stored then is read now.
/// </summary>
[GenerateSerializer]
internal sealed class LiveBattleStateBeforeDifficulty
{
    [Id(0)]
    public string ArenaId { get; init; } = "";

    [Id(1)]
    public BattleStatus Status { get; init; }

    [Id(2)]
    public int Round { get; init; }
}
