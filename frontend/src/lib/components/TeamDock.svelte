<script lang="ts">
	import type { Difficulty } from '#lib/arena/battle.ts';
	import type { Species } from '#lib/arena/catalog.ts';
	import { MAX_TEAM_SIZE } from '#lib/arena/team.ts';
	import DifficultyChoice from './DifficultyChoice.svelte';
	import { ELEMENT_STYLES } from './element-styles.ts';
	import MonsterSprite from './MonsterSprite.svelte';

	interface Props {
		/** The picked monsters in slot order. */
		team: Species[];
		/** Whether the battle can be started right now. */
		ready: boolean;
		/**
		 * What is on its way, if anything: a battle against the bot,
		 * one that waits for a human, or joining an open one.
		 */
		pending: 'bot' | 'human' | 'join' | null;
		/** Why the last start failed, if it did. */
		failure: string | null;
		/** How hard the bot is; bound, so the picker sets it. */
		difficulty: Difficulty;
		onremove: (slot: number) => void;
		/** Starts a battle against the bot. */
		onstart: () => void;
		/** Opens a battle that waits for a human opponent. */
		onwait: () => void;
	}

	let {
		team,
		ready,
		pending,
		failure,
		difficulty = $bindable(),
		onremove,
		onstart,
		onwait
	}: Props = $props();

	const slots = $derived(Array.from({ length: MAX_TEAM_SIZE }, (_, slot) => team[slot]));
</script>

<!-- Stays in reach while the list of monsters scrolls. -->
<section
	class="sticky bottom-0 -mx-4 flex flex-col gap-2 border-t border-slate-800 bg-slate-950 px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]"
	aria-labelledby="team-heading"
>
	<h2 id="team-heading" class="sr-only">Your team, {team.length} of {MAX_TEAM_SIZE}</h2>

	{#if team.length === 0}
		<p class="text-sm text-slate-400">Tap a monster to add it to your team.</p>
	{:else if team.length === MAX_TEAM_SIZE}
		<p class="text-sm text-slate-400">Your team is full. Tap a slot to take a monster out.</p>
	{/if}

	<DifficultyChoice bind:value={difficulty} disabled={pending !== null} />

	<div class="flex items-center gap-3">
		<ol class="flex gap-2">
			{#each slots as species, slot (slot)}
				<li>
					{#if species}
						<button
							class={[
								'relative grid h-12 w-12 cursor-pointer place-items-center rounded-lg border hover:border-red-400',
								ELEMENT_STYLES[species.element].tile
							]}
							type="button"
							title="Remove {species.name}"
							onclick={() => onremove(slot)}
							data-testid="team-member"
						>
							<MonsterSprite speciesId={species.id} class="h-10 w-10" />
							<span
								class="absolute -top-1.5 -right-1.5 grid h-4 w-4 place-items-center rounded-full bg-slate-600 text-[10px] leading-none text-slate-100"
								aria-hidden="true">✕</span
							>
							<span class="sr-only">Remove {species.name} from the team</span>
						</button>
					{:else}
						<span
							class="grid h-12 w-12 place-items-center rounded-lg border border-dashed border-slate-700 text-slate-600"
						>
							<span aria-hidden="true">+</span>
							<span class="sr-only">Empty slot</span>
						</span>
					{/if}
				</li>
			{/each}
		</ol>

		<button
			class="min-w-0 flex-1 rounded-lg bg-amber-500 px-4 py-3 text-lg font-semibold text-slate-950 enabled:cursor-pointer disabled:opacity-40"
			type="button"
			disabled={!ready || pending !== null}
			onclick={onstart}
		>
			{pending === 'bot' ? 'Starting…' : 'Fight the bot'}
		</button>
	</div>

	<!-- The second mode, quieter than the first (ADR-0007). -->
	<div class="flex items-center justify-between gap-3">
		<p class="text-xs text-slate-400">Or let another player join you.</p>
		<button
			class="min-h-10 shrink-0 rounded-lg border border-slate-600 px-3 text-sm font-medium text-slate-200 enabled:cursor-pointer enabled:hover:border-amber-300 disabled:opacity-40"
			type="button"
			disabled={!ready || pending !== null}
			onclick={onwait}
		>
			{pending === 'human' ? 'Opening…' : 'Wait for a human'}
		</button>
	</div>

	{#if failure}
		<p class="text-sm text-red-400" role="alert">{failure}</p>
	{/if}
</section>
