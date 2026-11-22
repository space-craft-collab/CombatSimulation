// The team a visitor picks before a battle: its size, what the
// picker shows about it, and the team "Fight again" hands back to
// the picker in the address, with the difficulty. Pure functions.

import { DIFFICULTIES, type Difficulty } from './battle.ts';
import type { Species } from './catalog.ts';

/** The most monsters one side may field (`StartBattleRequest.MaxTeamSize`). */
export const MAX_TEAM_SIZE = 3;

/** The query parameter "Fight again" carries the team in. */
export const TEAM_PARAM = 'team';

/** The query parameter "Fight again" carries the difficulty in. */
export const DIFFICULTY_PARAM = 'difficulty';

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
 * Reads a team from the address: species ids, separated by commas.
 * Ids the Catalog does not list are dropped, and the rest is cut
 * to the team size. No parameter is no team.
 */
export function parseTeam(raw: string | null, known: Species[]): string[] {
	if (!raw) {
		return [];
	}

	const ids = new Set(known.map((species) => species.id));

	return raw
		.split(',')
		.filter((id) => ids.has(id))
		.slice(0, MAX_TEAM_SIZE);
}

/**
 * The query that hands a team and a difficulty to the picker,
 * `?team=a,b&difficulty=Easy`. What the picker starts with anyway,
 * no team and the default difficulty, is left out.
 */
export function pickerQuery(team: string[], difficulty: Difficulty): string {
	const parts = [
		// Each id encoded on its own, so the commas stay readable.
		team.length > 0 ? `${TEAM_PARAM}=${team.map(encodeURIComponent).join(',')}` : null,
		difficulty !== DIFFICULTIES[0] ? `${DIFFICULTY_PARAM}=${difficulty}` : null
	].filter((part) => part !== null);

	return parts.length === 0 ? '' : `?${parts.join('&')}`;
}
