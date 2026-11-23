import { describe, expect, it } from 'vitest';
import type { Battle, Combatant } from './battle';
import { verdictOf } from './verdict';

function monster(slot: number, participantId: string, isBot = false): Combatant {
	return {
		slot,
		participantId,
		isBot,
		speciesId: 'ember-fox',
		hitPoints: 0,
		maxHitPoints: 40,
		abilities: [],
		effects: []
	};
}

function completed(winnerId: string | null, combatants: Combatant[]): Battle {
	return {
		battleId: 'b-1',
		arenaId: 'volcano',
		status: 'Completed',
		round: 7,
		winnerId,
		turnDeadline: null,
		combatants,
		difficulty: 'Normal'
	};
}

const AGAINST_BOT = [monster(0, 'ada'), monster(1, 'bot', true)];
const AGAINST_HUMAN = [monster(0, 'ada'), monster(1, 'bob')];

describe('verdictOf', () => {
	it('is nothing while the battle runs', () => {
		expect(verdictOf({ ...completed(null, AGAINST_BOT), status: 'InProgress' }, 'ada')).toBeNull();
	});

	it('tells the winner they won, and in which round', () => {
		expect(verdictOf(completed('ada', AGAINST_HUMAN), 'ada')).toEqual({
			title: 'You won!',
			detail: 'Decided in round 7.'
		});
	});

	it('names the bot when it won', () => {
		expect(verdictOf(completed('bot', AGAINST_BOT), 'ada')?.title).toBe('The bot won.');
	});

	it('names a human opponent who won, not the bot', () => {
		expect(verdictOf(completed('bob', AGAINST_HUMAN), 'ada')?.title).toBe('bob won.');
	});

	it.each([
		['ada', 'ada won.'],
		['bot', 'The bot won.']
	])('tells a spectator who won when %s did', (winnerId, title) => {
		expect(verdictOf(completed(winnerId, AGAINST_BOT), null)?.title).toBe(title);
	});

	it('calls a battle without a winner abandoned', () => {
		expect(verdictOf(completed(null, AGAINST_HUMAN), 'ada')).toEqual({
			title: 'The battle was abandoned.',
			detail: 'Nobody moved before the deadline.'
		});
	});

	it('tells the host of an open battle nobody joined', () => {
		expect(verdictOf({ ...completed(null, [monster(0, 'ada')]), round: 0 }, 'ada')?.title).toBe(
			'Nobody joined this battle.'
		);
	});
});
