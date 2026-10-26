using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Features.WatchBattle;
using Battles.Grains.Abstractions;
using Catalog.Contracts;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="ILiveBattleGrain"/>: runs the
/// turn-based round loop from ADR-0007. A battle either starts right
/// away or is opened by one human and waits for an opponent to join;
/// a reminder abandons it if nobody does. A round resolves once every
/// human side still standing has submitted one action; bots then
/// decide inline, so a human vs bot battle needs no timer. A reminder
/// enforces the human turn deadline; a grain timer plays the rounds
/// of a battle that has only bots left. Every resolved round leaves
/// as a <see cref="BattleEventDto"/> over SignalR once it is stored.
/// </summary>
/// <param name="state">The battle's persistent state.</param>
/// <param name="catalog">Source of the species base stats.</param>
/// <param name="timing">The round loop's pacing.</param>
/// <param name="clock">Source of the current time for the turn deadline.</param>
/// <param name="events">Sends the round deltas to the battle's watchers.</param>
internal sealed class LiveBattleGrain(
    [PersistentState("battle")] IPersistentState<LiveBattleState> state,
    ICatalogQueryService catalog,
    IOptions<BattleTimingOptions> timing,
    TimeProvider clock,
    BattleEventPublisher events)
    : Grain, ILiveBattleGrain, IRemindable
{
    // One reminder drives both deadlines: the join deadline while the
    // battle waits for players, the turn deadline once it runs.
    private const string DeadlineReminder = "deadline";

    // Orleans' minimum reminder period. The deadline itself is the
    // reminder's due time; the period only re-fires a tick that came
    // in early and was ignored.
    private static readonly TimeSpan DeadlineRetryPeriod = TimeSpan.FromMinutes(1);

    private IGrainTimer? _botLoop;

    private LiveBattleState Battle => state.State;

    /// <inheritdoc />
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Timers die with their activation, unlike reminders; re-arm a
        // bot-only battle that was still running.
        if (Battle.Status == BattleStatus.InProgress && HumanSidesStanding().Count == 0)
        {
            StartBotLoop();
        }

        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync(BattleSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        RequireStatus(BattleStatus.Created);
        ValidateSetup(setup);

        foreach (var participant in setup.Participants)
        {
            await AddParticipantAsync(participant);
        }

        Battle.ArenaId = setup.ArenaId;
        await BeginAsync();
    }

    /// <inheritdoc />
    public async Task OpenAsync(string arenaId, ParticipantSetup host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arenaId);
        ArgumentNullException.ThrowIfNull(host);
        RequireStatus(BattleStatus.Created);
        if (host.IsBot)
        {
            throw new ArgumentException("Only a human opens a battle and waits for an opponent.", nameof(host));
        }

        ValidateTeam(host, nameof(host));

        await AddParticipantAsync(host);
        Battle.ArenaId = arenaId;
        Battle.Status = BattleStatus.WaitingForPlayers;
        await ArmDeadlineAsync(timing.Value.JoinDeadline);
        await state.WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task JoinAsync(ParticipantSetup participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        RequireStatus(BattleStatus.WaitingForPlayers);
        ValidateTeam(participant, nameof(participant));
        if (Battle.Combatants.Any(combatant => combatant.ParticipantId == participant.ParticipantId))
        {
            throw new ArgumentException($"'{participant.ParticipantId}' already takes part.", nameof(participant));
        }

        await AddParticipantAsync(participant);
        await BeginAsync();
        await events.PublishStartedAsync(BattleResponse.From(Snapshot()));
    }

    /// <inheritdoc />
    public async Task SubmitActionAsync(BattleAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        RequireStatus(BattleStatus.InProgress);
        ValidateAction(action);

        Battle.PendingActions[action.ParticipantId] = action;
        if (Battle.PendingActions.Count != HumanSidesStanding().Count)
        {
            await state.WriteStateAsync();
            return;
        }

        AddBotActions();
        await PlayRoundAsync();
    }

    /// <inheritdoc />
    public Task<BattleState> GetStateAsync() => Task.FromResult(Snapshot());

    /// <summary>
    /// A deadline passed. While waiting for players, nobody joined and
    /// the open battle is abandoned. While running, the round resolves
    /// with the actions that arrived, and humans that missed it lose
    /// their move; if no human acted at all, the visitors walked away
    /// and the battle ends without a winner.
    /// </summary>
    /// <param name="reminderName">The reminder that fired.</param>
    /// <param name="status">Tick details from the runtime.</param>
    /// <returns>A task that completes once the tick is handled.</returns>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName != DeadlineReminder)
        {
            return;
        }

        if (Battle.Status is not (BattleStatus.WaitingForPlayers or BattleStatus.InProgress)
            || Battle.TurnDeadline is not { } deadline)
        {
            await UnregisterDeadlineAsync();
            return;
        }

        // A tick queued just before the round resolved, or one that
        // fired early; the re-armed reminder covers the new deadline.
        if (clock.GetUtcNow() < deadline)
        {
            return;
        }

        if (Battle.Status == BattleStatus.WaitingForPlayers || Battle.PendingActions.Count == 0)
        {
            Battle.Status = BattleStatus.Completed;
            await ScheduleNextRoundAsync();
            await state.WriteStateAsync();
            await PublishAsync(Battle.Round, []);
            return;
        }

        AddBotActions();
        await PlayRoundAsync();
    }

    private BattleState Snapshot() =>
        new(
            this.GetPrimaryKey(),
            Battle.ArenaId,
            Battle.Status,
            Battle.Round,
            Battle.WinnerId,
            Battle.TurnDeadline,
            [.. Battle.Combatants.Select(combatant => new CombatantSnapshot(
                combatant.Slot,
                combatant.ParticipantId,
                combatant.IsBot,
                combatant.SpeciesId,
                combatant.HitPoints,
                combatant.MaxHitPoints))]);

    private static void ValidateSetup(BattleSetup setup)
    {
        if (setup.Participants.Length < 2)
        {
            throw new ArgumentException("A battle needs at least two participants.", nameof(setup));
        }

        foreach (var participant in setup.Participants)
        {
            ValidateTeam(participant, nameof(setup));
        }

        if (setup.Participants.DistinctBy(participant => participant.ParticipantId).Count() != setup.Participants.Length)
        {
            throw new ArgumentException("Participant ids must be unique.", nameof(setup));
        }
    }

    private static void ValidateTeam(ParticipantSetup participant, string paramName)
    {
        if (participant.MonsterSpeciesIds.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Every participant needs at least one monster.", paramName);
        }
    }

    /// <summary>
    /// Gives the participant's monsters the next free slots. Every
    /// species is looked up first, so an unknown one leaves the battle
    /// untouched.
    /// </summary>
    private async Task AddParticipantAsync(ParticipantSetup participant)
    {
        var species = new List<MonsterSpeciesDto>();
        foreach (var speciesId in participant.MonsterSpeciesIds)
        {
            species.Add(await catalog.FindSpeciesAsync(speciesId)
                ?? throw new ArgumentException($"Unknown species '{speciesId}'.", nameof(participant)));
        }

        var initializations = new List<Task>();
        foreach (var monster in species)
        {
            var slot = Battle.Combatants.Count;
            Battle.Combatants.Add(new Combatant
            {
                Slot = slot,
                ParticipantId = participant.ParticipantId,
                Attack = monster.Attack,
                Defense = monster.Defense,
                Speed = monster.Speed,
                HitPoints = monster.MaxHitPoints,
                IsBot = participant.IsBot,
                SpeciesId = monster.Id,
                MaxHitPoints = monster.MaxHitPoints,
            });
            initializations.Add(Monster(slot).InitializeAsync(new MonsterStats(
                monster.Id, monster.MaxHitPoints, monster.Attack, monster.Defense, monster.Speed)));
        }

        await Task.WhenAll(initializations);
    }

    /// <summary>
    /// Starts round 1 once every side has joined.
    /// </summary>
    private async Task BeginAsync()
    {
        Battle.Status = BattleStatus.InProgress;
        Battle.Round = 1;
        await ScheduleNextRoundAsync();
        await state.WriteStateAsync();
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
            Battle.PendingActions[botId] = new BattleAction(botId, move.ActorSlot, move.TargetSlot, Ability.Strike);
        }
    }

    /// <summary>
    /// Resolves the pending actions, arms the next round, stores the
    /// result and only then tells the watchers, so no client sees a
    /// round that is not stored yet.
    /// </summary>
    private async Task PlayRoundAsync(CancellationToken cancellationToken = default)
    {
        var round = Battle.Round;
        var strikes = await ResolveRoundAsync();
        await ScheduleNextRoundAsync();
        await state.WriteStateAsync(cancellationToken);
        await PublishAsync(round, strikes);
    }

    private Task PublishAsync(int round, IReadOnlyList<StrikeDto> strikes) =>
        events.PublishAsync(new BattleEventDto(
            this.GetPrimaryKey(),
            round,
            Battle.Status,
            Battle.WinnerId,
            Battle.TurnDeadline,
            strikes));

    /// <summary>
    /// Plays the round's actions fastest monster first. An action whose
    /// actor or target fell earlier in the same round is dropped.
    /// </summary>
    /// <returns>The actions that landed, in resolution order.</returns>
    private async Task<List<StrikeDto>> ResolveRoundAsync()
    {
        var strikes = new List<StrikeDto>();
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
            strikes.Add(new StrikeDto(
                actor.Slot, target.Slot, action.AbilityId, target.HitPoints - snapshot.HitPoints, snapshot.HitPoints));
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

        return strikes;
    }

    /// <summary>
    /// Arms what drives the next round: a deadline reminder while a
    /// human side is standing, the bot loop once only bots are left,
    /// and nothing after the battle completed.
    /// </summary>
    private async Task ScheduleNextRoundAsync()
    {
        if (Battle.Status != BattleStatus.InProgress)
        {
            StopBotLoop();
            await CancelDeadlineAsync();
        }
        else if (HumanSidesStanding().Count == 0)
        {
            await CancelDeadlineAsync();
            StartBotLoop();
        }
        else
        {
            await ArmDeadlineAsync(timing.Value.TurnDeadline);
        }
    }

    private async Task ArmDeadlineAsync(TimeSpan dueIn)
    {
        Battle.TurnDeadline = clock.GetUtcNow() + dueIn;
        await this.RegisterOrUpdateReminder(DeadlineReminder, dueIn, DeadlineRetryPeriod);
    }

    private async Task CancelDeadlineAsync()
    {
        if (Battle.TurnDeadline is null)
        {
            return;
        }

        Battle.TurnDeadline = null;
        await UnregisterDeadlineAsync();
    }

    private async Task UnregisterDeadlineAsync()
    {
        if (await this.GetReminder(DeadlineReminder) is { } reminder)
        {
            await this.UnregisterReminder(reminder);
        }
    }

    private void StartBotLoop()
    {
        var interval = timing.Value.BotRoundInterval;
        _botLoop ??= this.RegisterGrainTimer(
            PlayBotRoundAsync,
            new GrainTimerCreationOptions(interval, interval) { KeepAlive = true });
    }

    private void StopBotLoop()
    {
        _botLoop?.Dispose();
        _botLoop = null;
    }

    private async Task PlayBotRoundAsync(CancellationToken cancellationToken)
    {
        if (Battle.Status != BattleStatus.InProgress)
        {
            StopBotLoop();
            return;
        }

        AddBotActions();
        await PlayRoundAsync(cancellationToken);
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
