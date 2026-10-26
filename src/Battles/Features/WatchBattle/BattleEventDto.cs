using System.Text.Json.Serialization;
using Battles.Grains.Abstractions;

namespace Battles.Features.WatchBattle;

/// <summary>
/// The round delta pushed to watching clients (ADR-0007): what
/// happened in the round that just resolved and where the battle
/// stands afterwards. A battle abandoned at the deadline sends one
/// last delta without strikes.
/// </summary>
/// <param name="BattleId">The battle's id.</param>
/// <param name="Round">The round that resolved.</param>
/// <param name="Status">The lifecycle state afterwards.</param>
/// <param name="WinnerId">The winner once completed; <see langword="null"/> while running or when abandoned.</param>
/// <param name="TurnDeadline">The deadline of the next round, while humans are still fighting.</param>
/// <param name="Strikes">The actions that landed, in resolution order.</param>
internal sealed record BattleEventDto(
    Guid BattleId,
    int Round,
    [property: JsonConverter(typeof(JsonStringEnumConverter<BattleStatus>))] BattleStatus Status,
    string? WinnerId,
    DateTimeOffset? TurnDeadline,
    IReadOnlyList<StrikeDto> Strikes);
