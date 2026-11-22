<script lang="ts">
	import { STRIKE, cooldownText, summaryOf } from '#lib/arena/abilities.ts';
	import type { Species } from '#lib/arena/catalog.ts';
	import { STATS, type StatKey } from '#lib/arena/team.ts';
	import ElementBadge from './ElementBadge.svelte';
	import { ELEMENT_STYLES } from './element-styles.ts';
	import MonsterSprite from './MonsterSprite.svelte';

	interface Props {
		species: Species;
		/** The highest value of each stat on offer; the bars are measured against it. */
		ceilings: Record<StatKey, number>;
		/** How often the species is in the team already. */
		picked: number;
		/** False once the team is full. */
		addable: boolean;
		onadd: () => void;
	}

	let { species, ceilings, picked, addable, onadd }: Props = $props();

	// Every species knows the basic attack; the page says so once.
	const special = $derived(species.abilities.filter((ability) => ability.id !== STRIKE));
</script>

<!-- The card itself is the control: an unseen button lies over all of it, named "Add …". -->
<li
	class={[
		'relative flex h-full flex-col gap-3 rounded-lg border bg-slate-900 p-3',
		picked > 0 ? 'border-amber-400/70' : 'border-slate-700',
		addable ? 'hover:border-amber-300' : 'opacity-50'
	]}
	data-testid="species-{species.id}"
>
	<div class="flex items-center gap-3">
		<div class={['relative shrink-0 rounded-lg border p-1', ELEMENT_STYLES[species.element].tile]}>
			<MonsterSprite speciesId={species.id} class="h-12 w-12" />
			{#if picked > 0}
				<span
					class="absolute -top-2 -right-2 grid h-5 min-w-5 place-items-center rounded-full bg-amber-400 px-1 text-xs font-bold text-slate-950"
					data-testid="picked-count"
				>
					<span aria-hidden="true">×{picked}</span>
					<span class="sr-only">{picked} in your team</span>
				</span>
			{/if}
		</div>
		<h3 class="flex min-w-0 flex-1 flex-wrap items-center gap-x-2 gap-y-0.5 font-semibold">
			{species.name}
			<ElementBadge element={species.element} />
		</h3>
		<button
			class="absolute inset-0 rounded-lg focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-300 enabled:cursor-pointer"
			type="button"
			disabled={!addable}
			onclick={onadd}
		>
			<span class="sr-only">Add {species.name}</span>
		</button>
	</div>

	<dl class="grid grid-cols-4 gap-x-3">
		{#each STATS as stat (stat.key)}
			{@const value = species[stat.key]}
			<div>
				<dt class="sr-only">{stat.name}</dt>
				<dd>
					<span class="flex items-baseline justify-between text-xs" aria-hidden="true">
						<span class="text-slate-400">{stat.label}</span>
						<span class="font-semibold text-slate-200 tabular-nums">{value}</span>
					</span>
					<span class="sr-only">{value}</span>
					<span class="mt-1 block h-1.5 overflow-hidden rounded-full bg-slate-700">
						<span
							class="block h-full rounded-full bg-slate-300"
							style:width="{(value / ceilings[stat.key]) * 100}%"
						></span>
					</span>
				</dd>
			</div>
		{/each}
	</dl>

	<ul class="flex flex-col gap-0.5 text-xs" aria-label="Abilities of {species.name}">
		{#each special as ability (ability.id)}
			{@const cooldown = cooldownText(ability)}
			<li>
				<span class="font-medium text-slate-200">{ability.name}</span>
				<span class="text-slate-400">
					{summaryOf(ability)}{cooldown ? ` · ${cooldown}` : ''}
				</span>
			</li>
		{/each}
	</ul>
</li>
