<script lang="ts">
	// An arena's picture behind its parent's content. The parent sets
	// `relative isolate overflow-hidden`; the picture and its shade
	// sit below the content. An arena without a picture leaves the
	// parent as it is.
	import { arenaImageOf } from '#lib/arena/arena-images.ts';

	interface Props {
		arenaId: string;
		/** What lies over the picture, so the text on it stays readable. */
		shade?: string;
		/** Zoom in a little while the parent `group` is hovered. */
		zoom?: boolean;
	}

	let {
		arenaId,
		shade = 'bg-linear-to-t from-slate-950/90 via-slate-950/40 to-slate-950/10',
		zoom = false
	}: Props = $props();

	const image = $derived(arenaImageOf(arenaId));
</script>

{#if image}
	<img
		src={image}
		alt=""
		class={[
			'absolute inset-0 -z-10 h-full w-full object-cover',
			zoom && 'transition-transform duration-300 group-hover:scale-105'
		]}
		data-testid="arena-image"
	/>
	<span class={['absolute inset-0 -z-10', shade]}></span>
{/if}
