<script lang="ts">
	import { BEATS } from '#lib/arena/abilities.ts';
	import type { Element } from '#lib/arena/catalog.ts';
	import ElementBadge from './ElementBadge.svelte';

	// Which element beats which, once around: what a team is picked by.
	const CYCLE: Element[] = ['Fire', 'Nature', 'Frost'];
</script>

<div class="text-sm text-slate-400" data-testid="element-cycle">
	<ul class="flex flex-wrap gap-x-4 gap-y-1">
		{#each CYCLE as element (element)}
			{@const beaten = BEATS[element]}
			{#if beaten}
				<li class="flex items-center gap-1.5">
					<ElementBadge {element} />
					beats
					<ElementBadge element={beaten} />
				</li>
			{/if}
		{/each}
	</ul>
	<p class="mt-1">A strong hit deals half again as much damage, a weak one a quarter less.</p>
</div>
