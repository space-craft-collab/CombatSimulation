import type { Element } from '#lib/arena/catalog.ts';

/**
 * The colours of an element. They only back up the word next to
 * them; the word carries the meaning. The dot on the battle stage
 * has no room for the word and carries it as a tooltip and as
 * screen reader text instead.
 */
export const ELEMENT_STYLES: Record<Element, { badge: string; tile: string; dot: string }> = {
	Fire: {
		badge: 'border-orange-500/60 bg-orange-950 text-orange-200',
		tile: 'border-orange-500/40 bg-orange-950/60',
		dot: 'bg-orange-500'
	},
	Frost: {
		badge: 'border-sky-500/60 bg-sky-950 text-sky-200',
		tile: 'border-sky-500/40 bg-sky-950/60',
		dot: 'bg-sky-400'
	},
	Nature: {
		badge: 'border-lime-500/60 bg-lime-950 text-lime-200',
		tile: 'border-lime-500/40 bg-lime-950/60',
		dot: 'bg-lime-500'
	},
	Neutral: {
		badge: 'border-slate-600 bg-slate-800 text-slate-300',
		tile: 'border-slate-600 bg-slate-800/60',
		dot: 'bg-slate-400'
	}
};
