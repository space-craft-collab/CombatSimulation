using Battles.Domain;
using Catalog.Contracts;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// One species the simulator can field.
/// </summary>
/// <param name="Id">The species id.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The species' element.</param>
/// <param name="AbilityIds">The abilities the species knows.</param>
/// <param name="Enter">Puts a fresh monster of the species into a slot for a side.</param>
internal sealed record RosterEntry(
    string Id,
    string Name,
    Element Element,
    IReadOnlyList<string> AbilityIds,
    Func<int, string, Fighter> Enter);
