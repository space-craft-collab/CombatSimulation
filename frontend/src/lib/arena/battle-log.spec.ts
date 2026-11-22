import { describe, expect, it } from 'vitest';
import type { Combatant, RoundEvent } from './battle';
import { ownedName, sentenceOf, type LogNames } from './battle-log';

const NAMES: LogNames = {
	monster: (slot) => (slot === 0 ? 'your Ember Fox' : "the bot's Moss Golem"),
	ability: (id) =>
		({ strike: 'Strike', 'flame-dash': 'Flame Dash', 'ember-bite': 'Ember Bite' })[id] ?? id
};

function event(overrides: Partial<RoundEvent> = {}): RoundEvent {
	return {
		kind: 'Ability',
		actorSlot: 0,
		targetSlot: 1,
		abilityId: 'strike',
		damage: 10,
		healing: 0,
		targetHitPoints: 70,
		effectiveness: 'Normal',
		effect: 'None',
		...overrides
	};
}

describe('sentenceOf', () => {
	it('tells a plain hit', () => {
		expect(sentenceOf(event(), NAMES)).toBe(
			"Your Ember Fox hits the bot's Moss Golem with Strike for 10."
		);
	});

	it('says when a hit is super effective', () => {
		expect(
			sentenceOf(event({ abilityId: 'flame-dash', damage: 27, effectiveness: 'Strong' }), NAMES)
		).toBe(
			"Your Ember Fox hits the bot's Moss Golem with Flame Dash for 27. It's super effective."
		);
	});

	it('says when a hit is not very effective', () => {
		expect(sentenceOf(event({ actorSlot: 1, targetSlot: 0, effectiveness: 'Weak' }), NAMES)).toBe(
			"The bot's Moss Golem hits your Ember Fox with Strike for 10. It's not very effective."
		);
	});

	it('tells a burn that took hold', () => {
		expect(sentenceOf(event({ abilityId: 'ember-bite', damage: 6, effect: 'Burn' }), NAMES)).toBe(
			"Your Ember Fox hits the bot's Moss Golem with Ember Bite for 6 and sets it on fire."
		);
	});

	it('tells a slow that took hold', () => {
		expect(sentenceOf(event({ abilityId: 'chill-wind', effect: 'Slow' }), NAMES)).toBe(
			"Your Ember Fox hits the bot's Moss Golem with chill-wind for 10 and slows it."
		);
	});

	it('tells a defeat instead of the effect on the fallen monster', () => {
		expect(
			sentenceOf(event({ abilityId: 'ember-bite', effect: 'Burn', targetHitPoints: 0 }), NAMES)
		).toBe("Your Ember Fox hits the bot's Moss Golem with Ember Bite for 10 and defeats it.");
	});

	it('tells a heal', () => {
		expect(
			sentenceOf(
				event({
					actorSlot: 1,
					targetSlot: 1,
					abilityId: 'regrow',
					damage: 0,
					healing: 24,
					effect: 'Heal'
				}),
				{ ...NAMES, ability: () => 'Regrow' }
			)
		).toBe("The bot's Moss Golem uses Regrow and heals 24.");
	});

	it('tells a heal at full health', () => {
		expect(
			sentenceOf(event({ targetSlot: 0, damage: 0, healing: 0, effect: 'Heal' }), {
				...NAMES,
				ability: () => 'Regrow'
			})
		).toBe('Your Ember Fox uses Regrow, but has nothing to heal.');
	});

	it('tells a guard', () => {
		expect(
			sentenceOf(event({ targetSlot: 0, damage: 0, effect: 'Guard' }), {
				...NAMES,
				ability: () => 'Shell Up'
			})
		).toBe('Your Ember Fox uses Shell Up and guards itself.');
	});

	it('tells an effect without damage, and no effectiveness for it', () => {
		expect(sentenceOf(event({ damage: 0, effect: 'Slow', effectiveness: 'Strong' }), NAMES)).toBe(
			"Your Ember Fox uses Strike on the bot's Moss Golem and slows it."
		);
	});

	it('tells the burn damage at the end of a round', () => {
		expect(
			sentenceOf(
				event({ kind: 'Burn', actorSlot: 1, abilityId: null, damage: 4, targetHitPoints: 40 }),
				NAMES
			)
		).toBe("The bot's Moss Golem takes 4 from its burn.");
	});

	it('tells a monster that falls to its burn', () => {
		expect(
			sentenceOf(
				event({ kind: 'Burn', actorSlot: 1, abilityId: null, damage: 4, targetHitPoints: 0 }),
				NAMES
			)
		).toBe("The bot's Moss Golem takes 4 from its burn and is defeated.");
	});
});

describe('ownedName', () => {
	function monster(participantId: string, isBot = false): Combatant {
		return {
			slot: 0,
			participantId,
			isBot,
			speciesId: 'ember-fox',
			hitPoints: 40,
			maxHitPoints: 40,
			abilities: [],
			effects: []
		};
	}

	it("names the viewer's own monster", () => {
		expect(ownedName(monster('ada'), 'ada', 'Ember Fox')).toBe('your Ember Fox');
	});

	it("names the bot's monster", () => {
		expect(ownedName(monster('bot', true), 'ada', 'Lava Crab')).toBe("the bot's Lava Crab");
	});

	it("names another player's monster, also for a spectator", () => {
		expect(ownedName(monster('grace'), null, 'Frost Owl')).toBe("grace's Frost Owl");
	});
});
