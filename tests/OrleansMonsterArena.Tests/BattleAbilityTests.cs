using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Abilities and their cooldowns in a running battle (ADR-0012),
/// through the grains and over HTTP. Two humans fight, so the test
/// decides every move; the abilities are read from the Catalog, so
/// the tests hold when the seed is retuned.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleAbilityTests(AppHostFixture host)
{
    private const string Fox = "ember-fox";
    private const string Golem = "moss-golem";

    private const string Yak = "glacier-yak";

    // Two monsters a side, so nobody falls while a cooldown runs
    // down, whatever the numbers are: the fox attacks from slot 0,
    // its golem in slot 1 takes the opponent's strikes, and the
    // opponent's golem and yak in slots 2 and 3 share the fox's hits.
    private const int FoxSlot = 0;
    private const int DecoySlot = 1;
    private const int FirstTarget = 2;
    private const int SecondTarget = 3;

    private static readonly ImmutableArray<ParticipantSetup> FoxAndGolemVsGolemAndYak =
    [
        new("player-1", false, [Fox, Golem]),
        new("player-2", false, [Golem, Yak]),
    ];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    [Fact]
    public async Task CreateBattleAsync_AnyTeam_ListsEveryAbilityOfTheSpeciesAsReady()
    {
        // Arrange
        var fox = await SpeciesAsync(Fox);

        // Act
        var state = await (await StartAsync()).GetStateAsync();

        // Assert
        var abilities = state.Combatants[FoxSlot].Abilities;
        Assert.Equal(fox.Abilities.Select(ability => ability.Id), abilities.Select(ability => ability.AbilityId));
        Assert.All(abilities, ability => Assert.Equal(0, ability.RoundsLeft));
    }

    [Fact]
    public async Task SubmitActionAsync_AbilityWithCooldown_HitsHarderThanTheBasicAttackAndCoolsDown()
    {
        // Arrange: the same target takes a basic attack in one battle
        // and the special attack in another.
        var special = await SpecialAttackAsync(Fox);
        var struck = await StartAsync();
        var blasted = await StartAsync();

        // Act
        await PlayRoundAsync(struck, "strike", FirstTarget);
        await PlayRoundAsync(blasted, special.Id, FirstTarget);

        // Assert
        var afterStrike = await struck.GetStateAsync();
        var afterSpecial = await blasted.GetStateAsync();
        Assert.True(afterSpecial.Combatants[FirstTarget].HitPoints < afterStrike.Combatants[FirstTarget].HitPoints);
        Assert.Equal(special.Cooldown, CooldownOf(afterSpecial, special.Id));
        Assert.Equal(0, CooldownOf(afterSpecial, "strike"));
        Assert.Equal(0, CooldownOf(afterStrike, special.Id));
    }

    [Fact]
    public async Task SubmitActionAsync_AbilityOnCooldown_ThrowsArgumentExceptionUntilItIsReady()
    {
        // Arrange
        var special = await SpecialAttackAsync(Fox);
        var battle = await StartAsync();
        await PlayRoundAsync(battle, special.Id, FirstTarget);

        // Act + Assert: refused for as many rounds as the cooldown says...
        for (var round = 0; round < special.Cooldown; round++)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => battle.SubmitActionAsync(new BattleAction("player-1", FoxSlot, SecondTarget, special.Id)));
            await PlayRoundAsync(battle, "strike", SecondTarget);
        }

        // ...and accepted again right after.
        await PlayRoundAsync(battle, special.Id, SecondTarget);
        Assert.Equal(special.Cooldown, CooldownOf(await battle.GetStateAsync(), special.Id));
    }

    [Fact]
    public async Task SubmitActionAsync_AbilityOfAnotherSpecies_ThrowsArgumentException()
    {
        // Arrange
        var golemOnly = await SpecialAttackAsync(Golem);
        var battle = await StartAsync();

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", FoxSlot, FirstTarget, golemOnly.Id)));
        Assert.Equal(1, (await battle.GetStateAsync()).Round);
    }

    [Fact]
    public async Task PostAction_WithAbility_Returns200AndShowsTheCooldown()
    {
        // Arrange
        var special = await SpecialAttackAsync(Fox);
        var battleId = (await StartAsync()).GetPrimaryKey();
        using var client = host.Factory.CreateClient();
        await Grains.GetGrain<ILiveBattleGrain>(battleId)
            .SubmitActionAsync(new BattleAction("player-2", FirstTarget, DecoySlot, "strike"));

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = FoxSlot, targetSlot = FirstTarget, abilityId = special.Id },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var battle = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        Assert.Equal(2, battle.Round);
        var cooling = battle.Combatants[FoxSlot].Abilities.Single(ability => ability.AbilityId == special.Id);
        Assert.Equal(special.Cooldown, cooling.RoundsLeft);
    }

    [Fact]
    public async Task PostAction_AbilityOnCooldown_Returns400()
    {
        // Arrange
        var special = await SpecialAttackAsync(Fox);
        var battle = await StartAsync();
        await PlayRoundAsync(battle, special.Id, FirstTarget);
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battle.GetPrimaryKey()}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = FoxSlot, targetSlot = FirstTarget, abilityId = special.Id },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<ILiveBattleGrain> StartAsync()
    {
        var arena = Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}");

        return Grains.GetGrain<ILiveBattleGrain>(await arena.CreateBattleAsync(FoxAndGolemVsGolemAndYak));
    }

    // One round: the fox uses the ability on the given opponent, and
    // the opponent's golem strikes the fox's companion.
    private static async Task PlayRoundAsync(ILiveBattleGrain battle, string foxAbilityId, int targetSlot)
    {
        await battle.SubmitActionAsync(new BattleAction("player-1", FoxSlot, targetSlot, foxAbilityId));
        await battle.SubmitActionAsync(new BattleAction("player-2", FirstTarget, DecoySlot, "strike"));
    }

    private static int CooldownOf(BattleState state, string abilityId) =>
        state.Combatants[FoxSlot].Abilities.Single(ability => ability.AbilityId == abilityId).RoundsLeft;

    private async Task<MonsterSpeciesDto> SpeciesAsync(string speciesId) =>
        await host.Factory.Services.GetRequiredService<ICatalogQueryService>()
            .FindSpeciesAsync(speciesId, Cancellation)
        ?? throw new InvalidOperationException($"The Catalog does not seed '{speciesId}'.");

    // The species' first attack that has a cooldown.
    private async Task<AbilityDto> SpecialAttackAsync(string speciesId) =>
        (await SpeciesAsync(speciesId)).Abilities
            .First(ability => ability.Cooldown > 0 && !ability.TargetsSelf);
}
