<script lang="ts">
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { STRIKE, summaryOf } from '#lib/arena/abilities.ts';
	import type { Battle, Difficulty } from '#lib/arena/battle.ts';
	import {
		ArenaApiError,
		joinBattle,
		openBattle,
		startBattleAgainstBot
	} from '#lib/arena/battles.ts';
	import type { Species } from '#lib/arena/catalog.ts';
	import { joinRefusal, lobbyOf } from '#lib/arena/lobby.ts';
	import { OpenBattleList } from '#lib/arena/open-battles.svelte.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import { MAX_TEAM_SIZE, countOf, statCeilings } from '#lib/arena/team.ts';
	import ArenaBackdrop from '#lib/components/ArenaBackdrop.svelte';
	import ElementCycle from '#lib/components/ElementCycle.svelte';
	import NicknameForm from '#lib/components/NicknameForm.svelte';
	import SpeciesCard from '#lib/components/SpeciesCard.svelte';
	import TeamDock from '#lib/components/TeamDock.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Species ids in slot order; the same species may be fielded twice.
	// Both start from what "Fight again" handed over, if anything,
	// and start over when the page data does (another arena).
	let team = $derived([...data.team]);
	let difficulty = $derived<Difficulty>(data.difficulty);
	// What is on its way: one request at a time.
	let pending = $state<'bot' | 'human' | 'join' | null>(null);
	let failure = $state<string | null>(null);
	let joinFailure = $state<string | null>(null);
	// The open battle a join is on its way to.
	let joiningId = $state<string | null>(null);

	const ready = $derived(player.nickname !== null && team.length > 0);
	const openBattles = $derived(new OpenBattleList(data.arena.id, data.openBattles));
	const lobby = $derived(lobbyOf(openBattles.battles, player.nickname, data.invitedId));

	$effect(() => {
		const list = openBattles;

		return untrack(() => list.poll());
	});

	const full = $derived(team.length >= MAX_TEAM_SIZE);
	const ceilings = $derived(statCeilings(data.species));
	const members = $derived(
		team
			.map((speciesId) => data.species.find((species) => species.id === speciesId))
			.filter((species): species is Species => species !== undefined)
	);
	// Every species knows it, so it is named once and not on each card.
	const strike = $derived(
		data.species.flatMap((species) => species.abilities).find((ability) => ability.id === STRIKE)
	);

	function add(speciesId: string) {
		if (!full) {
			team = [...team, speciesId];
		}
	}

	function remove(slot: number) {
		team = team.toSpliced(slot, 1);
	}

	function start() {
		void enter('bot', (nickname) =>
			startBattleAgainstBot(data.arena.id, nickname, team, difficulty)
		);
	}

	function wait() {
		void enter('human', (nickname) => openBattle(data.arena.id, nickname, team));
	}

	function join(battleId: string) {
		if (pending === null) {
			joiningId = battleId;
			void enter('join', (nickname) => joinBattle(battleId, nickname, team));
		}
	}

	// Starts, opens or joins a battle with the picked team and goes to it.
	async function enter(
		kind: 'bot' | 'human' | 'join',
		request: (nickname: string) => Promise<Battle>
	) {
		if (!player.nickname || team.length === 0 || pending !== null) {
			return;
		}

		pending = kind;
		failure = null;
		joinFailure = null;

		try {
			const battle = await request(player.nickname);
			await goto(resolve('/battles/[battleId]', { battleId: battle.battleId }));
		} catch (error) {
			if (kind === 'join') {
				joinFailure =
					error instanceof ArenaApiError
						? joinRefusal(error.status, error.message)
						: 'The battle could not be joined.';
				// The battle that was refused is likely gone from the list.
				void openBattles.refresh();
			} else {
				failure =
					error instanceof ArenaApiError ? error.message : 'The battle could not be started.';
			}

			pending = null;
		}
	}
</script>

<svelte:head>
	<title>{data.arena.name} · Orleans Monster Arena</title>
</svelte:head>

<div class="flex flex-col gap-6">
	<section>
		<a class="text-sm text-slate-400 underline" href={resolve('/')}>← All arenas</a>
		<div
			class="relative isolate mt-2 flex aspect-16/5 min-h-28 items-end overflow-hidden rounded-lg border border-slate-800 bg-slate-900 p-4"
		>
			<ArenaBackdrop arenaId={data.arena.id} />
			<h1 class="text-2xl font-bold text-shadow-md">{data.arena.name}</h1>
		</div>
		<p class="mt-3 text-slate-300">
			Field one to {MAX_TEAM_SIZE} monsters against the bot or another player. The same one may fight
			twice.
		</p>
	</section>

	{#if player.loaded && !player.nickname}
		<section class="rounded-lg border border-slate-700 bg-slate-900 p-4">
			<h2 class="mb-3 text-lg font-semibold">Choose a nickname to play</h2>
			<NicknameForm />
		</section>
	{/if}

	<section aria-labelledby="open-battles-heading" data-testid="open-battles">
		<h2 id="open-battles-heading" class="text-lg font-semibold">Players waiting</h2>
		{#if lobby.own.length === 0 && lobby.joinable.length === 0}
			<p class="mt-1 text-sm text-slate-400">
				Nobody is waiting for an opponent here right now. Pick a team and tap “Wait for a human” to
				open a battle someone can join.
			</p>
		{:else}
			<ul class="mt-2 flex flex-col gap-2">
				{#each lobby.own as open (open.battleId)}
					<li
						class="flex items-center justify-between gap-3 rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
						data-testid="own-battle"
					>
						<span class="text-sm text-slate-300">Your battle waits for an opponent.</span>
						<a
							class="grid min-h-10 shrink-0 place-items-center rounded-lg border border-slate-600 px-3 text-sm font-medium"
							href={resolve('/battles/[battleId]', { battleId: open.battleId })}
						>
							Back to it
						</a>
					</li>
				{/each}
				{#each lobby.joinable as open (open.battleId)}
					<li
						class={[
							'flex items-center justify-between gap-3 rounded-lg border bg-slate-900 px-3 py-2',
							open.battleId === data.invitedId ? 'border-amber-400' : 'border-slate-700'
						]}
						data-testid="open-battle"
						data-battle-id={open.battleId}
					>
						<span class="min-w-0 truncate text-sm">
							<span class="font-semibold">{open.hostId}</span>
							<span class="text-slate-400">is waiting</span>
						</span>
						<button
							class="min-h-10 shrink-0 rounded-lg bg-slate-200 px-4 text-sm font-semibold text-slate-950 enabled:cursor-pointer disabled:opacity-40"
							type="button"
							disabled={!ready || pending !== null}
							onclick={() => join(open.battleId)}
						>
							{pending === 'join' && joiningId === open.battleId ? 'Joining…' : 'Join'}
							<span class="sr-only">{open.hostId}'s battle</span>
						</button>
					</li>
				{/each}
			</ul>
			{#if lobby.joinable.length > 0 && !ready}
				<p class="mt-2 text-sm text-slate-400">
					{player.nickname
						? 'Pick your team below, then join.'
						: 'Choose a nickname and a team to join.'}
				</p>
			{/if}
		{/if}
		{#if joinFailure}
			<p class="mt-2 text-sm text-red-400" role="alert">{joinFailure}</p>
		{/if}
	</section>

	<section aria-labelledby="species-heading">
		<h2 id="species-heading" class="text-lg font-semibold">Monsters</h2>
		<div class="mt-2 mb-4 flex flex-col gap-2">
			<ElementCycle />
			{#if strike}
				<p class="text-sm text-slate-400">
					Every monster also knows {strike.name} ({summaryOf(strike)}).
				</p>
			{/if}
		</div>
		<ul class="grid gap-3 sm:grid-cols-2">
			{#each data.species as species (species.id)}
				<SpeciesCard
					{species}
					{ceilings}
					picked={countOf(team, species.id)}
					addable={!full}
					onadd={() => add(species.id)}
				/>
			{/each}
		</ul>
	</section>

	<TeamDock
		team={members}
		{ready}
		{pending}
		{failure}
		bind:difficulty
		onremove={remove}
		onstart={start}
		onwait={wait}
	/>
</div>
