import { describe, expect, it } from 'vitest';
import {
	DIFFICULTIES,
	applyRound,
	clockOf,
	endingFor,
	newerOf,
	outcomeFor,
	parseDifficulty,
	relationOf,
	secondsUntil,
	sidesOf,
	type Battle,
	type BattleEvent,
	type Combatant,
	type RoundEvent
} from './battle';

const PLAYER = 'ada';
const BOT = 'bot';

function monster(slot: number, participantId: string, hitPoints: number): Combatant {
	return {
		slot,
		participantId,
		isBot: participantId === BOT,
		speciesId: 'ember-fox',
		hitPoints,
		maxHitPoints: 40,
		abilities: [{ abilityId: 'strike', roundsLeft: 0 }],
		effects: []
	};
}

function hit(
	actorSlot: number,
	targetSlot: number,
	damage: number,
	targetHitPoints: number
): RoundEvent {
	return {
		kind: 'Ability',
		actorSlot,
		targetSlot,
		abilityId: 'strike',
		damage,
		healing: 0,
		targetHitPoints,
		effectiveness: 'Normal',
		effect: 'None'
	};
}

function battle(overrides: Partial<Battle> = {}): Battle {
	return {
		battleId: 'b-1',
		arenaId: 'volcano',
		status: 'InProgress',
		round: 2,
		winnerId: null,
		turnDeadline: '2026-11-21T20:00:00.000Z',
		combatants: [monster(0, PLAYER, 30), monster(1, BOT, 30)],
		difficulty: 'Normal',
		...overrides
	};
}

function roundEvent(overrides: Partial<BattleEvent> = {}): BattleEvent {
	return {
		battleId: 'b-1',
		round: 2,
		status: 'InProgress',
		winnerId: null,
		turnDeadline: '2026-11-21T20:01:00.000Z',
		events: [hit(0, 1, 10, 20), hit(1, 0, 10, 20)],
		combatants: [monster(0, PLAYER, 20), monster(1, BOT, 20)],
		...overrides
	};
}

describe('relationOf', () => {
	it('is next for the round the battle waits for', () => {
		expect(relationOf(battle(), roundEvent())).toBe('next');
	});

	it('is old for a round the battle is already past', () => {
		expect(relationOf(battle({ round: 3 }), roundEvent())).toBe('old');
	});

	it('is old for any round once the battle is completed', () => {
		expect(relationOf(battle({ status: 'Completed' }), roundEvent())).toBe('old');
	});

	it('is a gap when rounds in between are missing', () => {
		expect(relationOf(battle(), roundEvent({ round: 4 }))).toBe('gap');
	});
});

describe('applyRound', () => {
	it('takes the monsters from the delta and moves on to the next round', () => {
		const after = applyRound(battle(), roundEvent());

		expect(after.round).toBe(3);
		expect(after.status).toBe('InProgress');
		expect(after.turnDeadline).toBe('2026-11-21T20:01:00.000Z');
		expect(after.combatants.map((combatant) => combatant.hitPoints)).toEqual([20, 20]);
	});

	it('takes cooldowns and effects from the delta', () => {
		const burning: Combatant = {
			...monster(1, BOT, 16),
			abilities: [{ abilityId: 'flame-dash', roundsLeft: 2 }],
			effects: [{ kind: 'Burn', value: 4, roundsLeft: 2 }]
		};

		const after = applyRound(
			battle(),
			roundEvent({ combatants: [monster(0, PLAYER, 20), burning] })
		);

		expect(after.combatants[1]).toEqual(burning);
	});

	it('stays on the last round and names the winner when the battle is decided', () => {
		const after = applyRound(
			battle(),
			roundEvent({
				status: 'Completed',
				winnerId: PLAYER,
				turnDeadline: null,
				events: [hit(0, 1, 30, 0)],
				combatants: [monster(0, PLAYER, 30), monster(1, BOT, 0)]
			})
		);

		expect(after).toMatchObject({
			round: 2,
			status: 'Completed',
			winnerId: PLAYER,
			turnDeadline: null
		});
		expect(after.combatants[1].hitPoints).toBe(0);
	});

	it('completes an abandoned battle without touching the monsters', () => {
		const before = battle();

		const after = applyRound(
			before,
			roundEvent({
				status: 'Completed',
				turnDeadline: null,
				events: [],
				combatants: before.combatants
			})
		);

		expect(after.status).toBe('Completed');
		expect(after.winnerId).toBeNull();
		expect(after.combatants).toEqual(before.combatants);
	});

	it('keeps the difficulty, which the delta does not carry', () => {
		const after = applyRound(battle({ difficulty: 'Easy' }), roundEvent());

		expect(after.difficulty).toBe('Easy');
	});

	it('keeps the difficulty once the round decides the battle', () => {
		const after = applyRound(
			battle({ difficulty: 'Easy' }),
			roundEvent({ status: 'Completed', winnerId: PLAYER })
		);

		expect(after.difficulty).toBe('Easy');
	});

	it('leaves the given state unchanged', () => {
		const before = battle();

		applyRound(before, roundEvent());

		expect(before).toEqual(battle());
	});
});

describe('newerOf', () => {
	it('takes a state of a later round', () => {
		const later = battle({ round: 3 });

		expect(newerOf(battle(), later)).toBe(later);
	});

	it('keeps the current state over one of an earlier round', () => {
		const current = battle({ round: 3 });

		expect(newerOf(current, battle({ round: 2 }))).toBe(current);
	});

	it('takes a completed state whatever its round', () => {
		const completed = battle({ round: 2, status: 'Completed' });

		expect(newerOf(battle({ round: 5 }), completed)).toBe(completed);
	});

	it('takes the candidate when both are equally far', () => {
		const candidate = battle();

		expect(newerOf(battle(), candidate)).toBe(candidate);
	});

	it('ignores a state of another battle', () => {
		const current = battle();

		expect(newerOf(current, battle({ battleId: 'b-2', round: 9 }))).toBe(current);
	});
});

describe('outcomeFor', () => {
	it('is undecided while the battle runs', () => {
		expect(outcomeFor(battle(), PLAYER)).toBe('undecided');
	});

	it.each([
		[PLAYER, 'won'],
		[BOT, 'lost']
	] as const)('with %s as the winner the player has %s', (winnerId, outcome) => {
		expect(outcomeFor(battle({ status: 'Completed', winnerId }), PLAYER)).toBe(outcome);
	});

	it('is abandoned when a completed battle has no winner', () => {
		expect(outcomeFor(battle({ status: 'Completed' }), PLAYER)).toBe('abandoned');
	});

	it('is unjoined when an open battle completed with its host alone', () => {
		const expired = battle({ status: 'Completed', round: 0, combatants: [monster(0, PLAYER, 30)] });

		expect(outcomeFor(expired, PLAYER)).toBe('unjoined');
	});
});

describe('endingFor', () => {
	it.each([
		[PLAYER, 'won'],
		[BOT, 'lost']
	] as const)('with %s as the winner the player has %s', (winnerId, ending) => {
		expect(endingFor([PLAYER, BOT], winnerId, PLAYER)).toBe(ending);
	});

	it('is abandoned with two sides and no winner', () => {
		expect(endingFor([PLAYER, 'bob'], null, PLAYER)).toBe('abandoned');
	});

	it('is unjoined with the host alone', () => {
		expect(endingFor([PLAYER], null, PLAYER)).toBe('unjoined');
	});
});

describe('sidesOf', () => {
	it('names each side once, in the order of its first slot', () => {
		const combatants = [monster(0, 'bob', 30), monster(1, 'bob', 30), monster(2, PLAYER, 30)];

		expect(sidesOf(battle({ combatants }))).toEqual(['bob', PLAYER]);
	});

	it('has the host alone while an open battle waits', () => {
		expect(sidesOf(battle({ combatants: [monster(0, PLAYER, 30)] }))).toEqual([PLAYER]);
	});
});

describe('clockOf', () => {
	it.each([
		[300, '5:00'],
		[61, '1:01'],
		[9, '0:09'],
		[0, '0:00'],
		[-3, '0:00']
	])('shows %i seconds as %s', (seconds, clock) => {
		expect(clockOf(seconds)).toBe(clock);
	});
});

describe('secondsUntil', () => {
	const DEADLINE = '2026-11-21T20:00:00.000Z';

	it('rounds a started second up', () => {
		expect(secondsUntil(DEADLINE, Date.parse(DEADLINE) - 1500)).toBe(2);
	});

	it('never goes below zero', () => {
		expect(secondsUntil(DEADLINE, Date.parse(DEADLINE) + 5000)).toBe(0);
	});

	it('is null without a deadline', () => {
		expect(secondsUntil(null, 0)).toBeNull();
	});
});

describe('DIFFICULTIES', () => {
	it('offers Normal first, as the default, and Easy', () => {
		expect(DIFFICULTIES).toEqual(['Normal', 'Easy']);
	});
});

describe('parseDifficulty', () => {
	it.each(DIFFICULTIES)('reads %s', (difficulty) => {
		expect(parseDifficulty(difficulty)).toBe(difficulty);
	});

	it.each([null, '', 'easy', 'Hard'])('is Normal for %j', (raw) => {
		expect(parseDifficulty(raw)).toBe('Normal');
	});
});
