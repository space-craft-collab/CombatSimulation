<script lang="ts">
	import type { Element } from '#lib/arena/catalog.ts';

	interface Props {
		element: Element;
	}

	let { element }: Props = $props();

	// The colour only backs up the word; the word carries the meaning.
	const COLOURS: Record<Element, string> = {
		Fire: 'border-orange-500/60 bg-orange-950 text-orange-200',
		Frost: 'border-sky-500/60 bg-sky-950 text-sky-200',
		Nature: 'border-lime-500/60 bg-lime-950 text-lime-200',
		Neutral: 'border-slate-600 bg-slate-800 text-slate-300'
	};
</script>

<span
	class={['rounded border px-1 text-[10px] leading-4 font-semibold uppercase', COLOURS[element]]}
	data-testid="element"
>
	{element}
</span>
