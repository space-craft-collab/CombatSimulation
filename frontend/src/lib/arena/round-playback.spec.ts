import { describe, expect, it } from 'vitest';
import type { Combatant, RoundEvent } from './battle';
import { cueOf, hitPointsAt } from './round-playback';

function monster(slot: number, hitPoints: number): Combatant {
	return {
		slot,
		participantId: slot < 3 ? 'ada' : 'bot',
		isBot: slot >= 3,
		speciesId: 'ember-fox',
		hitPoints,
		maxHitPoints: 40,
		abilities: [],
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

function burn(slot: number, damage: number, targetHitPoints: number): RoundEvent {
	return { ...hit(slot, slot, damage, targetHitPoints), kind: 'Burn', abilityId: null };
}

function heal(slot: number, healing: number, targetHitPoints: number): RoundEvent {
	return {
		...hit(slot, slot, 0, targetHitPoints),
		abilityId: 'regrow',
		healing,
		effect: 'Heal'
	};
}

function guard(slot: number, hitPoints: number): RoundEvent {
	return { ...hit(slot, slot, 0, hitPoints), abilityId: 'stone-skin', effect: 'Guard' };
}

/** The hit points a monster shows at every step of a round, step 0 first. */
function stepsOf(events: RoundEvent[], combatant: Combatant): number[] {
	return Array.from({ length: events.length + 1 }, (_, step) =>
		hitPointsAt(events, step, combatant)
	);
}

describe('hitPointsAt', () => {
	it('steps down with a hit and then a burn on one monster', () => {
		const events = [hit(0, 3, 10, 30), hit(4, 1, 5, 35), burn(3, 4, 26)];

		expect(stepsOf(events, monster(3, 26))).toEqual([40, 30, 30, 26]);
	});

	it('steps down with each of several hits on one monster', () => {
		const events = [hit(0, 3, 10, 30), hit(1, 3, 12, 18)];

		expect(stepsOf(events, monster(3, 18))).toEqual([40, 30, 18]);
	});

	it('goes up with a heal', () => {
		const events = [hit(3, 0, 8, 20), heal(3, 6, 38)];

		expect(stepsOf(events, monster(3, 38))).toEqual([32, 32, 38]);
	});

	it('keeps the hit points through a guard', () => {
		const events = [guard(3, 25), hit(0, 1, 5, 35)];

		expect(stepsOf(events, monster(3, 25))).toEqual([25, 25, 25]);
	});

	it('shows a monster defeated from the step that defeats it on', () => {
		// The fallen monster neither moves nor burns any more in that round.
		const events = [hit(0, 3, 12, 0), hit(1, 4, 9, 31)];

		expect(stepsOf(events, monster(3, 0))).toEqual([12, 0, 0]);
	});

	it('holds a monster the round did not touch at its hit points', () => {
		const events = [hit(0, 3, 10, 30)];

		expect(stepsOf(events, monster(1, 22))).toEqual([22, 22]);
	});

	it('is the state after the round for an empty round', () => {
		expect(hitPointsAt([], 0, monster(3, 17))).toBe(17);
	});

	it('is the round start at step 0 and its end at the last step', () => {
		const events = [hit(0, 3, 10, 30), hit(3, 0, 7, 33), burn(0, 3, 30)];

		expect(hitPointsAt(events, 0, monster(0, 30))).toBe(40);
		expect(hitPointsAt(events, 0, monster(3, 30))).toBe(40);
		expect(hitPointsAt(events, events.length, monster(0, 30))).toBe(30);
		expect(hitPointsAt(events, events.length, monster(3, 30))).toBe(30);
	});
});

describe('cueOf', () => {
	it('has the actor lunge and its target struck', () => {
		const events = [hit(0, 3, 10, 30)];

		expect(cueOf(events, 1, 0)).toEqual({ kind: 'lunge', step: 1, targetSlot: 3 });
		expect(cueOf(events, 1, 3)).toEqual({ kind: 'struck', step: 1, damage: 10 });
	});

	it('has a burning monster pulse with its damage, nobody lunging', () => {
		const events = [burn(3, 4, 26)];

		expect(cueOf(events, 1, 3)).toEqual({ kind: 'pulse', step: 1, damage: 4, healing: 0 });
	});

	it('has a healing monster pulse with its healing', () => {
		expect(cueOf([heal(3, 6, 38)], 1, 3)).toEqual({
			kind: 'pulse',
			step: 1,
			damage: 0,
			healing: 6
		});
	});

	it('has a guarding monster pulse with nothing to show', () => {
		expect(cueOf([guard(3, 25)], 1, 3)).toEqual({
			kind: 'pulse',
			step: 1,
			damage: 0,
			healing: 0
		});
	});

	it('takes the latest of a monster hit and then burning', () => {
		const events = [hit(0, 3, 10, 30), hit(4, 1, 5, 35), burn(3, 4, 26)];

		expect(cueOf(events, 1, 3)).toEqual({ kind: 'struck', step: 1, damage: 10 });
		expect(cueOf(events, 2, 3)).toEqual({ kind: 'struck', step: 1, damage: 10 });
		expect(cueOf(events, 3, 3)).toEqual({ kind: 'pulse', step: 3, damage: 4, healing: 0 });
	});

	it('is nothing at step 0', () => {
		expect(cueOf([hit(0, 3, 10, 30)], 0, 3)).toBeNull();
	});

	it('is nothing for a monster the round did not touch', () => {
		expect(cueOf([hit(0, 3, 10, 30)], 1, 2)).toBeNull();
	});

	it('is nothing in an empty round', () => {
		expect(cueOf([], 0, 0)).toBeNull();
	});
});
