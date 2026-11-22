<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { STRIKE, summaryOf } from '#lib/arena/abilities.ts';
	import type { Difficulty } from '#lib/arena/battle.ts';
	import { ArenaApiError, startBattleAgainstBot } from '#lib/arena/battles.ts';
	import type { Species } from '#lib/arena/catalog.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import {
		MAX_TEAM_SIZE,
		countOf,
		loadStoredTeam,
		statCeilings,
		storeTeam
	} from '#lib/arena/team.ts';
	import ArenaBackdrop from '#lib/components/ArenaBackdrop.svelte';
	import ElementCycle from '#lib/components/ElementCycle.svelte';
	import NicknameForm from '#lib/components/NicknameForm.svelte';
	import SpeciesCard from '#lib/components/SpeciesCard.svelte';
	import TeamDock from '#lib/components/TeamDock.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Species ids in slot order; the same species may be fielded twice.
	let team = $state<string[]>([]);
	let difficulty = $state<Difficulty>('Normal');
	let starting = $state(false);
	let failure = $state<string | null>(null);

	// The team picked last time, so "Fight again" need not start from
	// nothing. It lives in the browser only, like the nickname.
	onMount(() => {
		team = loadStoredTeam(data.species);
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
			team.push(speciesId);
			storeTeam(team);
		}
	}

	function remove(slot: number) {
		team.splice(slot, 1);
		storeTeam(team);
	}

	async function start() {
		if (!player.nickname || team.length === 0) {
			return;
		}

		starting = true;
		failure = null;

		try {
			const battle = await startBattleAgainstBot(data.arena.id, player.nickname, team, difficulty);
			await goto(resolve('/battles/[battleId]', { battleId: battle.battleId }));
		} catch (error) {
			failure = error instanceof ArenaApiError ? error.message : 'The battle could not be started.';
			starting = false;
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
			Field one to {MAX_TEAM_SIZE} monsters against the bot. The same one may fight twice.
		</p>
	</section>

	{#if player.loaded && !player.nickname}
		<section class="rounded-lg border border-slate-700 bg-slate-900 p-4">
			<h2 class="mb-3 text-lg font-semibold">Choose a nickname to play</h2>
			<NicknameForm />
		</section>
	{/if}

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
		ready={player.nickname !== null && team.length > 0}
		{starting}
		{failure}
		bind:difficulty
		onremove={remove}
		onstart={start}
	/>
</div>
