<script lang="ts">
	interface Props {
		hitPoints: number;
		maxHitPoints: number;
	}

	let { hitPoints, maxHitPoints }: Props = $props();

	const share = $derived(maxHitPoints > 0 ? hitPoints / maxHitPoints : 0);
</script>

<div
	class="h-2 w-full overflow-hidden rounded-full bg-slate-700"
	role="meter"
	aria-label="Hit points"
	aria-valuemin={0}
	aria-valuemax={maxHitPoints}
	aria-valuenow={hitPoints}
>
	<div
		class={[
			'h-full rounded-full transition-[width] duration-300',
			share > 0.5 ? 'bg-emerald-500' : share > 0.25 ? 'bg-amber-500' : 'bg-red-500'
		]}
		style:width="{share * 100}%"
	></div>
</div>
