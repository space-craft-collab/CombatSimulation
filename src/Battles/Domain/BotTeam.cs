namespace Battles.Domain;

/// <summary>
/// How the bot puts its team together: as many monsters as its
/// opponent fields, drawn at random from the species there are.
/// </summary>
internal static class BotTeam
{
    /// <summary>
    /// Draws a team. No species is drawn twice as long as there are
    /// enough of them; a team larger than the choice starts over with
    /// all species once each one is taken.
    /// </summary>
    /// <param name="speciesIds">The species to choose from.</param>
    /// <param name="size">The number of monsters to field.</param>
    /// <param name="draw">Returns a random index below the bound it is given.</param>
    /// <returns>The species of the team, in slot order.</returns>
    /// <exception cref="ArgumentException">There is no species to choose from.</exception>
    public static IReadOnlyList<string> Pick(IReadOnlyCollection<string> speciesIds, int size, Func<int, int> draw)
    {
        ArgumentNullException.ThrowIfNull(speciesIds);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        if (speciesIds.Count == 0)
        {
            throw new ArgumentException("The bot needs at least one species to choose from.", nameof(speciesIds));
        }

        var team = new List<string>(size);
        var left = new List<string>();
        while (team.Count < size)
        {
            if (left.Count == 0)
            {
                left.AddRange(speciesIds);
            }

            var index = draw(left.Count);
            team.Add(left[index]);
            left.RemoveAt(index);
        }

        return team;
    }
}
