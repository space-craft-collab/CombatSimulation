<script lang="ts">
	import { isReady, readinessText, summaryOf } from '#lib/arena/abilities.ts';
	import type { AbilityCooldown } from '#lib/arena/battle.ts';
	import type { Ability } from '#lib/arena/catalog.ts';

	interface Props {
		/** The acting monster's abilities in their order, with their cooldowns. */
		abilities: { ability: Ability; cooldown: AbilityCooldown }[];
		/** The ability the move uses. */
		selected: string | undefined;
		onselect: (abilityId: string) => void;
	}

	let { abilities, selected, onselect }: Props = $props();
</script>

<!-- One row each: on a phone three side by side leave no room for the summary. -->
<ul class="grid gap-1.5 sm:grid-cols-3" aria-label="Abilities">
	{#each abilities as { ability, cooldown } (ability.id)}
		{@const ready = isReady(cooldown)}
		<li class="min-w-0">
			<button
				type="button"
				class={[
					'flex h-full w-full items-center justify-between gap-2 rounded-lg border px-3 py-1.5 text-left',
					selected === ability.id
						? 'border-amber-400 bg-slate-800'
						: 'border-slate-700 bg-slate-900',
					ready ? 'cursor-pointer hover:border-amber-300' : 'opacity-50'
				]}
				disabled={!ready}
				aria-pressed={selected === ability.id}
				onclick={() => onselect(ability.id)}
				data-testid="ability-{ability.id}"
			>
				<span class="min-w-0">
					<span class="block truncate text-sm font-medium">{ability.name}</span>
					<span class="block text-xs text-slate-400">{summaryOf(ability)}</span>
				</span>
				<span
					class={['shrink-0 text-xs', ready ? 'text-slate-500' : 'font-semibold text-amber-300']}
				>
					{readinessText(cooldown)}
				</span>
			</button>
		</li>
	{/each}
</ul>
