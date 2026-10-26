using System.Text.Json.Serialization;
using Battles.Grains.Abstractions;

namespace Battles.Features.ListBattles;

/// <summary>
/// One battle in an arena's list, as the HTTP API shows it.
/// </summary>
/// <param name="BattleId">The battle's id.</param>
/// <param name="Status">Waiting for players, or in progress.</param>
/// <param name="ParticipantIds">The sides that have joined so far.</param>
internal sealed record BattleSummaryResponse(
    Guid BattleId,
    [property: JsonConverter(typeof(JsonStringEnumConverter<BattleStatus>))] BattleStatus Status,
    IReadOnlyList<string> ParticipantIds);
