import { describe, expect, it } from 'vitest';
import type { Species } from './catalog';
import { MAX_TEAM_SIZE, countOf, parseStoredTeam, statCeilings } from './team';

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

describe('parseStoredTeam', () => {
	it('reads the ids in their order, a double included', () => {
		expect(parseStoredTeam('["glacier-yak","ember-fox","glacier-yak"]', [FOX, YAK])).toEqual([
			'glacier-yak',
			'ember-fox',
			'glacier-yak'
		]);
	});

	it('is no team when nothing is stored', () => {
		expect(parseStoredTeam(null, [FOX])).toEqual([]);
	});

	it.each(['', 'ember-fox', '{"team":["ember-fox"]}', '"ember-fox"', 'null'])(
		'is no team for %j',
		(raw) => {
			expect(parseStoredTeam(raw, [FOX])).toEqual([]);
		}
	);

	it('drops what is no id of a listed species', () => {
		expect(parseStoredTeam('["ember-fox","retired-newt",7,null]', [FOX, YAK])).toEqual([
			'ember-fox'
		]);
	});

	it('cuts a team that is too long to the team size', () => {
		const stored = JSON.stringify(Array.from({ length: MAX_TEAM_SIZE + 2 }, () => 'ember-fox'));

		expect(parseStoredTeam(stored, [FOX])).toHaveLength(MAX_TEAM_SIZE);
	});
});
