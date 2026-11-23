<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import type { Ending } from '#lib/arena/battle.ts';
	import { HISTORY_LIMIT, lineOf, summaryOf, whenOf } from '#lib/arena/dashboard.ts';
	import { player } from '#lib/arena/player.svelte.ts';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// The visitor's own dashboard once the browser has read their
	// nickname; the server renders every one as someone else's.
	const own = $derived(player.nickname === data.playerId);
	const summary = $derived(summaryOf(data.record));
	const lines = $derived(data.history.map((entry) => lineOf(entry, data.playerId, data.arenas)));
	const empty = $derived(summary.decided === 0 && lines.length === 0);

	// Times show in UTC until the page runs in the browser, then in
	// its own time zone, so hydration finds what the server rendered.
	let hydrated = $state(false);

	onMount(() => (hydrated = true));

	const BADGES: Record<Ending, string> = {
		won: 'border-emerald-500/60 text-emerald-300',
		lost: 'border-red-500/60 text-red-300',
		abandoned: 'border-slate-600 text-slate-300',
		unjoined: 'border-slate-600 text-slate-300'
	};
</script>

<svelte:head>
	<title>{data.playerId} · Orleans Monster Arena</title>
</svelte:head>

{#snippet stat(label: string, value: string | number, testId: string)}
	<div class="rounded-lg border border-slate-800 bg-slate-900 p-3">
		<dt class="text-sm text-slate-400">{label}</dt>
		<dd class="text-2xl font-bold tabular-nums" data-testid={testId}>{value}</dd>
	</div>
{/snippet}

<div class="flex flex-col gap-6">
	<section>
		<h1 class="text-2xl font-bold break-words" data-testid="player-name">{data.playerId}</h1>
		<p class="mt-1 text-slate-400">
			{own ? 'Your record and your last battles.' : 'Record and last battles.'}
		</p>
	</section>

	{#if !data.settled}
		<p
			class="rounded border border-amber-500 p-3 text-sm text-amber-300"
			role="status"
			data-testid="not-settled"
		>
			The battle just over is not counted yet. Reload in a moment to see it here.
		</p>
	{/if}

	{#if empty}
		<section
			class="rounded-lg border border-slate-700 bg-slate-900 p-4 text-center"
			data-testid="empty-dashboard"
		>
			<h2 class="text-xl font-bold">No battles yet</h2>
			<p class="mt-1 text-sm text-slate-400">
				{own
					? 'Fight a battle to the end, and it shows up here with your wins and losses.'
					: `${data.playerId} has not finished a battle yet, or nobody plays by this name.`}
			</p>
			<p class="mt-4">
				<a
					class="inline-block rounded bg-amber-500 px-4 py-2 font-semibold text-slate-950"
					href={resolve('/')}
				>
					Choose an arena
				</a>
			</p>
		</section>
	{:else}
		<section aria-labelledby="record-heading">
			<h2 id="record-heading" class="mb-2 text-lg font-semibold">Record</h2>
			{#if summary.decided > 0}
				<dl class="grid grid-cols-3 gap-2 text-center" data-testid="record">
					{@render stat('Wins', data.record.wins, 'wins')}
					{@render stat('Losses', data.record.losses, 'losses')}
					{@render stat('Win rate', `${summary.winRate}%`, 'win-rate')}
				</dl>
			{:else}
				<p
					class="rounded-lg border border-slate-800 p-3 text-sm text-slate-400"
					data-testid="no-record"
				>
					No battle decided yet. Abandoned battles and battles nobody joined count for nobody.
				</p>
			{/if}
		</section>

		<section aria-labelledby="history-heading">
			<h2 id="history-heading" class="mb-2 text-lg font-semibold">
				{lines.length >= HISTORY_LIMIT ? `Last ${HISTORY_LIMIT} battles` : 'Last battles'}
			</h2>
			<ol class="flex flex-col gap-2" data-testid="history">
				{#each lines as line (line.battleId)}
					<li>
						<!-- A finished battle's page loads it from the cold store. -->
						<a
							class="flex flex-col gap-1 rounded-lg border border-slate-800 bg-slate-900 p-3 hover:border-amber-300"
							href={resolve('/battles/[battleId]', { battleId: line.battleId })}
							data-testid="history-entry"
						>
							<span class="flex min-w-0 items-baseline gap-2">
								<span
									class={[
										'shrink-0 rounded-full border px-2 py-0.5 text-xs font-semibold',
										BADGES[line.ending]
									]}
									data-testid="history-result">{line.result}</span
								>
								{#if line.opponent}
									<span class="truncate font-medium" data-testid="history-opponent"
										>{line.opponent}</span
									>
								{/if}
								<time class="ml-auto shrink-0 text-xs text-slate-400" datetime={line.completedAt}
									>{whenOf(line.completedAt, hydrated ? undefined : 'UTC')}</time
								>
							</span>
							<span class="text-sm text-slate-400">
								<span data-testid="history-arena">{line.arenaName}</span>
								<span aria-hidden="true">·</span>
								<span data-testid="history-detail">{line.detail}</span>
							</span>
						</a>
					</li>
				{:else}
					<li class="text-sm text-slate-400">No finished battle is listed.</li>
				{/each}
			</ol>
		</section>
	{/if}
</div>
