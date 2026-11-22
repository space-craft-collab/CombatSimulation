<script lang="ts">
	import type { Element } from '#lib/arena/catalog.ts';
	import { ELEMENT_STYLES } from './element-styles.ts';

	interface Props {
		element: Element;
	}

	let { element }: Props = $props();
</script>

<span
	class={[
		'rounded border px-1 text-[10px] leading-4 font-semibold uppercase',
		ELEMENT_STYLES[element].badge
	]}
	data-testid="element"
>
	{element}
</span>
