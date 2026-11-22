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
		changeOf,
		isStanding,
		outcomeFor,
		secondsUntil,
		type Combatant
	} from '#lib/arena/battle.ts';
	import { ownedName, sentenceOf, type LogNames } from '#lib/arena/battle-log.ts';
	import { abilitiesById, type Ability, type Element } from '#lib/arena/catalog.ts';
	import { LiveBattle } from '#lib/arena/live-battle.svelte.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import AbilityPicker from '#lib/components/AbilityPicker.svelte';
	import CombatantCard from '#lib/components/CombatantCard.svelte';
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
	// No move before the hub answers: the round's delta would be
	// missed. Without a connection the HTTP response still shows it.
	const canAct = $derived(
		player.loaded && playerId !== null && inProgress && live.feed !== 'connecting'
	);
	const outcome = $derived(playerId ? outcomeFor(battle, playerId) : 'undecided');
	const secondsLeft = $derived(secondsUntil(battle.turnDeadline, now));
	const arenaName = $derived(
		data.catalog.arenas.find((arena) => arena.id === battle.arenaId)?.name ?? battle.arenaId
	);
	const lastRound = $derived(live.rounds.at(-1));

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

	function lastChangeOf(
		slot: number
	): { round: number; damage: number; healing: number } | undefined {
		return lastRound && { round: lastRound.round, ...changeOf(lastRound.events, slot) };
	}

	function pickAbility(abilityId: string) {
		pickedAbility = { abilityId, round: battle.round };
	}

	function submit() {
		if (playerId && move) {
			void live.act(playerId, move);
		}
	}
</script>

<svelte:head>
	<title>Battle in {arenaName} · Orleans Monster Arena</title>
</svelte:head>

<div class="flex flex-col gap-4">
	<section class="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
		<h1 class="text-2xl font-bold">{arenaName}</h1>
		<p class="text-slate-300">
			<span data-testid="round">Round {battle.round}</span>
			{#if canAct && secondsLeft !== null}
				<span class="text-slate-500">·</span>
				<span class={secondsLeft <= 10 ? 'text-red-400' : ''} data-testid="countdown">
					{secondsLeft}s to move
				</span>
			{/if}
		</p>
	</section>

	{#if live.feed === 'offline' && inProgress}
		<p class="rounded border border-amber-500 p-3 text-sm text-amber-300" role="status">
			The live connection is down. Reload the page to catch up.
		</p>
	{/if}

	{#if battle.status === 'Completed'}
		<section
			class="rounded-lg border border-slate-700 bg-slate-900 p-4 text-center"
			data-testid="battle-result"
		>
			<h2 class="text-xl font-bold">
				{#if outcome === 'won'}
					You won!
				{:else if outcome === 'lost'}
					The bot won.
				{:else if battle.winnerId === null}
					The battle was abandoned.
				{:else}
					{battle.winnerId} won.
				{/if}
			</h2>
			<p class="mt-1 text-sm text-slate-400">
				{#if battle.winnerId === null}
					Nobody moved before the deadline.
				{:else}
					Decided in round {battle.round}.
				{/if}
			</p>
			<p class="mt-4 flex flex-wrap justify-center gap-3">
				<a
					class="rounded bg-amber-500 px-4 py-2 font-semibold text-slate-950"
					href={resolve('/arenas/[arenaId]', { arenaId: battle.arenaId })}
				>
					Fight again
				</a>
				<a class="rounded border border-slate-600 px-4 py-2" href={resolve('/')}>All arenas</a>
			</p>
		</section>
	{/if}

	{#snippet side(
		label: string,
		testId: string,
		combatants: Combatant[],
		picking: boolean,
		picked: Combatant | undefined,
		onpick: (slot: number) => void
	)}
		<section aria-label={label} data-testid={testId}>
			<h2 class="mb-1 text-sm font-semibold text-slate-400">{label}</h2>
			<ul class="flex justify-center gap-2">
				{#each combatants as combatant (combatant.slot)}
					<li class="max-w-40 min-w-0 flex-1 basis-0">
						<CombatantCard
							{combatant}
							name={nameOf(combatant.speciesId)}
							element={elementOf(combatant.speciesId)}
							owner={ownerOf(combatant)}
							selectable={picking && isStanding(combatant)}
							selected={picking && picked?.slot === combatant.slot}
							lastChange={lastChangeOf(combatant.slot)}
							onselect={() => onpick(combatant.slot)}
						/>
					</li>
				{/each}
			</ul>
		</section>
	{/snippet}

	{@render side(
		away[0] ? ownerOf(away[0]) : 'Opponent',
		'away-side',
		away,
		canAct && targeted,
		target,
		(slot) => (pickedTarget = slot)
	)}
	{@render side(
		home[0] ? ownerOf(home[0]) : 'Challenger',
		'home-side',
		home,
		canAct,
		actor,
		(slot) => (pickedActor = slot)
	)}

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
