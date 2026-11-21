<script lang="ts">
	import { untrack } from 'svelte';
	import { resolve } from '$app/paths';
	import {
		isStanding,
		outcomeFor,
		secondsUntil,
		type Combatant,
		type Strike
	} from '#lib/arena/battle.ts';
	import { LiveBattle } from '#lib/arena/live-battle.svelte.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import CombatantCard from '#lib/components/CombatantCard.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// A new battle in the page data (a reload, or a navigation to
	// another battle) starts over; the old one is let go below.
	const live = $derived(new LiveBattle(data.battle));
	const battle = $derived(live.battle);

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

	const actor = $derived(pick(home, pickedActor));
	const target = $derived(pick(away, pickedTarget));

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

	function pick(side: Combatant[], picked: number | null): Combatant | undefined {
		const standing = side.filter(isStanding);

		return standing.find((combatant) => combatant.slot === picked) ?? standing[0];
	}

	function nameOf(speciesId: string): string {
		return data.catalog.species.find((species) => species.id === speciesId)?.name ?? speciesId;
	}

	function ownerOf(combatant: Combatant): string {
		if (combatant.participantId === playerId) {
			return 'You';
		}

		return combatant.isBot ? 'Bot' : combatant.participantId;
	}

	// "your Ember Fox", "the bot's Lava Crab", "ada's Frost Owl"
	function describe(slot: number): string {
		const combatant = battle.combatants.find((entry) => entry.slot === slot);

		if (!combatant) {
			return 'a monster';
		}

		const owner =
			combatant.participantId === playerId
				? 'your'
				: combatant.isBot
					? "the bot's"
					: `${combatant.participantId}'s`;

		return `${owner} ${nameOf(combatant.speciesId)}`;
	}

	// "Your Ember Fox hits the bot's Lava Crab for 6 and defeats it."
	function sentence(hit: Strike): string {
		const actorName = describe(hit.actorSlot);
		const outcome = hit.targetHitPoints === 0 ? ' and defeats it' : '';

		return (
			actorName.charAt(0).toUpperCase() +
			actorName.slice(1) +
			` hits ${describe(hit.targetSlot)} for ${hit.damage}${outcome}.`
		);
	}

	function lastHitOf(slot: number): { round: number; damage: number } | undefined {
		const strike = lastRound?.strikes.findLast((entry) => entry.targetSlot === slot);

		return strike && lastRound ? { round: lastRound.round, damage: strike.damage } : undefined;
	}

	function strike() {
		if (playerId && actor && target) {
			void live.strike(playerId, actor.slot, target.slot);
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
		combatants: Combatant[],
		picked: Combatant | undefined,
		onpick: (slot: number) => void
	)}
		<section aria-label={label}>
			<h2 class="mb-1 text-xs font-semibold tracking-widest text-slate-500 uppercase">{label}</h2>
			<ul class="flex justify-center gap-2">
				{#each combatants as combatant (combatant.slot)}
					<li class="max-w-40 min-w-0 flex-1 basis-0">
						<CombatantCard
							{combatant}
							name={nameOf(combatant.speciesId)}
							owner={ownerOf(combatant)}
							selectable={canAct && isStanding(combatant)}
							selected={canAct && picked?.slot === combatant.slot}
							lastHit={lastHitOf(combatant.slot)}
							onselect={() => onpick(combatant.slot)}
						/>
					</li>
				{/each}
			</ul>
		</section>
	{/snippet}

	{@render side(
		away[0] ? ownerOf(away[0]) : 'Opponent',
		away,
		target,
		(slot) => (pickedTarget = slot)
	)}
	{@render side(
		home[0] ? ownerOf(home[0]) : 'Challenger',
		home,
		actor,
		(slot) => (pickedActor = slot)
	)}

	{#if canAct && actor && target}
		<!-- Stays in reach while the log below grows. -->
		<section class="sticky bottom-0 flex flex-col gap-2 bg-slate-950/95 py-3">
			<button
				class="rounded-lg bg-amber-500 px-4 py-3 text-lg font-semibold text-slate-950 disabled:opacity-40"
				type="button"
				disabled={live.submitting}
				onclick={strike}
				data-testid="strike"
			>
				{nameOf(actor.speciesId)} strikes {nameOf(target.speciesId)}
			</button>
			<p class="text-center text-xs text-slate-400">
				Tap a monster to change who strikes or who is struck.
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
							{#each played.strikes as hit, index (index)}
								<li>{sentence(hit)}</li>
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
