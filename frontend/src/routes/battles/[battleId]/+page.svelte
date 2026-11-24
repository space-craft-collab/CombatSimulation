<script lang="ts">
	import { untrack } from 'svelte';
	import { resolve } from '$app/paths';
	import {
		STRIKE,
		chooseAbility,
		matchupHint,
		moveOf,
		needsTarget,
		unknownAbility
	} from '#lib/arena/abilities.ts';
	import {
		clockOf,
		isStanding,
		secondsUntil,
		type Combatant,
		type Difficulty
	} from '#lib/arena/battle.ts';
	import { ownedName, sentenceOf, type LogNames } from '#lib/arena/battle-log.ts';
	import { abilitiesById, type Ability, type Element } from '#lib/arena/catalog.ts';
	import { afterQuery, playerParam } from '#lib/arena/dashboard.ts';
	import { LiveBattle } from '#lib/arena/live-battle.svelte.ts';
	import { inviteQuery } from '#lib/arena/lobby.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import { Playback } from '#lib/arena/playback.svelte.ts';
	import { cueOf, hitPointsAt } from '#lib/arena/round-playback.ts';
	import { pickerQuery } from '#lib/arena/team.ts';
	import { verdictOf } from '#lib/arena/verdict.ts';
	import AbilityPicker from '#lib/components/AbilityPicker.svelte';
	import BattleStage from '#lib/components/BattleStage.svelte';
	import DifficultyChoice from '#lib/components/DifficultyChoice.svelte';
	import type { StageCue, StageMonster } from '#lib/components/stage.ts';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// A new battle in the page data (a reload, or a navigation to
	// another battle) starts over; the old one is let go below.
	const live = $derived(new LiveBattle(data.battle));
	const battle = $derived(live.battle);
	const abilities = $derived(abilitiesById(data.catalog.species));

	$effect(() => {
		const followed = live;
		// Untracked: following reads the battle's state, and a new
		// round must not tear the connection down and open another.
		untrack(() => void followed.follow());

		return () => void followed.dispose();
	});

	// The stage plays each round that comes in event by event. The
	// battle's state is that round's end already, and every pick goes
	// by it: only what the stage draws lags behind.
	const playback = new Playback();

	// Before the stage is drawn, so the round's end does not show first.
	$effect.pre(() => {
		const played = live.rounds.at(-1);
		// A new battle has no rounds yet: the old one's playback goes.
		untrack(() => (played ? playback.play(played) : playback.clear()));
	});

	$effect(() => () => playback.clear());

	// The countdown's clock. It ticks in the browser only.
	let now = $state(Date.now());

	$effect(() => {
		const timer = setInterval(() => (now = Date.now()), 250);

		return () => clearInterval(timer);
	});

	// The viewer's side, if the nickname fights here; anyone else watches.
	const playerId = $derived(
		battle.combatants.some((combatant) => combatant.participantId === player.nickname)
			? player.nickname
			: null
	);
	// A spectator sees the side in the first slot below.
	const homeId = $derived(playerId ?? battle.combatants[0]?.participantId);
	const home = $derived(battle.combatants.filter((entry) => entry.participantId === homeId));
	const away = $derived(battle.combatants.filter((entry) => entry.participantId !== homeId));

	// What the viewer tapped. A tapped monster that fell is replaced
	// by the first one standing, so a move is always ready to send.
	let pickedActor = $state<number | null>(null);
	let pickedTarget = $state<number | null>(null);
	// A tapped ability holds for the round it was tapped in only: once
	// used it is on cooldown, and the next round starts on the default
	// again instead of jumping back to it the moment it is ready.
	let pickedAbility = $state<{ abilityId: string; round: number } | null>(null);

	const actor = $derived(pick(home, pickedActor));
	const target = $derived(pick(away, pickedTarget));
	const ability = $derived.by(() => {
		if (!actor) {
			return undefined;
		}

		const picked = pickedAbility?.round === battle.round ? pickedAbility.abilityId : null;

		// A monster the battle lists no abilities for still has the basic attack.
		return abilityOf(chooseAbility(actor, picked) ?? STRIKE);
	});
	// Guard and heal are used on the actor itself: no target to pick.
	const targeted = $derived(ability === undefined || needsTarget(ability));
	const move = $derived(actor && ability ? moveOf(actor, ability, target) : null);
	const hint = $derived(
		ability && target && targeted ? matchupHint(ability, elementOf(target.speciesId)) : null
	);

	const inProgress = $derived(battle.status === 'InProgress');
	const waiting = $derived(battle.status === 'WaitingForPlayers');
	// No move before the hub answers: the round's delta would be
	// missed. Without a connection the HTTP response still shows it.
	// Once the move is in, the round waits for the other side.
	const canAct = $derived(
		player.loaded && playerId !== null && inProgress && live.feed !== 'connecting' && !live.moved
	);
	const verdict = $derived(verdictOf(battle, playerId));
	// While the battle waits for players, its deadline is the join deadline.
	const secondsLeft = $derived(secondsUntil(battle.turnDeadline, now));
	// The bot the host may bring in instead of waiting.
	let botDifficulty = $state<Difficulty>('Normal');
	const arenaName = $derived(
		data.catalog.arenas.find((arena) => arena.id === battle.arenaId)?.name ?? battle.arenaId
	);
	// "Fight again" starts the picker from the team the viewer just
	// fielded and the difficulty it was fielded on; a spectator gets
	// an empty picker.
	const fightAgain = $derived(
		resolve('/arenas/[arenaId]', { arenaId: battle.arenaId }) +
			(playerId
				? pickerQuery(
						home.toSorted((a, b) => a.slot - b.slot).map((combatant) => combatant.speciesId),
						battle.difficulty
					)
				: '')
	);

	// A player's way from the result to their dashboard, which waits
	// until this battle is counted there.
	const record = $derived(
		playerId
			? resolve('/players/[playerId]', { playerId: playerParam(playerId) }) +
					afterQuery(battle.battleId)
			: null
	);

	const names: LogNames = {
		monster: (slot) => {
			const combatant = battle.combatants.find((entry) => entry.slot === slot);

			return combatant ? ownedName(combatant, playerId, nameOf(combatant.speciesId)) : 'a monster';
		},
		ability: (id) => abilityOf(id).name
	};

	function pick(side: Combatant[], picked: number | null): Combatant | undefined {
		const standing = side.filter(isStanding);

		return standing.find((combatant) => combatant.slot === picked) ?? standing[0];
	}

	function abilityOf(id: string): Ability {
		return abilities.get(id) ?? unknownAbility(id);
	}

	function nameOf(speciesId: string): string {
		return data.catalog.species.find((species) => species.id === speciesId)?.name ?? speciesId;
	}

	function elementOf(speciesId: string): Element {
		return data.catalog.species.find((species) => species.id === speciesId)?.element ?? 'Neutral';
	}

	function ownerOf(combatant: Combatant): string {
		if (combatant.participantId === playerId) {
			return 'You';
		}

		return combatant.isBot ? 'Bot' : combatant.participantId;
	}

	function shownHitPoints(combatant: Combatant): number {
		const played = playback.round;

		return played && playback.playing
			? hitPointsAt(played.events, playback.step, combatant)
			: combatant.hitPoints;
	}

	function cueAt(slot: number): StageCue | undefined {
		const played = playback.round;
		const cue = played && cueOf(played.events, playback.step, slot);

		return played && cue ? { ...cue, key: `${played.round}-${cue.step}` } : undefined;
	}

	/** One side as the stage shows it; `picking` while the viewer may pick there. */
	function onStage(
		combatants: Combatant[],
		picking: boolean,
		picked: Combatant | undefined,
		onpick: (slot: number) => void
	): StageMonster[] {
		return combatants.map((combatant) => ({
			combatant,
			hitPoints: shownHitPoints(combatant),
			name: nameOf(combatant.speciesId),
			element: elementOf(combatant.speciesId),
			owner: ownerOf(combatant),
			selectable: picking && isStanding(combatant),
			selected: picking && picked?.slot === combatant.slot,
			cue: cueAt(combatant.slot),
			onselect: () => onpick(combatant.slot)
		}));
	}

	function pickAbility(abilityId: string) {
		pickedAbility = { abilityId, round: battle.round };
	}

	function submit() {
		if (playerId && move) {
			void live.act(playerId, move);
		}
	}

	function seatBot() {
		void live.seatBot(botDifficulty);
	}
</script>

<svelte:head>
	<title>Battle in {arenaName} · Orleans Monster Arena</title>
</svelte:head>

<div class="flex flex-col gap-4">
	<!-- The title rides on the stage: below it, a phone has no room to spare. -->
	<BattleStage
		arenaId={battle.arenaId}
		away={{
			label: away[0] ? ownerOf(away[0]) : 'Opponent',
			monsters: onStage(away, canAct && targeted, target, (slot) => (pickedTarget = slot))
		}}
		home={{
			label: home[0] ? ownerOf(home[0]) : 'Challenger',
			monsters: onStage(home, canAct, actor, (slot) => (pickedActor = slot))
		}}
		emptySeat={waiting ? 'Waiting for an opponent…' : 'Nobody joined.'}
	>
		{#snippet title()}
			<h1
				class="flex items-baseline gap-2 text-base font-bold text-shadow-black/60 text-shadow-sm sm:text-xl"
			>
				{arenaName}
				{#if battle.difficulty === 'Easy'}
					<span
						class="rounded-full border border-emerald-500/60 bg-slate-950/60 px-2 py-0.5 text-xs font-semibold text-emerald-300 text-shadow-none"
						title="The bot's monsters entered weakened"
						data-testid="difficulty">Easy</span
					>
				{/if}
			</h1>
			<p class="text-sm text-slate-200 text-shadow-black/60 text-shadow-sm">
				{#if waiting}
					<span data-testid="round">Open battle</span>
				{:else if battle.round > 0}
					<span data-testid="round">Round {battle.round}</span>
				{/if}
				{#if (canAct || live.moved) && secondsLeft !== null}
					<span class="text-slate-400">·</span>
					<span class={secondsLeft <= 10 ? 'text-red-400' : ''} data-testid="countdown">
						{secondsLeft}s {canAct ? 'to move' : 'left'}
					</span>
				{/if}
			</p>
		{/snippet}
	</BattleStage>

	{#if live.feed === 'offline' && (inProgress || waiting)}
		<p class="rounded border border-amber-500 p-3 text-sm text-amber-300" role="status">
			The live connection is down. Reload the page to catch up.
		</p>
	{/if}

	{#if waiting && player.loaded}
		<section
			class="flex flex-col gap-3 rounded-lg border border-slate-700 bg-slate-900 p-4"
			data-testid="waiting"
		>
			{#if playerId}
				<div>
					<h2 class="text-xl font-bold">Waiting for an opponent</h2>
					<p class="mt-1 text-sm text-slate-400">
						Players who open {arenaName} see your battle and can join it; you can also send them this
						page's address.
						{#if secondsLeft !== null && secondsLeft > 0}
							It stays open for another
							<span class="text-slate-200 tabular-nums" data-testid="join-countdown"
								>{clockOf(secondsLeft)}</span
							>.
						{:else if secondsLeft === 0}
							Its time is up; it closes in a moment.
						{/if}
					</p>
				</div>
				<!-- Offered at once, not after a while (ADR-0007). -->
				<div class="flex flex-col gap-2 border-t border-slate-800 pt-3" data-testid="bot-offer">
					<p class="font-medium">Rather not wait? Play the bot instead.</p>
					<DifficultyChoice bind:value={botDifficulty} disabled={live.submitting} />
					<button
						class="rounded-lg bg-amber-500 px-4 py-3 text-lg font-semibold text-slate-950 enabled:cursor-pointer disabled:opacity-40"
						type="button"
						disabled={live.submitting}
						onclick={seatBot}
						data-testid="bot-instead"
					>
						{live.submitting ? 'Bringing in the bot…' : 'Fight the bot instead'}
					</button>
					{#if live.failure}
						<p class="text-sm text-red-400" role="alert">{live.failure}</p>
					{/if}
				</div>
			{:else}
				<div>
					<h2 class="text-xl font-bold">{homeId} is waiting for an opponent</h2>
					<p class="mt-1 text-sm text-slate-400">Pick a team in {arenaName} to take them on.</p>
				</div>
				<!-- The path comes from resolve(); the rule only cannot see past the query appended to it. -->
				<!-- eslint-disable svelte/no-navigation-without-resolve -->
				<a
					class="rounded-lg bg-amber-500 px-4 py-3 text-center text-lg font-semibold text-slate-950"
					href={resolve('/arenas/[arenaId]', { arenaId: battle.arenaId }) +
						inviteQuery(battle.battleId)}
					data-testid="join-link"
				>
					Pick a team and join
				</a>
				<!-- eslint-enable svelte/no-navigation-without-resolve -->
			{/if}
		</section>
	{/if}

	{#if verdict}
		<section
			class="rounded-lg border border-slate-700 bg-slate-900 p-4 text-center"
			data-testid="battle-result"
		>
			<h2 class="text-xl font-bold">{verdict.title}</h2>
			<p class="mt-1 text-sm text-slate-400">{verdict.detail}</p>
			<p class="mt-4 flex flex-wrap justify-center gap-3">
				<!-- The path comes from resolve(); the rule only cannot see past the query appended to it. -->
				<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
				<a class="rounded bg-amber-500 px-4 py-2 font-semibold text-slate-950" href={fightAgain}>
					Fight again
				</a>
				<a class="rounded border border-slate-600 px-4 py-2" href={resolve('/')}>All arenas</a>
			</p>
			{#if record}
				<p class="mt-3 text-sm">
					<!-- The path comes from resolve(); the rule only cannot see past the query appended to it. -->
					<!-- eslint-disable svelte/no-navigation-without-resolve -->
					<a
						class="text-slate-300 underline underline-offset-4"
						href={record}
						data-testid="record-link">Your record and last battles</a
					>
					<!-- eslint-enable svelte/no-navigation-without-resolve -->
				</p>
			{/if}
		</section>
	{/if}

	{#if canAct && actor && ability && move}
		<section aria-labelledby="abilities-heading">
			<h2 id="abilities-heading" class="mb-1 text-sm font-semibold text-slate-400">
				{nameOf(actor.speciesId)}'s abilities
			</h2>
			<AbilityPicker
				abilities={actor.abilities.map((cooldown) => ({
					ability: abilityOf(cooldown.abilityId),
					cooldown
				}))}
				selected={ability.id}
				onselect={pickAbility}
			/>
		</section>

		<!-- Stays in reach while the log below grows. -->
		<section class="sticky bottom-0 flex flex-col gap-2 bg-slate-950/95 py-3">
			{#if hint}
				<p
					class={[
						'text-center text-sm font-medium',
						hint.effectiveness === 'Strong' ? 'text-emerald-300' : 'text-red-300'
					]}
					data-testid="matchup"
				>
					{ability.name} is {hint.text}.
				</p>
			{/if}
			<button
				class="rounded-lg bg-amber-500 px-4 py-3 text-lg leading-snug font-semibold text-slate-950 disabled:opacity-40"
				type="button"
				disabled={live.submitting}
				onclick={submit}
				data-testid="strike"
			>
				{#if targeted && target}
					{nameOf(actor.speciesId)} uses {ability.name} on {nameOf(target.speciesId)}
				{:else}
					{nameOf(actor.speciesId)} uses {ability.name}
				{/if}
			</button>
			<p class="text-center text-xs text-slate-400">
				{targeted
					? 'Tap a monster to change who acts or whom it targets.'
					: 'Tap one of your monsters to change who acts.'}
			</p>
			{#if live.failure}
				<p class="text-sm text-red-400" role="alert">{live.failure}</p>
			{/if}
		</section>
	{:else if live.moved}
		<!-- Against a human, the round waits for the other side's move. -->
		<section
			class="sticky bottom-0 flex flex-col gap-1 bg-slate-950/95 py-3 text-center"
			role="status"
			data-testid="waiting-for-move"
		>
			<p class="text-lg font-semibold">Your move is in.</p>
			<p class="text-sm text-slate-400">
				Waiting for {away[0] ? ownerOf(away[0]) : 'your opponent'}. The round is played once they
				move{secondsLeft !== null ? `, or in ${secondsLeft}s` : ''}.
			</p>
		</section>
	{:else if player.loaded && playerId === null && inProgress}
		<p class="text-center text-sm text-slate-400">You are watching this battle.</p>
	{/if}

	{#if live.rounds.length > 0}
		<section aria-labelledby="log-heading">
			<h2 id="log-heading" class="mb-2 text-lg font-semibold">Battle log</h2>
			<ol class="flex flex-col gap-2 text-sm" data-testid="battle-log">
				{#each live.rounds.toReversed() as played (played.round)}
					<li class="rounded border border-slate-800 p-2">
						<span class="font-medium text-slate-300">Round {played.round}</span>
						<ul class="mt-1 text-slate-400">
							{#each played.events as happened, index (index)}
								<li>{sentenceOf(happened, names)}</li>
							{:else}
								<li>No move was made.</li>
							{/each}
						</ul>
					</li>
				{/each}
			</ol>
		</section>
	{/if}
</div>
