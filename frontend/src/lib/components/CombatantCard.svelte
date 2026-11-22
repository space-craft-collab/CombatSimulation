<script lang="ts">
	import { effectLabel } from '#lib/arena/abilities.ts';
	import { isStanding, type Combatant } from '#lib/arena/battle.ts';
	import type { Element } from '#lib/arena/catalog.ts';
	import ElementBadge from './ElementBadge.svelte';
	import HealthBar from './HealthBar.svelte';
	import MonsterSprite from './MonsterSprite.svelte';

	interface Props {
		combatant: Combatant;
		/** The species' display name. */
		name: string;
		/** The species' element. */
		element: Element;
		/** Whose monster it is, in the viewer's words. */
		owner: string;
		/** Whether the viewer can pick this monster right now. */
		selectable?: boolean;
		selected?: boolean;
		/** What the round just played did to it, for a short flash. */
		lastChange?: { round: number; damage: number; healing: number };
		onselect?: () => void;
	}

	let {
		combatant,
		name,
		element,
		owner,
		selectable = false,
		selected = false,
		lastChange,
		onselect
	}: Props = $props();

	const standing = $derived(isStanding(combatant));
</script>

<button
	type="button"
	class={[
		'relative flex w-full flex-col items-center gap-1 rounded-lg border p-2 text-center',
		selected ? 'border-amber-400 bg-slate-800' : 'border-slate-700 bg-slate-900',
		selectable && 'cursor-pointer hover:border-amber-300'
	]}
	disabled={!selectable}
	aria-pressed={selected}
	onclick={onselect}
	data-testid="combatant"
	data-slot={combatant.slot}
>
	<MonsterSprite speciesId={combatant.speciesId} defeated={!standing} class="h-14 w-14" />

	<span class="w-full truncate text-sm font-medium">{name}</span>
	<span class="sr-only">{owner}</span>
	<HealthBar hitPoints={combatant.hitPoints} maxHitPoints={combatant.maxHitPoints} />
	<span class="text-xs text-slate-300">
		{standing ? `HP ${combatant.hitPoints} / ${combatant.maxHitPoints}` : 'Defeated'}
	</span>

	<!-- The element and the effects share one line, wrapping on a narrow card. -->
	<span class="flex flex-wrap justify-center gap-1">
		<ElementBadge {element} />
		{#if standing}
			{#each combatant.effects as effect (effect.kind)}
				{@const label = effectLabel(effect)}
				<span
					class="rounded bg-slate-700 px-1 text-[10px] leading-4 font-medium text-slate-100"
					title={label.long}
					data-testid="effect"
				>
					<span aria-hidden="true">{label.short}</span>
					<span class="sr-only">{label.long}</span>
				</span>
			{/each}
		{/if}
	</span>

	{#if lastChange}
		{#key lastChange.round}
			{#if lastChange.damage > 0}
				<span class="hit-flash absolute top-1 right-2 text-lg font-bold text-red-400">
					−{lastChange.damage}
				</span>
			{/if}
			{#if lastChange.healing > 0}
				<span class="hit-flash absolute top-1 left-2 text-lg font-bold text-emerald-400">
					+{lastChange.healing}
				</span>
			{/if}
		{/key}
	{/if}
</button>
