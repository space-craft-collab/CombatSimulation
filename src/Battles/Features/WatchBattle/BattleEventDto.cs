using System.Text.Json.Serialization;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.WatchBattle;

/// <summary>
/// The round delta pushed to watching clients (ADR-0007): what
/// happened in the round that just resolved and where the battle
/// stands afterwards. A battle abandoned at the deadline sends one
/// last delta without events.
/// </summary>
/// <param name="BattleId">The battle's id.</param>
/// <param name="Round">The round that resolved.</param>
/// <param name="Status">The lifecycle state afterwards.</param>
/// <param name="WinnerId">The winner once completed; <see langword="null"/> while running or when abandoned.</param>
/// <param name="TurnDeadline">The deadline of the next round, while humans are still fighting.</param>
/// <param name="Events">What happened, in resolution order.</param>
/// <param name="Combatants">Every monster slot as it stands after the round: hit points, cooldowns and
/// effects. A client shows the events and takes its state from here.</param>
internal sealed record BattleEventDto(
    Guid BattleId,
    int Round,
    [property: JsonConverter(typeof(JsonStringEnumConverter<BattleStatus>))] BattleStatus Status,
    string? WinnerId,
    DateTimeOffset? TurnDeadline,
    IReadOnlyList<RoundEventDto> Events,
    IReadOnlyList<CombatantResponse> Combatants);
