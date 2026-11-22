// The team a visitor picks before a battle: its size, what the
// picker shows about it, and the last one fielded, kept in the
// browser so "Fight again" starts from it. Pure functions, apart
// from the two that touch `localStorage`.

import type { Species } from './catalog.ts';

/** The most monsters one side may field (`StartBattleRequest.MaxTeamSize`). */
export const MAX_TEAM_SIZE = 3;

const STORAGE_KEY = 'arena.team';

/** The four base stats of a species, in the order the picker shows them. */
export const STATS = [
	{ key: 'maxHitPoints', label: 'HP', name: 'Hit points' },
	{ key: 'attack', label: 'ATK', name: 'Attack' },
	{ key: 'defense', label: 'DEF', name: 'Defense' },
	{ key: 'speed', label: 'SPD', name: 'Speed' }
] as const;

/** One of the base stats of a species. */
export type StatKey = (typeof STATS)[number]['key'];

/**
 * The highest value of each stat among `species`: what a stat bar
 * is measured against, so the bars compare the monsters on offer
 * and not against a fixed scale. At least 1, so nothing divides by 0.
 */
export function statCeilings(species: Species[]): Record<StatKey, number> {
	const ceiling = (key: StatKey) => Math.max(1, ...species.map((entry) => entry[key]));

	return {
		maxHitPoints: ceiling('maxHitPoints'),
		attack: ceiling('attack'),
		defense: ceiling('defense'),
		speed: ceiling('speed')
	};
}

/** How often a species is in the team; the same one may be fielded twice. */
export function countOf(team: string[], speciesId: string): number {
	return team.filter((id) => id === speciesId).length;
}

/**
 * Reads a stored team: a JSON array of species ids. Anything else
 * is no team. Ids the Catalog no longer lists are dropped, and the
 * rest is cut to the team size.
 */
export function parseStoredTeam(raw: string | null, known: Species[]): string[] {
	if (raw === null) {
		return [];
	}

	let stored: unknown;

	try {
		stored = JSON.parse(raw);
	} catch {
		// Not JSON: someone else wrote to the key.
		return [];
	}

	if (!Array.isArray(stored)) {
		return [];
	}

	const ids = new Set(known.map((species) => species.id));

	return stored
		.filter((id): id is string => typeof id === 'string' && ids.has(id))
		.slice(0, MAX_TEAM_SIZE);
}

// Storage can be unavailable (private mode, blocked by a policy).
// The team then lasts for this visit to the page only.

/** The team last picked in this browser, as far as the Catalog still lists it. */
export function loadStoredTeam(known: Species[]): string[] {
	try {
		return parseStoredTeam(localStorage.getItem(STORAGE_KEY), known);
	} catch {
		return [];
	}
}

/** Keeps the team for the next visit. */
export function storeTeam(team: string[]): void {
	try {
		localStorage.setItem(STORAGE_KEY, JSON.stringify(team));
	} catch {
		// See above: the team still holds on the page.
	}
}
