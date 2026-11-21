<script lang="ts">
	// The monsters, drawn as inline SVG: the repository carries no
	// image assets (ADR-0004). A species without a drawing gets a
	// plain blob, so a new Catalog entry still shows up.
	interface Props {
		speciesId: string;
		/** A defeated monster is drawn grey and faded. */
		defeated?: boolean;
		class?: string;
	}

	let { speciesId, defeated = false, class: className = 'h-16 w-16' }: Props = $props();
</script>

<svg
	viewBox="0 0 64 64"
	class={[className, defeated && 'opacity-40 grayscale']}
	aria-hidden="true"
	data-species={speciesId}
>
	{#if speciesId === 'ember-fox'}
		<path d="M50 50q10-6 4-16q8 6 6 18q-4 8-12 4z" fill="#fbbf24" />
		<path d="M14 22l6-16l8 14zM50 22l-6-16l-8 14z" fill="#ea580c" />
		<ellipse cx="32" cy="32" rx="20" ry="17" fill="#f97316" />
		<path d="M20 36q12 18 24 0q-12 6-24 0z" fill="#fed7aa" />
		<circle cx="24" cy="30" r="2.5" fill="#1c1917" />
		<circle cx="40" cy="30" r="2.5" fill="#1c1917" />
		<circle cx="32" cy="42" r="2" fill="#1c1917" />
	{:else if speciesId === 'lava-crab'}
		<g stroke="#7f1d1d" stroke-width="3" stroke-linecap="round">
			<path d="M20 48l-6 8M30 50l-2 8M34 50l2 8M44 48l6 8" />
		</g>
		<g stroke="#b91c1c" stroke-width="4" stroke-linecap="round">
			<path d="M16 34l-6-8M48 34l6-8M26 28l-2-10M38 28l2-10" />
		</g>
		<circle cx="10" cy="24" r="7" fill="#dc2626" />
		<circle cx="54" cy="24" r="7" fill="#dc2626" />
		<ellipse cx="32" cy="38" rx="18" ry="12" fill="#b91c1c" />
		<path d="M20 39l7-5l7 6l9-5" fill="none" stroke="#fb923c" stroke-width="2" />
		<circle cx="24" cy="16" r="3.5" fill="#fff" />
		<circle cx="40" cy="16" r="3.5" fill="#fff" />
		<circle cx="24" cy="16" r="1.5" fill="#1c1917" />
		<circle cx="40" cy="16" r="1.5" fill="#1c1917" />
	{:else if speciesId === 'frost-owl'}
		<path d="M17 20l3-14l7 11zM47 20l-3-14l-7 11z" fill="#38bdf8" />
		<ellipse cx="32" cy="36" rx="17" ry="21" fill="#7dd3fc" />
		<ellipse cx="32" cy="44" rx="10" ry="11" fill="#e0f2fe" />
		<circle cx="25" cy="28" r="6" fill="#fff" />
		<circle cx="39" cy="28" r="6" fill="#fff" />
		<circle cx="25" cy="28" r="2.5" fill="#0c4a6e" />
		<circle cx="39" cy="28" r="2.5" fill="#0c4a6e" />
		<path d="M29 33h6l-3 6z" fill="#fbbf24" />
	{:else if speciesId === 'moss-golem'}
		<rect x="6" y="28" width="8" height="20" rx="3" fill="#57534e" />
		<rect x="50" y="28" width="8" height="20" rx="3" fill="#57534e" />
		<rect x="14" y="24" width="36" height="32" rx="6" fill="#57534e" />
		<rect x="22" y="8" width="20" height="18" rx="4" fill="#78716c" />
		<path d="M20 13q12-12 24 0v4q-12-6-24 0z" fill="#4d7c0f" />
		<circle cx="22" cy="32" r="5" fill="#65a30d" />
		<circle cx="43" cy="47" r="6" fill="#65a30d" />
		<circle cx="40" cy="30" r="3" fill="#65a30d" />
		<rect x="26" y="16" width="3" height="4" fill="#bef264" />
		<rect x="35" y="16" width="3" height="4" fill="#bef264" />
	{:else}
		<path d="M12 40q0-28 20-28t20 28q0 14-20 14t-20-14z" fill="#64748b" />
		<circle cx="25" cy="32" r="3" fill="#0f172a" />
		<circle cx="39" cy="32" r="3" fill="#0f172a" />
	{/if}
</svg>
