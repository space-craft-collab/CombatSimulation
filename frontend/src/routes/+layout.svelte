<script lang="ts">
	import './layout.css';
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import favicon from '#lib/assets/favicon.svg';
	import { playerParam } from '#lib/arena/dashboard.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	// The nickname lives in the browser only; the server renders
	// every page for a visitor it does not know.
	onMount(() => player.load());
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<div class="min-h-screen bg-slate-950 text-slate-100">
	<header class="border-b border-slate-800">
		<div class="mx-auto flex max-w-3xl items-center justify-between gap-4 px-4 py-3">
			<a class="shrink-0 text-base font-bold sm:text-lg" href={resolve('/')}>
				Orleans Monster Arena
			</a>
			{#if player.nickname}
				<!-- On a phone the name alone: next to the title there is no room for more.
				     It leads to the player's dashboard; preloaded on a tap only, not on
				     hover, so a battle that ended since the pointer passed is not missing. -->
				<a
					class="truncate text-sm text-slate-400 hover:text-slate-200"
					href={resolve('/players/[playerId]', { playerId: playerParam(player.nickname) })}
					title="Your record and your last battles"
					data-sveltekit-preload-data="tap"
					data-testid="playing-as"
				>
					<span class="max-sm:sr-only">Playing as</span>
					<span
						class="font-semibold text-slate-200 underline decoration-slate-600 underline-offset-4"
						>{player.nickname}</span
					>
				</a>
			{/if}
		</div>
	</header>

	<main class="mx-auto max-w-3xl px-4 py-6">
		{@render children()}
	</main>
</div>
