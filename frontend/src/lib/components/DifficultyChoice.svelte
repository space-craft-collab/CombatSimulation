<script lang="ts">
	import { DIFFICULTIES, type Difficulty } from '#lib/arena/battle.ts';

	interface Props {
		/** How hard the bot is; bound, so the choice sets it. */
		value: Difficulty;
		disabled?: boolean;
	}

	let { value = $bindable(), disabled = false }: Props = $props();
</script>

<!-- Two options, tapped like buttons; radios underneath, so the
     arrow keys and screen readers treat them as one choice. -->
<fieldset class="flex items-center gap-3">
	<legend class="sr-only">Difficulty</legend>
	<div class="flex shrink-0 rounded-lg border border-slate-700 p-0.5">
		{#each DIFFICULTIES as option (option)}
			<label
				class="grid min-h-10 min-w-16 cursor-pointer place-items-center rounded-md px-3 text-sm font-medium text-slate-400 has-checked:bg-slate-700 has-checked:text-slate-100 has-focus-visible:outline-2 has-focus-visible:outline-amber-400"
			>
				<input
					class="sr-only"
					type="radio"
					name="difficulty"
					value={option}
					bind:group={value}
					{disabled}
				/>
				{option}
			</label>
		{/each}
	</div>
	<p class="text-xs text-slate-400">Easy weakens the bot's monsters.</p>
</fieldset>
