<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { ArenaApiError, startBattleAgainstBot } from '#lib/arena/battles.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import MonsterSprite from '#lib/components/MonsterSprite.svelte';
	import NicknameForm from '#lib/components/NicknameForm.svelte';
	import type { PageProps } from './$types';

	/** The most monsters one side may field (`StartBattleRequest.MaxTeamSize`). */
	const MAX_TEAM_SIZE = 3;

	let { data }: PageProps = $props();

	// Species ids in slot order; the same species may be fielded twice.
	let team = $state<string[]>([]);
	let starting = $state(false);
	let failure = $state<string | null>(null);

	const full = $derived(team.length >= MAX_TEAM_SIZE);

	function nameOf(speciesId: string): string {
		return data.species.find((species) => species.id === speciesId)?.name ?? speciesId;
	}

	async function start() {
		if (!player.nickname || team.length === 0) {
			return;
		}

		starting = true;
		failure = null;

		try {
			const battle = await startBattleAgainstBot(data.arena.id, player.nickname, team);
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
		<h1 class="mt-2 text-2xl font-bold">{data.arena.name}</h1>
		<p class="mt-1 text-slate-300">
			Field one to {MAX_TEAM_SIZE} monsters. The bot fields the same.
		</p>
	</section>

	<section aria-labelledby="team-heading">
		<h2 id="team-heading" class="mb-3 text-lg font-semibold">
			Your team ({team.length}/{MAX_TEAM_SIZE})
		</h2>
		{#if team.length === 0}
			<p class="rounded-lg border border-dashed border-slate-700 p-4 text-sm text-slate-400">
				Add a monster from the list below.
			</p>
		{:else}
			<ol class="flex flex-wrap gap-2">
				{#each team as speciesId, slot (slot)}
					<li>
						<button
							class="flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900 py-1 pr-3 pl-1 hover:border-red-400"
							type="button"
							onclick={() => team.splice(slot, 1)}
							data-testid="team-member"
						>
							<MonsterSprite {speciesId} class="h-10 w-10" />
							<span>{nameOf(speciesId)}</span>
							<span class="text-slate-400" aria-hidden="true">✕</span>
							<span class="sr-only">Remove from the team</span>
						</button>
					</li>
				{/each}
			</ol>
		{/if}
	</section>

	<section aria-labelledby="species-heading">
		<h2 id="species-heading" class="mb-3 text-lg font-semibold">Monsters</h2>
		<ul class="grid gap-3 sm:grid-cols-2">
			{#each data.species as species (species.id)}
				<li
					class="flex items-center gap-3 rounded-lg border border-slate-700 bg-slate-900 p-3"
					data-testid="species-{species.id}"
				>
					<MonsterSprite speciesId={species.id} class="h-14 w-14 shrink-0" />
					<div class="min-w-0 flex-1">
						<p class="font-medium">{species.name}</p>
						<p class="text-xs text-slate-400">
							HP {species.maxHitPoints} · ATK {species.attack} · DEF {species.defense} · SPD
							{species.speed}
						</p>
					</div>
					<button
						class="rounded border border-amber-400 px-3 py-1 text-sm text-amber-300 disabled:opacity-40"
						type="button"
						disabled={full}
						onclick={() => team.push(species.id)}
					>
						Add<span class="sr-only"> {species.name}</span>
					</button>
				</li>
			{/each}
		</ul>
	</section>

	<section class="flex flex-col gap-3">
		{#if player.loaded && !player.nickname}
			<div class="rounded-lg border border-slate-700 bg-slate-900 p-4">
				<h2 class="mb-3 text-lg font-semibold">Choose a nickname to play</h2>
				<NicknameForm />
			</div>
		{/if}

		<button
			class="rounded-lg bg-amber-500 px-4 py-3 text-lg font-semibold text-slate-950 disabled:opacity-40"
			type="button"
			disabled={!player.nickname || team.length === 0 || starting}
			onclick={start}
		>
			{starting ? 'Starting…' : 'Fight the bot'}
		</button>

		{#if failure}
			<p class="text-sm text-red-400" role="alert">{failure}</p>
		{/if}
	</section>
</div>
