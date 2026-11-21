<script lang="ts">
	import './layout.css';
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import favicon from '#lib/assets/favicon.svg';
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
			<a class="text-lg font-bold" href={resolve('/')}>Orleans Monster Arena</a>
			{#if player.nickname}
				<span class="truncate text-sm text-slate-400" data-testid="playing-as">
					Playing as <span class="font-semibold text-slate-200">{player.nickname}</span>
				</span>
			{/if}
		</div>
	</header>

	<main class="mx-auto max-w-3xl px-4 py-6">
		{@render children()}
	</main>
</div>
