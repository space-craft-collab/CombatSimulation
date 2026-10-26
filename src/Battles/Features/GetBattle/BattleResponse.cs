using System.Text.Json.Serialization;
using Battles.Grains.Abstractions;

namespace Battles.Features.GetBattle;

/// <summary>
/// A battle as the HTTP API shows it. Mapped from the grain's
/// <see cref="BattleState"/> so no Orleans type reaches the wire;
/// the start and submit slices answer with it too.
/// </summary>
/// <param name="BattleId">The battle's id.</param>
/// <param name="ArenaId">The arena the battle runs on.</param>
/// <param name="Status">The lifecycle state.</param>
/// <param name="Round">The current round.</param>
/// <param name="WinnerId">The winner once completed; <see langword="null"/> while running or when abandoned.</param>
/// <param name="TurnDeadline">When the round resolves without humans that have not acted.</param>
/// <param name="Combatants">Every monster slot in slot order.</param>
internal sealed record BattleResponse(
    Guid BattleId,
    string ArenaId,
    [property: JsonConverter(typeof(JsonStringEnumConverter<BattleStatus>))] BattleStatus Status,
    int Round,
    string? WinnerId,
    DateTimeOffset? TurnDeadline,
    IReadOnlyList<CombatantResponse> Combatants)
{
    /// <summary>
    /// Maps the grain's view of a battle.
    /// </summary>
    public static BattleResponse From(BattleState state) =>
        new(
            state.BattleId,
            state.ArenaId,
            state.Status,
            state.Round,
            state.WinnerId,
            state.TurnDeadline,
            [.. state.Combatants.Select(combatant => new CombatantResponse(
                combatant.Slot,
                combatant.ParticipantId,
                combatant.IsBot,
                combatant.SpeciesId,
                combatant.HitPoints,
                combatant.MaxHitPoints))]);
}
