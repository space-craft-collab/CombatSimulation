using Battles.Features.StartBattle;

namespace Battles.Features.JoinBattle;

/// <summary>
/// Body of <c>POST /battles/{battleId}/participants</c>: a second human
/// joins an open battle.
/// </summary>
internal sealed record JoinBattleRequest
{
    /// <summary>
    /// The joining player's id; it must differ from the host's.
    /// </summary>
    public required string PlayerId { get; init; }

    /// <summary>
    /// The Catalog species the player fields, in slot order.
    /// </summary>
    public required IReadOnlyList<string> MonsterSpeciesIds { get; init; }

    /// <summary>
    /// Checks the request's shape, by the same rules as starting a battle.
    /// </summary>
    /// <returns>Errors per field; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() =>
        StartBattleRequest.ValidateTeam(PlayerId, MonsterSpeciesIds);
}
