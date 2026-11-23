import { describe, expect, it } from 'vitest';
import {
	HISTORY_LIMIT,
	afterQuery,
	isSettled,
	lineOf,
	parsePlayerId,
	playerParam,
	summaryOf,
	whenOf,
	type Dashboard,
	type HistoryEntry
} from './dashboard';

const PLAYER = 'ada';
const ARENAS = [{ id: 'volcano', name: 'Volcano' }];

function entry(overrides: Partial<HistoryEntry> = {}): HistoryEntry {
	return {
		battleId: 'b-1',
		arenaId: 'volcano',
		completedAt: '2026-11-23T19:26:21.815Z',
		round: 7,
		winnerId: PLAYER,
		participantIds: [PLAYER, 'bot'],
		...overrides
	};
}

function dashboard(wins: number, losses: number, history: HistoryEntry[]): Dashboard {
	return { record: { wins, losses }, history };
}

describe('parsePlayerId', () => {
	it.each(['ada', 'Ada Lovelace', 'what?', 'Bot'])(
		'takes %j, a nickname a player may have',
		(raw) => {
			expect(parsePlayerId(raw)).toBe(raw);
		}
	);

	it.each(['bot', ' ada', 'ada ', 'a/b', '..', 'a'.repeat(65)])(
		'refuses %j, which no player can have',
		(raw) => {
			expect(parsePlayerId(raw)).toBeNull();
		}
	);
});

describe('playerParam', () => {
	it('escapes what would end the path segment', () => {
		expect(playerParam('what? #1')).toBe('what%3F%20%231');
	});

	it('leaves a plain name as it is', () => {
		expect(playerParam('e2e-123')).toBe('e2e-123');
	});
});

describe('afterQuery', () => {
	it('names the battle just over', () => {
		expect(afterQuery('b-1')).toBe('?after=b-1');
	});
});

describe('summaryOf', () => {
	it('has no win rate before the first decided battle', () => {
		expect(summaryOf({ wins: 0, losses: 0 })).toEqual({ decided: 0, winRate: null });
	});

	it('rounds the win rate to whole percent', () => {
		expect(summaryOf({ wins: 2, losses: 1 })).toEqual({ decided: 3, winRate: 67 });
	});
});

describe('isSettled', () => {
	it('is settled when no battle is waited for', () => {
		expect(isSettled(dashboard(0, 0, []), PLAYER, null)).toBe(true);
	});

	it('is not settled while the battle is not in the history', () => {
		expect(isSettled(dashboard(1, 0, [entry({ battleId: 'older' })]), PLAYER, 'b-1')).toBe(false);
	});

	it('is not settled while the record lags behind the history', () => {
		const history = [entry(), entry({ battleId: 'older', winnerId: 'bot' })];

		expect(isSettled(dashboard(0, 1, history), PLAYER, 'b-1')).toBe(false);
	});

	it('is settled once the record counts every decided battle listed', () => {
		const history = [entry(), entry({ battleId: 'older', winnerId: 'bot' })];

		expect(isSettled(dashboard(1, 1, history), PLAYER, 'b-1')).toBe(true);
	});

	it('does not wait for abandoned or unjoined battles to be counted', () => {
		const history = [
			entry({ winnerId: null }),
			entry({ battleId: 'b-2', participantIds: [PLAYER] })
		];

		expect(isSettled(dashboard(0, 0, history), PLAYER, 'b-2')).toBe(true);
	});

	it('takes a record that counts more than a full history lists', () => {
		const history = Array.from({ length: HISTORY_LIMIT }, (_, index) =>
			entry({ battleId: `b-${index + 1}` })
		);

		expect(isSettled(dashboard(80, 20, history), PLAYER, 'b-1')).toBe(true);
	});
});

describe('lineOf', () => {
	it('tells a win against the bot', () => {
		expect(lineOf(entry(), PLAYER, ARENAS)).toEqual({
			battleId: 'b-1',
			ending: 'won',
			result: 'Won',
			arenaName: 'Volcano',
			opponent: 'vs the bot',
			detail: 'Decided in round 7',
			completedAt: '2026-11-23T19:26:21.815Z'
		});
	});

	it('names a human opponent who won', () => {
		const line = lineOf(
			entry({ winnerId: 'bob', participantIds: ['bob', PLAYER] }),
			PLAYER,
			ARENAS
		);

		expect(line).toMatchObject({ ending: 'lost', result: 'Lost', opponent: 'vs bob' });
	});

	it('tells an abandoned battle and its round', () => {
		expect(lineOf(entry({ winnerId: null, round: 3 }), PLAYER, ARENAS)).toMatchObject({
			ending: 'abandoned',
			result: 'Abandoned',
			opponent: 'vs the bot',
			detail: 'No move before the deadline in round 3'
		});
	});

	it('has no opponent for an open battle nobody joined', () => {
		expect(
			lineOf(entry({ winnerId: null, round: 0, participantIds: [PLAYER] }), PLAYER, ARENAS)
		).toMatchObject({ ending: 'unjoined', result: 'Nobody joined', opponent: null });
	});

	it('falls back to the id of an arena the Catalog does not list', () => {
		expect(lineOf(entry({ arenaId: 'gone' }), PLAYER, ARENAS).arenaName).toBe('gone');
	});
});

describe('whenOf', () => {
	const ISO = '2026-11-23T23:26:21.815Z';

	it('says UTC when it renders in UTC', () => {
		expect(whenOf(ISO, 'UTC')).toBe('23 Nov 2026, 23:26 UTC');
	});

	it('shifts into another time zone, the date included', () => {
		expect(whenOf(ISO, 'Europe/Berlin')).toBe('24 Nov 2026, 00:26');
	});
});
