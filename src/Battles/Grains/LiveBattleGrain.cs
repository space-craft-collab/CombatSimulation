using Battles.Domain;
using Battles.Grains.Abstractions;
using Catalog.Contracts;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="ILiveBattleGrain"/>: runs the
/// turn-based round loop from ADR-0007. A round resolves once every
/// human side still standing has submitted one action; bots then
/// decide inline, so a human vs bot battle needs no timer.
/// </summary>
/// <param name="state">The battle's persistent state.</param>
/// <param name="catalog">Source of the species base stats.</param>
internal sealed class LiveBattleGrain(
    [PersistentState("battle")] IPersistentState<LiveBattleState> state,
    ICatalogQueryService catalog)
    : Grain, ILiveBattleGrain
{
    // Abilities are not modelled yet; the bot uses the basic attack.
    private const string BotAbilityId = "strike";

    private LiveBattleState Battle => state.State;

    /// <inheritdoc />
    public async Task StartAsync(BattleSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        RequireStatus(BattleStatus.Created);
        ValidateSetup(setup);

        var combatants = new List<Combatant>();
        var initializations = new List<Task>();
        foreach (var participant in setup.Participants)
        {
            foreach (var speciesId in participant.MonsterSpeciesIds)
            {
                var species = await catalog.FindSpeciesAsync(speciesId)
                    ?? throw new ArgumentException($"Unknown species '{speciesId}'.", nameof(setup));
                var slot = combatants.Count;

                combatants.Add(new Combatant
                {
                    Slot = slot,
                    ParticipantId = participant.ParticipantId,
                    Attack = species.Attack,
                    Defense = species.Defense,
                    Speed = species.Speed,
                    HitPoints = species.MaxHitPoints,
                    IsBot = participant.IsBot,
                });
                initializations.Add(Monster(slot).InitializeAsync(new MonsterStats(
                    species.Id, species.MaxHitPoints, species.Attack, species.Defense, species.Speed)));
            }
        }

        await Task.WhenAll(initializations);

        Battle.ArenaId = setup.ArenaId;
        Battle.Combatants = combatants;
        Battle.Status = BattleStatus.InProgress;
        Battle.Round = 1;
        await state.WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task SubmitActionAsync(BattleAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        RequireStatus(BattleStatus.InProgress);
        ValidateAction(action);

        Battle.PendingActions[action.ParticipantId] = action;
        if (Battle.PendingActions.Count == HumanSidesStanding().Count)
        {
            AddBotActions();
            await ResolveRoundAsync();
        }

        await state.WriteStateAsync();
    }

    /// <inheritdoc />
    public Task<BattleState> GetStateAsync() =>
        Task.FromResult(new BattleState(
            this.GetPrimaryKey(), Battle.ArenaId, Battle.Status, Battle.Round, Battle.WinnerId));

    private static void ValidateSetup(BattleSetup setup)
    {
        if (setup.Participants.Length < 2)
        {
            throw new ArgumentException("A battle needs at least two participants.", nameof(setup));
        }

        if (setup.Participants.Any(participant => participant.MonsterSpeciesIds.IsDefaultOrEmpty))
        {
            throw new ArgumentException("Every participant needs at least one monster.", nameof(setup));
        }

        if (setup.Participants.DistinctBy(participant => participant.ParticipantId).Count() != setup.Participants.Length)
        {
            throw new ArgumentException("Participant ids must be unique.", nameof(setup));
        }
    }

    private void ValidateAction(BattleAction action)
    {
        if (!SidesStanding().Contains(action.ParticipantId))
        {
            throw new ArgumentException($"'{action.ParticipantId}' is not fighting in this battle.", nameof(action));
        }

        if (!HumanSidesStanding().Contains(action.ParticipantId))
        {
            throw new ArgumentException($"'{action.ParticipantId}' is a bot and acts on its own.", nameof(action));
        }

        if (Battle.PendingActions.ContainsKey(action.ParticipantId))
        {
            throw new InvalidOperationException(
                $"'{action.ParticipantId}' already acted in round {Battle.Round}.");
        }

        var actor = FindStanding(action.ActorSlot);
        if (actor?.ParticipantId != action.ParticipantId)
        {
            throw new ArgumentException($"Slot {action.ActorSlot} is not a standing monster of '{action.ParticipantId}'.", nameof(action));
        }

        var target = FindStanding(action.TargetSlot);
        if (target is null || target.ParticipantId == action.ParticipantId)
        {
            throw new ArgumentException($"Slot {action.TargetSlot} is not a standing opponent.", nameof(action));
        }
    }

    private void AddBotActions()
    {
        var fighters = Battle.Combatants
            .Select(combatant => new Fighter(
                combatant.Slot, combatant.ParticipantId, combatant.Attack, combatant.HitPoints))
            .ToList();

        foreach (var botId in Standing().Where(combatant => combatant.IsBot).Select(combatant => combatant.ParticipantId).Distinct())
        {
            var move = BotStrategy.Decide(botId, fighters);
            Battle.PendingActions[botId] = new BattleAction(botId, move.ActorSlot, move.TargetSlot, BotAbilityId);
        }
    }

    /// <summary>
    /// Plays the round's actions fastest monster first. An action whose
    /// actor or target fell earlier in the same round is dropped.
    /// </summary>
    private async Task ResolveRoundAsync()
    {
        var ordered = Battle.PendingActions.Values
            .Select(action => (Action: action, Actor: Battle.Combatants[action.ActorSlot]))
            .OrderByDescending(entry => entry.Actor.Speed)
            .ThenBy(entry => entry.Actor.Slot);

        foreach (var (action, actor) in ordered)
        {
            var target = Battle.Combatants[action.TargetSlot];
            if (actor.IsDefeated || target.IsDefeated)
            {
                continue;
            }

            var snapshot = await Monster(target.Slot).ApplyDamageAsync(DamageRule.Calculate(actor.Attack, target.Defense));
            target.HitPoints = snapshot.HitPoints;
        }

        Battle.PendingActions.Clear();

        var standing = SidesStanding();
        if (standing.Count <= 1)
        {
            Battle.Status = BattleStatus.Completed;
            Battle.WinnerId = standing.SingleOrDefault();
        }
        else
        {
            Battle.Round++;
        }
    }

    private IEnumerable<Combatant> Standing() => Battle.Combatants.Where(combatant => !combatant.IsDefeated);

    private HashSet<string> SidesStanding() => [.. Standing().Select(combatant => combatant.ParticipantId)];

    private HashSet<string> HumanSidesStanding() =>
        [.. Standing().Where(combatant => !combatant.IsBot).Select(combatant => combatant.ParticipantId)];

    private Combatant? FindStanding(int slot) =>
        Battle.Combatants.ElementAtOrDefault(slot) is { IsDefeated: false } combatant ? combatant : null;

    private IMonsterInstanceGrain Monster(int slot) =>
        GrainFactory.GetGrain<IMonsterInstanceGrain>($"{this.GetPrimaryKey()}:{slot}");

    private void RequireStatus(BattleStatus expected)
    {
        if (Battle.Status != expected)
        {
            throw new InvalidOperationException(
                $"Battle {this.GetPrimaryKey()} is {Battle.Status}, expected {expected}.");
        }
    }
}
