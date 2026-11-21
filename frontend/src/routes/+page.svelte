<script lang="ts">
	import { runConnectionCheck } from '#lib/arena/connection-check.ts';
	import type { PageProps } from './$types';

	// Walking skeleton of Phase 4: the Catalog is rendered on the
	// server, the check below runs in the browser. The real screens
	// replace this page.
	let { data }: PageProps = $props();

	let steps = $state<string[]>([]);
	let failure = $state<string | null>(null);
	let running = $state(false);

	async function check() {
		steps = [];
		failure = null;
		running = true;

		try {
			await runConnectionCheck(data.arenas[0].id, data.species[0].id, (step) => steps.push(step));
		} catch (error) {
			failure = error instanceof Error ? error.message : String(error);
		} finally {
			running = false;
		}
	}
</script>

<svelte:head>
	<title>Orleans Monster Arena</title>
</svelte:head>

<main class="mx-auto flex max-w-2xl flex-col gap-8 p-6">
	<h1 class="text-3xl font-bold">Orleans Monster Arena</h1>

	<section>
		<h2 class="mb-2 text-xl font-semibold">Arenas</h2>
		<ul class="flex flex-wrap gap-2">
			{#each data.arenas as arena (arena.id)}
				<li class="rounded border border-slate-300 px-3 py-1" data-testid="arena">{arena.name}</li>
			{/each}
		</ul>
	</section>

	<section>
		<h2 class="mb-2 text-xl font-semibold">Species</h2>
		<ul class="grid gap-2 sm:grid-cols-2">
			{#each data.species as species (species.id)}
				<li class="rounded border border-slate-300 p-3" data-testid="species">
					<p class="font-medium">{species.name}</p>
					<p class="text-sm text-slate-600">
						HP {species.maxHitPoints} · ATK {species.attack} · DEF {species.defense} · SPD {species.speed}
					</p>
				</li>
			{/each}
		</ul>
	</section>

	<section>
		<h2 class="mb-2 text-xl font-semibold">Connection check</h2>
		<p class="mb-3 text-sm text-slate-600">
			Starts a battle against the bot, follows it on the SignalR hub and plays one round.
		</p>
		<button
			class="rounded bg-slate-800 px-4 py-2 text-white disabled:opacity-50"
			disabled={running || data.arenas.length === 0 || data.species.length === 0}
			onclick={check}
		>
			{running ? 'Checking…' : 'Run check'}
		</button>

		<ol class="mt-3 list-decimal pl-5 text-sm" data-testid="check-steps">
			{#each steps as step (step)}
				<li>{step}</li>
			{/each}
		</ol>

		{#if failure}
			<p class="mt-3 text-sm text-red-700" data-testid="check-failure">Failed: {failure}</p>
		{/if}
	</section>
</main>
