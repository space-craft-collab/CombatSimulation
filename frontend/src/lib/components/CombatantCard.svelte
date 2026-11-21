<script lang="ts">
	import { isStanding, type Combatant } from '#lib/arena/battle.ts';
	import HealthBar from './HealthBar.svelte';
	import MonsterSprite from './MonsterSprite.svelte';

	interface Props {
		combatant: Combatant;
		/** The species' display name. */
		name: string;
		/** Whose monster it is, in the viewer's words. */
		owner: string;
		/** Whether the viewer can pick this monster right now. */
		selectable?: boolean;
		selected?: boolean;
		/** The damage it took in the round just played, for a short flash. */
		lastHit?: { round: number; damage: number };
		onselect?: () => void;
	}

	let {
		combatant,
		name,
		owner,
		selectable = false,
		selected = false,
		lastHit,
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

	{#if lastHit}
		{#key lastHit.round}
			<span class="hit-flash absolute top-1 right-2 text-lg font-bold text-red-400">
				−{lastHit.damage}
			</span>
		{/key}
	{/if}
</button>
