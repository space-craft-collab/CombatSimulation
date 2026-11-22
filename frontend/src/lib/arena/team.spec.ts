import { describe, expect, it } from 'vitest';
import type { Species } from './catalog';
import { MAX_TEAM_SIZE, countOf, parseTeam, pickerQuery, statCeilings } from './team';

function species(id: string, overrides: Partial<Species> = {}): Species {
	return {
		id,
		name: id,
		element: 'Fire',
		maxHitPoints: 40,
		attack: 20,
		defense: 5,
		speed: 10,
		abilities: [],
		...overrides
	};
}

const FOX = species('ember-fox', { maxHitPoints: 44, attack: 21, defense: 4, speed: 14 });
const YAK = species('glacier-yak', { maxHitPoints: 62, attack: 13, defense: 8, speed: 4 });

describe('statCeilings', () => {
	it('takes the highest value of each stat, whichever species has it', () => {
		expect(statCeilings([FOX, YAK])).toEqual({
			maxHitPoints: 62,
			attack: 21,
			defense: 8,
			speed: 14
		});
	});

	it('is 1 at least, so a bar of an empty roster divides by no zero', () => {
		expect(statCeilings([])).toEqual({ maxHitPoints: 1, attack: 1, defense: 1, speed: 1 });
	});
});

describe('countOf', () => {
	it('counts a species fielded twice as two', () => {
		expect(countOf(['ember-fox', 'glacier-yak', 'ember-fox'], 'ember-fox')).toBe(2);
	});

	it('is 0 for a species that is not in the team', () => {
		expect(countOf(['ember-fox'], 'glacier-yak')).toBe(0);
	});
});

describe('parseTeam', () => {
	it('reads the ids in their order, a double included', () => {
		expect(parseTeam('glacier-yak,ember-fox,glacier-yak', [FOX, YAK])).toEqual([
			'glacier-yak',
			'ember-fox',
			'glacier-yak'
		]);
	});

	it.each([null, ''])('is no team for %j', (raw) => {
		expect(parseTeam(raw, [FOX])).toEqual([]);
	});

	it('drops what is no id of a listed species', () => {
		expect(parseTeam('ember-fox,retired-newt,,7', [FOX, YAK])).toEqual(['ember-fox']);
	});

	it('cuts a team that is too long to the team size', () => {
		const raw = Array.from({ length: MAX_TEAM_SIZE + 2 }, () => 'ember-fox').join(',');

		expect(parseTeam(raw, [FOX])).toHaveLength(MAX_TEAM_SIZE);
	});
});

describe('pickerQuery', () => {
	it('joins the ids with commas', () => {
		expect(pickerQuery(['ember-fox', 'glacier-yak'], 'Normal')).toBe('?team=ember-fox,glacier-yak');
	});

	it('carries a difficulty other than the default', () => {
		expect(pickerQuery(['ember-fox'], 'Easy')).toBe('?team=ember-fox&difficulty=Easy');
	});

	it('carries the difficulty without a team', () => {
		expect(pickerQuery([], 'Easy')).toBe('?difficulty=Easy');
	});

	it('is empty for no team on the default', () => {
		expect(pickerQuery([], 'Normal')).toBe('');
	});

	it('is read back by parseTeam', () => {
		const team = ['glacier-yak', 'ember-fox', 'glacier-yak'];
		const raw = new URLSearchParams(pickerQuery(team, 'Easy')).get('team');

		expect(parseTeam(raw, [FOX, YAK])).toEqual(team);
	});
});
