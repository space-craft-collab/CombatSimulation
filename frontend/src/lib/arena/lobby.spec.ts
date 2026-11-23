import { describe, expect, it } from 'vitest';
import {
	JOIN_PARAM,
	inviteQuery,
	joinRefusal,
	lobbyOf,
	openBattlesOf,
	type BattleSummary,
	type OpenBattle
} from './lobby';

function summary(overrides: Partial<BattleSummary> = {}): BattleSummary {
	return {
		battleId: 'b-1',
		status: 'WaitingForPlayers',
		participantIds: ['ada'],
		...overrides
	};
}

function open(battleId: string, hostId: string): OpenBattle {
	return { battleId, hostId };
}

describe('openBattlesOf', () => {
	it('takes the battles that wait for players, with the first side as the host', () => {
		expect(openBattlesOf([summary()])).toEqual([open('b-1', 'ada')]);
	});

	it('leaves out the running battles, which nobody can join', () => {
		expect(
			openBattlesOf([
				summary({ battleId: 'b-1', status: 'InProgress', participantIds: ['ada', 'bob'] }),
				summary({ battleId: 'b-2', participantIds: ['cy'] })
			])
		).toEqual([open('b-2', 'cy')]);
	});

	it('leaves out a waiting battle without a side', () => {
		expect(openBattlesOf([summary({ participantIds: [] })])).toEqual([]);
	});
});

describe('lobbyOf', () => {
	const battles = [open('b-1', 'ada'), open('b-2', 'bob'), open('b-3', 'cy')];

	it("does not offer the visitor's own battle to join", () => {
		expect(lobbyOf(battles, 'bob')).toEqual({
			own: [open('b-2', 'bob')],
			joinable: [open('b-1', 'ada'), open('b-3', 'cy')]
		});
	});

	it('offers every battle before a nickname is chosen', () => {
		expect(lobbyOf(battles, null)).toEqual({ own: [], joinable: battles });
	});

	it('lists the battle the visitor was sent to first, the rest in their order', () => {
		expect(lobbyOf(battles, 'dee', 'b-3').joinable).toEqual([
			open('b-3', 'cy'),
			open('b-1', 'ada'),
			open('b-2', 'bob')
		]);
	});

	it('keeps the order when the battle the visitor was sent to is gone', () => {
		expect(lobbyOf(battles, 'dee', 'b-9').joinable).toEqual(battles);
	});
});

describe('inviteQuery', () => {
	it('names the battle in the join parameter', () => {
		expect(inviteQuery('0f8f-12')).toBe(`?${JOIN_PARAM}=0f8f-12`);
	});
});

describe('joinRefusal', () => {
	it('explains a conflict as a battle someone else took', () => {
		expect(joinRefusal(409, 'Battle b-1 is InProgress, expected WaitingForPlayers.')).toMatch(
			/no longer open/
		);
	});

	it('explains a battle that is gone', () => {
		expect(joinRefusal(404, 'Not Found')).toBe('That battle does not exist any more.');
	});

	it.each([400, undefined])('passes on what the API said for status %s', (status) => {
		expect(joinRefusal(status, 'Unknown species.')).toBe('Unknown species.');
	});
});
