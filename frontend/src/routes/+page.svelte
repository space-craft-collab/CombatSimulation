<script lang="ts">
	import { resolve } from '$app/paths';
	import { player } from '#lib/arena/player.svelte.ts';
	import NicknameForm from '#lib/components/NicknameForm.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let renaming = $state(false);
</script>

<svelte:head>
	<title>Orleans Monster Arena</title>
</svelte:head>

<div class="flex flex-col gap-8">
	<section>
		<h1 class="text-2xl font-bold">Pick a team, fight the bot</h1>
		<p class="mt-2 text-slate-300">
			A turn-based monster battle. Every battle runs as an Orleans grain on the server; each round
			reaches this page live.
		</p>
	</section>

	{#if player.loaded && (!player.nickname || renaming)}
		<section class="rounded-lg border border-slate-700 bg-slate-900 p-4" data-testid="nickname">
			<h2 class="mb-3 text-lg font-semibold">
				{player.nickname ? 'Change your nickname' : 'First, choose a nickname'}
			</h2>
			<NicknameForm onsaved={() => (renaming = false)} />
		</section>
	{:else if player.nickname}
		<p class="text-sm text-slate-400">
			Playing as <span class="font-semibold text-slate-200">{player.nickname}</span>.
			<button class="underline" type="button" onclick={() => (renaming = true)}>Change</button>
		</p>
	{/if}

	<section>
		<h2 class="mb-3 text-lg font-semibold">Choose an arena</h2>
		<ul class="grid gap-3 sm:grid-cols-2">
			{#each data.arenas as arena (arena.id)}
				<li>
					<a
						class="block rounded-lg border border-slate-700 bg-slate-900 p-4 font-medium hover:border-amber-300"
						href={resolve('/arenas/[arenaId]', { arenaId: arena.id })}
						data-testid="arena"
					>
						{arena.name}
					</a>
				</li>
			{:else}
				<li class="text-slate-400">No arena is open right now.</li>
			{/each}
		</ul>
	</section>
</div>
