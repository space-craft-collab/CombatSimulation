using Battles.Domain;
using Battles.Grains.Abstractions;
using Catalog.Contracts;

namespace Battles.Features.StartBattle;

/// <summary>
/// The bot as a side of a battle: it fields a team of its own, as
/// large as its opponent's, from the species the Catalog knows.
/// </summary>
/// <param name="catalog">Knows the species there are.</param>
/// <param name="draw">Returns a random index below the bound it is given
/// (<see cref="Random.Next(int)"/> in the running host).</param>
internal sealed class BotOpponent(ICatalogQueryService catalog, Func<int, int> draw)
{
    /// <summary>
    /// The participant id of the bot opponent.
    /// </summary>
    public const string BotId = "bot";

    /// <summary>
    /// Puts the bot's team together.
    /// </summary>
    /// <param name="teamSize">The number of monsters the opponent fields.</param>
    /// <param name="cancellationToken">Cancels the Catalog lookup.</param>
    /// <returns>The bot as a participant.</returns>
    public async Task<ParticipantSetup> FieldAsync(int teamSize, CancellationToken cancellationToken = default)
    {
        var species = await catalog.ListSpeciesAsync(cancellationToken);

        return new ParticipantSetup(
            BotId,
            true,
            [.. BotTeam.Pick([.. species.Select(known => known.Id)], teamSize, draw)]);
    }
}
