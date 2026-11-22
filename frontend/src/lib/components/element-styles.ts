import type { Element } from '#lib/arena/catalog.ts';

/**
 * The colours of an element. They only back up the word next to
 * them; the word carries the meaning.
 */
export const ELEMENT_STYLES: Record<Element, { badge: string; tile: string }> = {
	Fire: {
		badge: 'border-orange-500/60 bg-orange-950 text-orange-200',
		tile: 'border-orange-500/40 bg-orange-950/60'
	},
	Frost: {
		badge: 'border-sky-500/60 bg-sky-950 text-sky-200',
		tile: 'border-sky-500/40 bg-sky-950/60'
	},
	Nature: {
		badge: 'border-lime-500/60 bg-lime-950 text-lime-200',
		tile: 'border-lime-500/40 bg-lime-950/60'
	},
	Neutral: {
		badge: 'border-slate-600 bg-slate-800 text-slate-300',
		tile: 'border-slate-600 bg-slate-800/60'
	}
};
