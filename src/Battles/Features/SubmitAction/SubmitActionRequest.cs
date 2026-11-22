using Battles.Domain;

namespace Battles.Features.SubmitAction;

/// <summary>
/// Body of <c>POST /battles/{battleId}/actions</c>: one participant's
/// move for the current round.
/// </summary>
internal sealed record SubmitActionRequest
{
    /// <summary>
    /// The participant acting.
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The slot of the participant's monster that acts.
    /// </summary>
    public required int ActorSlot { get; init; }

    /// <summary>
    /// The slot of the opponent's monster it targets; the acting
    /// monster's own slot for an ability it uses on itself.
    /// </summary>
    public required int TargetSlot { get; init; }

    /// <summary>
    /// The ability used, one the acting monster knows and has ready
    /// (<c>GET /species</c> lists them); the basic attack if left out.
    /// </summary>
    public string AbilityId { get; init; } = AbilitySpec.StrikeId;
}
