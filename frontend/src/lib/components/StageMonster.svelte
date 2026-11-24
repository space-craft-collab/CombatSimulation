<script lang="ts">
	import { effectLabel } from '#lib/arena/abilities.ts';
	import { isStanding } from '#lib/arena/battle.ts';
	import { ELEMENT_STYLES } from './element-styles.ts';
	import HealthBar from './HealthBar.svelte';
	import MonsterSprite from './MonsterSprite.svelte';
	import type { StageMonster } from './stage.ts';

	interface Props {
		monster: StageMonster;
		/**
		 * Where it stands. The back row is the opponent's, smaller for
		 * the distance, and a pick there is the target; the front row is
		 * the viewer's, and a pick there is the monster that acts.
		 */
		row: 'back' | 'front';
	}

	let { monster, row }: Props = $props();

	const combatant = $derived(monster.combatant);
	const standing = $derived(isStanding(combatant));
	const actor = $derived(monster.selected && row === 'front');
	const target = $derived(monster.selected && row === 'back');
</script>

<button
	type="button"
	class={[
		'group relative flex w-full flex-col items-center gap-0.5 rounded-lg px-0.5 pt-1 focus-visible:outline-2 focus-visible:outline-amber-300',
		monster.selectable && 'cursor-pointer'
	]}
	disabled={!monster.selectable}
	aria-pressed={monster.selected}
	onclick={monster.onselect}
	data-testid="combatant"
	data-slot={combatant.slot}
>
	{#if target}
		<!-- The target's marker, pointing down at its plate. -->
		<span
			class="target-bounce absolute -top-2 left-1/2 h-2.5 w-3.5 -translate-x-1/2 bg-rose-400 drop-shadow-[0_0_4px_rgb(251_113_133)] [clip-path:polygon(0_0,100%_0,50%_100%)]"
			aria-hidden="true"
		></span>
	{/if}

	<!-- The name plate: dark enough to read on every arena's picture. -->
	<span
		class={[
			'flex w-24 max-w-full flex-col gap-0.5 rounded-md bg-slate-950/80 px-1.5 pt-0.5 pb-1 text-left shadow-md ring-1 sm:w-28',
			actor
				? 'ring-amber-300'
				: target
					? 'ring-rose-400'
					: 'ring-white/15 group-enabled:group-hover:ring-white/50',
			!standing && 'opacity-75'
		]}
	>
		<span class="flex min-w-0 items-center gap-1">
			<span
				class={['size-2 shrink-0 rounded-full', ELEMENT_STYLES[monster.element].dot]}
				title={monster.element}
				aria-hidden="true"
			></span>
			<span class="truncate text-xs leading-3.5 font-semibold sm:text-sm sm:leading-4"
				>{monster.name}</span
			>
		</span>
		<span class="sr-only">{monster.owner}</span>
		{#if standing}
			<span class="flex items-center gap-1">
				<HealthBar hitPoints={combatant.hitPoints} maxHitPoints={combatant.maxHitPoints} />
				<span class="text-[10px] leading-none text-slate-200 tabular-nums" aria-hidden="true">
					{combatant.hitPoints}
				</span>
				<span class="sr-only">HP {combatant.hitPoints} / {combatant.maxHitPoints}</span>
			</span>
		{:else}
			<span class="text-[10px] leading-none text-slate-400">Defeated</span>
		{/if}
		<span class="sr-only">{monster.element}</span>
	</span>

	<span class={['relative', row === 'front' ? 'size-14 sm:size-24' : 'size-11 sm:size-19']}>
		<!-- The ground under its feet: a shadow, and the acting monster's ring. -->
		<span
			class={[
				'absolute bottom-0 left-1/2 h-[22%] w-[85%] -translate-x-1/2 translate-y-1/4 rounded-[50%] bg-black/55 blur-[1px] transition-opacity duration-1000',
				!standing && 'opacity-40'
			]}
		></span>
		{#if actor}
			<span
				class="absolute bottom-0 left-1/2 h-[30%] w-[120%] -translate-x-1/2 translate-y-1/3 rounded-[50%] border-2 border-amber-300 bg-amber-300/20 shadow-[0_0_12px_rgb(252_211_77)]"
			></span>
		{/if}

		<span
			class={['absolute inset-0', standing && 'idle-bob']}
			style:animation-delay="{combatant.slot * -0.7}s"
		>
			{#key monster.lastChange?.round}
				<span class={['block size-full', monster.lastChange?.damage && 'hit-shake']}>
					<MonsterSprite
						speciesId={combatant.speciesId}
						defeated={!standing}
						class="size-full transition-[opacity,filter] duration-1000"
					/>
				</span>
			{/key}
		</span>

		{#if standing && combatant.effects.length > 0}
			<span class="absolute top-0 right-full -mr-2 flex flex-col items-end gap-0.5">
				{#each combatant.effects as effect (effect.kind)}
					{@const label = effectLabel(effect)}
					<span
						class="rounded bg-slate-950/85 px-1 text-[9px] leading-3.5 font-semibold whitespace-nowrap text-slate-100 ring-1 ring-white/20"
						title={label.long}
						data-testid="effect"
					>
						<span aria-hidden="true">{label.short}</span>
						<span class="sr-only">{label.long}</span>
					</span>
				{/each}
			</span>
		{/if}

		{#if monster.lastChange}
			{#key monster.lastChange.round}
				{#if monster.lastChange.damage > 0}
					<span
						class="hit-flash absolute -top-1 left-full -ml-3 text-lg font-extrabold text-red-400 text-shadow-black/80 text-shadow-md"
					>
						−{monster.lastChange.damage}
					</span>
				{/if}
				{#if monster.lastChange.healing > 0}
					<span
						class="hit-flash absolute -top-1 right-full -mr-3 text-lg font-extrabold text-emerald-400 text-shadow-black/80 text-shadow-md"
					>
						+{monster.lastChange.healing}
					</span>
				{/if}
			{/key}
		{/if}
	</span>
</button>
