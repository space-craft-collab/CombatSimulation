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
	{:else if speciesId === 'glacier-yak'}
		<rect x="14" y="46" width="8" height="12" rx="2" fill="#475569" />
		<rect x="42" y="46" width="8" height="12" rx="2" fill="#475569" />
		<ellipse cx="32" cy="38" rx="25" ry="15" fill="#94a3b8" />
		<path
			d="M7 38h50l-3 10l-4-4l-4 6l-4-5l-4 6l-4-5l-4 6l-4-5l-4 6l-4-5l-4 5l-4-4z"
			fill="#cbd5e1"
		/>
		<path d="M47 29l3-7l3 7zM52 33l3-6l3 6z" fill="#7dd3fc" />
		<path d="M20 18q-14 0-14-12q6 7 15 6zM44 18q14 0 14-12q-6 7-15 6z" fill="#f1f5f9" />
		<rect x="19" y="10" width="26" height="26" rx="10" fill="#cbd5e1" />
		<path d="M19 18q13-12 26 0v3q-13-7-26 0z" fill="#e0f2fe" />
		<circle cx="26" cy="24" r="2.5" fill="#0f172a" />
		<circle cx="38" cy="24" r="2.5" fill="#0f172a" />
		<ellipse cx="32" cy="31" rx="8" ry="4.5" fill="#94a3b8" />
		<circle cx="29" cy="31" r="1.2" fill="#0f172a" />
		<circle cx="35" cy="31" r="1.2" fill="#0f172a" />
	{:else if speciesId === 'thorn-lynx'}
		<path d="M48 46q14-4 10-20q6 12 0 24q-4 4-10 2z" fill="#4d7c0f" />
		<path d="M15 26l2-19l11 12zM49 26l-2-19l-11 12z" fill="#65a30d" />
		<path d="M17 7l-2-5l4 3zM47 7l2-5l-4 3z" fill="#365314" />
		<path d="M14 38l-7 5l8 1zM50 38l7 5l-8 1z" fill="#84cc16" />
		<ellipse cx="32" cy="34" rx="19" ry="17" fill="#84cc16" />
		<path d="M24 18l2-6l2 6zM31 17l1-6l1 6zM36 18l2-6l2 6z" fill="#365314" />
		<path d="M23 40q9 11 18 0q-9 5-18 0z" fill="#ecfccb" />
		<ellipse cx="25" cy="31" rx="3.5" ry="2.5" fill="#fde047" />
		<ellipse cx="39" cy="31" rx="3.5" ry="2.5" fill="#fde047" />
		<ellipse cx="25" cy="31" rx="1" ry="2.3" fill="#1c1917" />
		<ellipse cx="39" cy="31" rx="1" ry="2.3" fill="#1c1917" />
		<path d="M30 38h4l-2 2.5z" fill="#1c1917" />
	{:else}
		<path d="M12 40q0-28 20-28t20 28q0 14-20 14t-20-14z" fill="#64748b" />
		<circle cx="25" cy="32" r="3" fill="#0f172a" />
		<circle cx="39" cy="32" r="3" fill="#0f172a" />
	{/if}
</svg>
