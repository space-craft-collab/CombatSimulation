import { describe, expect, it } from 'vitest';
import {
	chooseAbility,
	cooldownText,
	effectLabel,
	effectivenessOf,
	isReady,
	matchupHint,
	moveOf,
	needsTarget,
	readinessText,
	summaryOf,
	unknownAbility
} from './abilities';
import type { Combatant } from './battle';
import type { Ability } from './catalog';

function ability(overrides: Partial<Ability> = {}): Ability {
	return {
		id: 'strike',
		name: 'Strike',
		element: 'Neutral',
		power: 100,
		cooldown: 0,
		effect: 'None',
		effectValue: 0,
		effectRounds: 0,
		targetsSelf: false,
		...overrides
	};
}

const FLAME_DASH = ability({
	id: 'flame-dash',
	name: 'Flame Dash',
	element: 'Fire',
	power: 150,
	cooldown: 2
});
const EMBER_BITE = ability({
	id: 'ember-bite',
	name: 'Ember Bite',
	element: 'Fire',
	power: 80,
	cooldown: 3,
	effect: 'Burn',
	effectValue: 4,
	effectRounds: 3
});
const SHELL_UP = ability({
	id: 'shell-up',
	name: 'Shell Up',
	power: 0,
	cooldown: 4,
	effect: 'Guard',
	effectValue: 50,
	effectRounds: 3,
	targetsSelf: true
});
const REGROW = ability({
	id: 'regrow',
	name: 'Regrow',
	element: 'Nature',
	power: 0,
	cooldown: 4,
	effect: 'Heal',
	effectValue: 30,
	targetsSelf: true
});

function fox(roundsLeft: Record<string, number> = {}, slot = 0): Combatant {
	return {
		slot,
		participantId: 'ada',
		isBot: false,
		speciesId: 'ember-fox',
		hitPoints: 40,
		maxHitPoints: 40,
		abilities: ['strike', 'flame-dash', 'ember-bite'].map((abilityId) => ({
			abilityId,
			roundsLeft: roundsLeft[abilityId] ?? 0
		})),
		effects: []
	};
}

describe('effectivenessOf', () => {
	it.each([
		['Fire', 'Nature'],
		['Nature', 'Frost'],
		['Frost', 'Fire']
	] as const)('%s is strong against %s and weak the other way round', (strong, weak) => {
		expect(effectivenessOf(strong, weak)).toBe('Strong');
		expect(effectivenessOf(weak, strong)).toBe('Weak');
	});

	it('is normal between equal elements', () => {
		expect(effectivenessOf('Fire', 'Fire')).toBe('Normal');
	});

	it('is normal for a neutral ability', () => {
		expect(effectivenessOf('Neutral', 'Nature')).toBe('Normal');
	});
});

describe('matchupHint', () => {
	it('names the advantage', () => {
		expect(matchupHint(FLAME_DASH, 'Nature')).toEqual({
			effectiveness: 'Strong',
			text: 'strong against Nature'
		});
	});

	it('names the disadvantage', () => {
		expect(matchupHint(FLAME_DASH, 'Frost')).toEqual({
			effectiveness: 'Weak',
			text: 'weak against Frost'
		});
	});

	it('says nothing for an even matchup', () => {
		expect(matchupHint(FLAME_DASH, 'Fire')).toBeNull();
	});

	it('says nothing for the neutral strike', () => {
		expect(matchupHint(ability(), 'Nature')).toBeNull();
	});

	it('says nothing for an ability the monster uses on itself', () => {
		expect(matchupHint(REGROW, 'Frost')).toBeNull();
	});
});

describe('summaryOf', () => {
	it.each([
		[ability(), '100 % · Neutral'],
		[FLAME_DASH, '150 % · Fire'],
		[EMBER_BITE, '80 % · Fire · burn 4 for 3 rounds'],
		[SHELL_UP, 'Guard 50 % for 3 rounds'],
		[REGROW, 'Heal 30 %'],
		[
			ability({ power: 80, element: 'Frost', effect: 'Slow', effectValue: 50, effectRounds: 1 }),
			'80 % · Frost · slow 50 % for 1 round'
		]
	])('describes %o as %j', (described, summary) => {
		expect(summaryOf(described)).toBe(summary);
	});

	it('is empty for an ability that does nothing known', () => {
		expect(summaryOf(unknownAbility('mystery'))).toBe('');
	});
});

describe('cooldownText', () => {
	it('names the cooldown', () => {
		expect(cooldownText(FLAME_DASH)).toBe('cooldown 2');
	});

	it('is null for an ability ready every round', () => {
		expect(cooldownText(ability())).toBeNull();
	});
});

describe('readiness', () => {
	it('is ready without rounds left', () => {
		expect(isReady({ abilityId: 'strike', roundsLeft: 0 })).toBe(true);
		expect(readinessText({ abilityId: 'strike', roundsLeft: 0 })).toBe('Ready');
	});

	it('counts the rounds until it is ready', () => {
		expect(isReady({ abilityId: 'flame-dash', roundsLeft: 2 })).toBe(false);
		expect(readinessText({ abilityId: 'flame-dash', roundsLeft: 2 })).toBe('ready in 2');
	});
});

describe('chooseAbility', () => {
	it('defaults to the first ready ability', () => {
		expect(chooseAbility(fox(), null)).toBe('strike');
	});

	it('takes the picked ability while it is ready', () => {
		expect(chooseAbility(fox(), 'flame-dash')).toBe('flame-dash');
	});

	it('falls back to the default once the picked ability is on cooldown', () => {
		expect(chooseAbility(fox({ 'flame-dash': 2 }), 'flame-dash')).toBe('strike');
	});

	it('falls back to the default for an ability the monster does not know', () => {
		expect(chooseAbility(fox(), 'shell-up')).toBe('strike');
	});

	it('skips abilities on cooldown for the default', () => {
		expect(chooseAbility(fox({ strike: 1 }), null)).toBe('flame-dash');
	});

	it('has nothing for a monster without abilities', () => {
		expect(chooseAbility({ ...fox(), abilities: [] }, null)).toBeUndefined();
	});
});

describe('moveOf', () => {
	const target = { ...fox({}, 1), participantId: 'bot', isBot: true };

	it('needs a target for an attack', () => {
		expect(needsTarget(FLAME_DASH)).toBe(true);
		expect(moveOf(fox(), FLAME_DASH, target)).toEqual({
			actorSlot: 0,
			targetSlot: 1,
			abilityId: 'flame-dash'
		});
	});

	it('is not ready while an attack has no target', () => {
		expect(moveOf(fox(), FLAME_DASH, undefined)).toBeNull();
	});

	it('targets the actor itself for an ability used on oneself, whatever was picked', () => {
		expect(needsTarget(SHELL_UP)).toBe(false);
		expect(moveOf(fox(), SHELL_UP, target)).toEqual({
			actorSlot: 0,
			targetSlot: 0,
			abilityId: 'shell-up'
		});
		expect(moveOf(fox(), SHELL_UP, undefined)?.targetSlot).toBe(0);
	});
});

describe('effectLabel', () => {
	it.each([
		[
			{ kind: 'Burn', value: 4, roundsLeft: 2 },
			'Burn 2',
			'Burn: burns for 4 each round, 2 rounds left'
		],
		[
			{ kind: 'Guard', value: 50, roundsLeft: 1 },
			'Guard 1',
			'Guard: takes 50 % less damage, 1 round left'
		],
		[{ kind: 'Slow', value: 50, roundsLeft: 3 }, 'Slow 3', 'Slow: is 50 % slower, 3 rounds left']
	] as const)('labels %o', (effect, short, long) => {
		expect(effectLabel(effect)).toEqual({ short, long });
	});
});
