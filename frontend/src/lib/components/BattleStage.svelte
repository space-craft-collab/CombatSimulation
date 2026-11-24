<script lang="ts">
	// The battle as a stage: the monsters stand in the arena's
	// picture, the opponent's at the back, the viewer's in front.
	// It only draws; the page owns the battle and every pick.
	import type { Snippet } from 'svelte';
	import ArenaBackdrop from './ArenaBackdrop.svelte';
	import StageMonster from './StageMonster.svelte';
	import type { StageSide } from './stage.ts';

	interface Props {
		arenaId: string;
		away: StageSide;
		home: StageSide;
		/** What the back row says while nobody has taken the seat. */
		emptySeat: string;
		/** The bar along the top: the arena's name and the round. */
		title: Snippet;
	}

	let { arenaId, away, home, emptySeat, title }: Props = $props();

	let stage = $state<HTMLElement>();

	// Where a monster stands across the stage, for one that lunges at it.
	function centerOf(slot: number): number | undefined {
		const rect = stage?.querySelector(`[data-slot="${slot}"]`)?.getBoundingClientRect();

		return rect && rect.left + rect.width / 2;
	}
</script>

<!-- Wider than square: a square stage pushes the abilities and the strike
     button off a phone's screen. The pictures keep their free floor from
     52 % of their height down at any of these ratios, so the rows'
     heights hold for every arena. Without a picture, a plain sky and
     floor meet at that same height. -->
<div
	class="relative isolate aspect-[8/5] overflow-hidden rounded-lg border border-slate-800 bg-[linear-gradient(to_bottom,var(--color-slate-900),var(--color-slate-700)_52%,var(--color-slate-500)_52%,var(--color-slate-600)_53%,var(--color-slate-800))] sm:aspect-video"
	data-testid="stage"
	bind:this={stage}
>
	<ArenaBackdrop
		{arenaId}
		shade="bg-[radial-gradient(ellipse_at_center,transparent_45%,rgb(2_6_23/0.6))]"
	/>

	<div
		class="absolute inset-x-0 top-0 flex flex-wrap items-baseline justify-between gap-x-3 bg-linear-to-b from-slate-950/85 via-slate-950/50 to-transparent px-3 pt-0.5 pb-5 sm:pt-1.5"
	>
		{@render title()}
	</div>

	{@render side(away, 'away-side', 'back')}
	{@render side(home, 'home-side', 'front')}
</div>

{#snippet side(side: StageSide, testId: string, row: 'back' | 'front')}
	<!-- A layer over the whole stage, so its label can sit in a corner;
	     only the monsters in it take taps. -->
	<section
		class="pointer-events-none absolute inset-0"
		aria-label={side.label}
		data-testid={testId}
	>
		<h2
			class={[
				'absolute left-2 max-w-[20%] truncate rounded bg-slate-950/60 px-1.5 text-[10px] leading-3.5 font-semibold text-slate-300 sm:max-w-[30%] sm:text-xs sm:leading-4',
				row === 'back' ? 'top-7 sm:top-11' : 'bottom-1.5'
			]}
		>
			{side.label}
		</h2>
		<ul
			class={[
				'absolute flex items-end justify-center',
				row === 'back' ? 'inset-x-[6%] bottom-[45%]' : 'inset-x-0 bottom-[4%]'
			]}
		>
			{#each side.monsters as monster (monster.combatant.slot)}
				<li class={['pointer-events-auto w-1/3', row === 'back' ? 'sm:w-40' : 'sm:w-48']}>
					<StageMonster {monster} {row} {centerOf} />
				</li>
			{:else}
				<!-- The seat of an open battle, while nobody has taken it. -->
				<li
					class="pointer-events-auto rounded-lg border border-dashed border-slate-400/60 bg-slate-950/75 px-3 py-2 text-center text-sm text-slate-200"
				>
					{emptySeat}
				</li>
			{/each}
		</ul>
	</section>
{/snippet}
