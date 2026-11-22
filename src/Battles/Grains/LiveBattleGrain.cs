using System.Diagnostics.CodeAnalysis;
using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Features.WatchBattle;
using Battles.Grains.Abstractions;
using Battles.Infrastructure;
using Catalog.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Players.Contracts;

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
/// A completed battle crosses to the cold store (ADR-0003): its
/// snapshot goes to SQL, a decided result to the player stats, and
/// the hot state is cleared before the grain deactivates.
/// </summary>
/// <param name="state">The battle's persistent state.</param>
/// <param name="catalog">Source of the species base stats.</param>
/// <param name="timing">The round loop's pacing.</param>
/// <param name="clock">Source of the current time for the turn deadline.</param>
/// <param name="events">Sends the round deltas to the battle's watchers.</param>
/// <param name="snapshots">Stores the completed battle in the cold store.</param>
/// <param name="playerStats">Counts a decided battle for its players.</param>
/// <param name="logger">Logs a failed hand-over to the cold store.</param>
// Pinned to the name Orleans would derive anyway: the grain type is part
// of every stored state and reminder key, so a class rename must not
// change it.
[GrainType("livebattle")]
internal sealed class LiveBattleGrain(
    [PersistentState("battle")] IPersistentState<LiveBattleState> state,
    ICatalogQueryService catalog,
    IOptions<BattleTimingOptions> timing,
    TimeProvider clock,
    BattleEventPublisher events,
    BattleSnapshotWriteRepository snapshots,
    IPlayerStatsService playerStats,
    ILogger<LiveBattleGrain> logger)
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

    // Set when a round failed after some monster grains were already
    // told their new hit points; the next round tells all of them.
    private bool _monstersInDoubt;

    private LiveBattleState Battle => state.State;

    /// <inheritdoc />
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Timers die with their activation, unlike reminders.
        ResumeBotLoop();

        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync(BattleSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        RequireStatus(BattleStatus.Created);
        ValidateSetup(setup);

        // Every side's species are looked up before the first monster
        // is added, so an unknown one leaves the battle untouched.
        var teams = new List<(ParticipantSetup Participant, List<MonsterSpeciesDto> Species)>();
        foreach (var participant in setup.Participants)
        {
            teams.Add((participant, await ResolveTeamAsync(participant)));
        }

        foreach (var (participant, species) in teams)
        {
            await AddParticipantAsync(participant, species);
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

        await AddParticipantAsync(host, await ResolveTeamAsync(host));
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

        await AddParticipantAsync(participant, await ResolveTeamAsync(participant));
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
            await HandOverAsync();
            return;
        }

        AddBotActions();
        await PlayRoundAsync();
    }

    // The abilities are taken as the combat rules see them, so a
    // battle stored before abilities existed shows the basic attack
    // its monsters fight with.
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
                combatant.MaxHitPoints,
                [.. combatant.ToFighter().Abilities.Select(ability =>
                    new AbilityCooldown(ability.Spec.Id, ability.CooldownLeft))],
                [.. combatant.Effects.Select(ToSnapshot)]))],
            Battle.Difficulty);

    // Only effects that last are stored on a combatant; a heal is
    // over the moment it is used.
    private static ActiveEffectSnapshot ToSnapshot(CombatantEffect effect) =>
        new(
            effect.Kind switch
            {
                AbilityEffect.Burn => StatusEffect.Burn,
                AbilityEffect.Guard => StatusEffect.Guard,
                AbilityEffect.Slow => StatusEffect.Slow,
                _ => throw new InvalidOperationException($"'{effect.Kind}' is not an effect that lasts."),
            },
            effect.Value,
            effect.RoundsLeft);

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
    /// Looks up the species a participant fields, in slot order.
    /// It changes nothing, so a caller resolves every team before it
    /// adds the first monster.
    /// </summary>
    /// <exception cref="ArgumentException">A species does not exist.</exception>
    private async Task<List<MonsterSpeciesDto>> ResolveTeamAsync(ParticipantSetup participant)
    {
        var species = new List<MonsterSpeciesDto>();
        foreach (var speciesId in participant.MonsterSpeciesIds)
        {
            species.Add(await catalog.FindSpeciesAsync(speciesId)
                ?? throw new ArgumentException($"Unknown species '{speciesId}'.", nameof(participant)));
        }

        return species;
    }

    /// <summary>
    /// Gives the participant's monsters the next free slots. A bot on
    /// Easy fields them weakened (ADR-0013), which makes the battle an
    /// Easy one; a human side is never weakened. The monster grains
    /// get the stats the battle plays with.
    /// </summary>
    private async Task AddParticipantAsync(ParticipantSetup participant, List<MonsterSpeciesDto> species)
    {
        var weakened = participant is { IsBot: true, Difficulty: Difficulty.Easy };
        var initializations = new List<Task>();
        foreach (var monster in species)
        {
            var slot = Battle.Combatants.Count;
            var fighter = Fighter.Enter(slot, participant.ParticipantId, monster);
            if (weakened)
            {
                fighter = EasyHandicap.Weaken(fighter);
            }

            Battle.Combatants.Add(Combatant.From(fighter, participant.IsBot, monster));
            initializations.Add(Monster(slot).InitializeAsync(new MonsterStats(
                monster.Id, fighter.MaxHitPoints, fighter.Attack, fighter.Defense, fighter.Speed)));
        }

        if (weakened)
        {
            Battle.Difficulty = Difficulty.Easy;
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

        if (MoveRule.Reject(Fighters(), ToRoundAction(action)) is { } reason)
        {
            throw new ArgumentException(reason, nameof(action));
        }
    }

    private void AddBotActions()
    {
        var fighters = Fighters();

        foreach (var botId in Standing().Where(combatant => combatant.IsBot).Select(combatant => combatant.ParticipantId).Distinct())
        {
            var move = BotStrategy.Decide(botId, fighters);
            Battle.PendingActions[botId] = new BattleAction(botId, move.ActorSlot, move.TargetSlot, move.AbilityId);
        }
    }

    // The battle as the combat rules see it.
    private List<Fighter> Fighters() => [.. Battle.Combatants.Select(combatant => combatant.ToFighter())];

    private static RoundAction ToRoundAction(BattleAction action) =>
        new(action.ParticipantId, action.ActorSlot, action.TargetSlot, action.AbilityId);

    /// <summary>
    /// Resolves the pending actions, arms the next round, stores the
    /// result and only then tells the watchers, so no client sees a
    /// round that is not stored yet. A round that ends the battle
    /// hands it over to the cold store in the same grain turn.
    /// If anything fails before the round is stored, the grain falls
    /// back to the state it last stored, so the round can be played
    /// again from where it stood: the caller's own action is not
    /// stored yet and may be sent again.
    /// </summary>
    private async Task PlayRoundAsync()
    {
        var round = Battle.Round;
        List<RoundEventDto> roundEvents;
        try
        {
            roundEvents = await ResolveRoundAsync();
            await ScheduleNextRoundAsync();
            await state.WriteStateAsync();
            _monstersInDoubt = false;
        }
        catch
        {
            // Some monster grains may already stand where the failed
            // round put them; the next round tells every one of them
            // again.
            _monstersInDoubt = true;
            StopBotLoop();
            await state.ReadStateAsync();
            ResumeBotLoop();
            throw;
        }

        await PublishAsync(round, roundEvents);

        if (Battle.Status == BattleStatus.Completed)
        {
            await HandOverAsync();
        }
    }

    /// <summary>
    /// The hot→cold bridge (ADR-0003), straight-line and without an
    /// outbox: store the snapshot, count a decided battle, then clear
    /// the hot state and deactivate. It runs in the turn that
    /// completed the battle, so no caller ever reads the battle in
    /// between. If the snapshot or the count fails, the hot state is
    /// kept as the source for a replay; if only the clear fails, the
    /// cold store is complete and the hot rows merely stay behind.
    /// It takes no cancellation token on purpose: a half-done
    /// hand-over helps nobody.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any failure of the cold store must keep the hot state and is logged, not rethrown: "
            + "the battle is already completed and published.")]
    private async Task HandOverAsync()
    {
        var battleId = this.GetPrimaryKey();
        try
        {
            await snapshots.AddAsync(ToSnapshot(), CancellationToken.None);
            if (DecidedResult() is { } result)
            {
                await playerStats.RecordResultAsync(result, CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            Log.HandOverFailed(logger, exception, battleId);
            return;
        }

        try
        {
            await Task.WhenAll(Battle.Combatants.Select(combatant => Monster(combatant.Slot).ClearAsync()));
            await state.ClearStateAsync();
        }
        catch (Exception exception)
        {
            Log.ClearFailed(logger, exception, battleId);
        }

        DeactivateOnIdle();
    }

    private BattleSnapshot ToSnapshot() =>
        new()
        {
            BattleId = this.GetPrimaryKey(),
            ArenaId = Battle.ArenaId,
            Round = Battle.Round,
            WinnerId = Battle.WinnerId,
            CompletedAt = clock.GetUtcNow(),
            Difficulty = Battle.Difficulty switch
            {
                Difficulty.Easy => BattleDifficulty.Easy,
                _ => BattleDifficulty.Normal,
            },
            Combatants = [.. Battle.Combatants.Select(combatant => new SnapshotCombatant(
                combatant.Slot,
                combatant.ParticipantId,
                combatant.IsBot,
                combatant.SpeciesId,
                combatant.HitPoints,
                combatant.MaxHitPoints))],
        };

    /// <summary>
    /// The human sides of a decided battle for the player stats.
    /// Bots are left out, so against a bot only the human side
    /// counts; an abandoned or bot-only battle counts for nobody.
    /// </summary>
    /// <returns>The result, or <see langword="null"/> if there is
    /// nothing to count.</returns>
    private BattleResultDto? DecidedResult()
    {
        if (Battle.WinnerId is not { } winnerId)
        {
            return null;
        }

        var humans = Battle.Combatants
            .Where(combatant => !combatant.IsBot)
            .Select(combatant => combatant.ParticipantId)
            .Distinct()
            .ToList();

        return humans.Count == 0
            ? null
            : new BattleResultDto(
                humans.Contains(winnerId) ? winnerId : null,
                [.. humans.Where(id => id != winnerId)]);
    }

    private Task PublishAsync(int round, IReadOnlyList<RoundEventDto> roundEvents) =>
        events.PublishAsync(new BattleEventDto(
            this.GetPrimaryKey(),
            round,
            Battle.Status,
            Battle.WinnerId,
            Battle.TurnDeadline,
            roundEvents,
            BattleResponse.From(Snapshot()).Combatants));

    /// <summary>
    /// Lets the combat rules resolve the pending actions, tells each
    /// monster grain whose monster changed where it stands now, and
    /// takes the round over into the battle's own state. The monster
    /// grains come first and get absolute hit points: if one of the
    /// writes fails, nothing of the round is in the battle's state
    /// yet, and telling a monster the same value again is harmless.
    /// </summary>
    /// <returns>What happened, in resolution order.</returns>
    private async Task<List<RoundEventDto>> ResolveRoundAsync()
    {
        var result = RoundResolver.Resolve(Fighters(), [.. Battle.PendingActions.Values.Select(ToRoundAction)]);

        await Task.WhenAll(result.Fighters
            .Where(after => _monstersInDoubt || after.HitPoints != Battle.Combatants[after.Slot].HitPoints)
            .Select(after => Monster(after.Slot).SetHitPointsAsync(after.HitPoints)));

        foreach (var after in result.Fighters)
        {
            Battle.Combatants[after.Slot].Take(after);
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

        return
        [
            .. result.Events.Select(roundEvent => new RoundEventDto(
                roundEvent.Kind,
                roundEvent.ActorSlot,
                roundEvent.TargetSlot,
                roundEvent.AbilityId,
                roundEvent.Damage,
                roundEvent.Healing,
                roundEvent.TargetHitPoints,
                roundEvent.Effectiveness,
                roundEvent.Effect)),
        ];
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

    // The deadline is cut to whole milliseconds, the precision Table
    // Storage keeps a reminder's start time in. A reminder reloaded
    // after a restart would otherwise tick a fraction of a millisecond
    // before the deadline, be ignored as early, and only come back
    // one retry period later.
    private async Task ArmDeadlineAsync(TimeSpan dueIn)
    {
        var deadline = clock.GetUtcNow() + dueIn;
        Battle.TurnDeadline = deadline.AddTicks(-(deadline.Ticks % TimeSpan.TicksPerMillisecond));
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

    // Re-arms the loop of a bot-only battle that is still running.
    private void ResumeBotLoop()
    {
        if (Battle.Status == BattleStatus.InProgress && HumanSidesStanding().Count == 0)
        {
            StartBotLoop();
        }
    }

    private void StopBotLoop()
    {
        _botLoop?.Dispose();
        _botLoop = null;
    }

    // The timer's token is not passed on: the round that ends the
    // battle stops the loop, which cancels that token while the round
    // still has to store its result and hand the battle over.
    private async Task PlayBotRoundAsync(CancellationToken cancellationToken)
    {
        if (Battle.Status != BattleStatus.InProgress)
        {
            StopBotLoop();
            return;
        }

        AddBotActions();
        await PlayRoundAsync();
    }

    private IEnumerable<Combatant> Standing() => Battle.Combatants.Where(combatant => !combatant.IsDefeated);

    private HashSet<string> SidesStanding() => [.. Standing().Select(combatant => combatant.ParticipantId)];

    private HashSet<string> HumanSidesStanding() =>
        [.. Standing().Where(combatant => !combatant.IsBot).Select(combatant => combatant.ParticipantId)];

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
