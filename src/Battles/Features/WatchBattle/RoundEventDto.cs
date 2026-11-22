using System.Text.Json.Serialization;
using Battles.Domain;
using Catalog.Contracts;

namespace Battles.Features.WatchBattle;

/// <summary>
/// One thing that happened during a round, in the order it happened:
/// an ability that was used and what it did, or a burn that took its
/// toll.
/// </summary>
/// <param name="Kind">What kind of thing happened.</param>
/// <param name="ActorSlot">The slot of the monster that acted; for a burn, the burning monster itself.</param>
/// <param name="TargetSlot">The slot of the monster it happened to; the actor's own slot for a guard,
/// a heal and a burn.</param>
/// <param name="AbilityId">The ability that was used; <see langword="null"/> for a burn.</param>
/// <param name="Damage">The hit points the target actually lost.</param>
/// <param name="Healing">The hit points the target actually got back.</param>
/// <param name="TargetHitPoints">The target's hit points afterwards.</param>
/// <param name="Effectiveness">How the ability's element stood against the target's.</param>
/// <param name="Effect">The effect that came into play: the one the ability put on its target, <c>Heal</c>
/// for a heal, <c>Burn</c> for a burn's damage, <c>None</c> for a plain hit.</param>
internal sealed record RoundEventDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter<RoundEventKind>))] RoundEventKind Kind,
    int ActorSlot,
    int TargetSlot,
    string? AbilityId,
    int Damage,
    int Healing,
    int TargetHitPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter<Effectiveness>))] Effectiveness Effectiveness,
    [property: JsonConverter(typeof(JsonStringEnumConverter<AbilityEffect>))] AbilityEffect Effect);
