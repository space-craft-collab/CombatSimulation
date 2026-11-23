// A player's dashboard: their record of decided battles and their
// last battles, told from their side. Pure functions; the HTTP calls
// are in `players.ts`.

import { endingFor, type Ending } from './battle.ts';
import type { Arena } from './catalog.ts';
import { BOT_ID, validateNickname } from './nickname.ts';

/** The most battles a player's history holds (`GetBattleHistoryEndpoint.Limit`). */
export const HISTORY_LIMIT = 50;

/**
 * The query parameter the result panel hands the battle just over
 * in, so the dashboard waits until the backend has counted it.
 */
export const AFTER_PARAM = 'after';

/** Battles won and lost: the `GET /players/{id}` answer. Abandoned and unjoined ones count for nobody. */
export interface PlayerRecord {
	wins: number;
	losses: number;
}

/** One completed battle in a player's history: the `GET /players/{id}/battles` entry. */
export interface HistoryEntry {
	battleId: string;
	arenaId: string;
	/** ISO 8601, UTC. */
	completedAt: string;
	/** The round it ended in. */
	round: number;
	/** The side left standing, the bot's id included; null when abandoned or unjoined. */
	winnerId: string | null;
	/** Every side, in slot order; the host alone for an open battle nobody joined. */
	participantIds: string[];
}

/** What the dashboard shows of a player. */
export interface Dashboard {
	record: PlayerRecord;
	/** Newest first, at most `HISTORY_LIMIT`. */
	history: HistoryEntry[];
}

/**
 * Reads the player id from the dashboard's address. A nickname that
 * no player could have chosen (the bot's, one too long, one with
 * whitespace around it) is no player id; any other may belong to a
 * player, or to nobody yet: the API cannot tell the two apart.
 *
 * @returns The player id, or null if no player can have it.
 */
export function parsePlayerId(raw: string): string | null {
	const checked = validateNickname(raw);

	return 'nickname' in checked && checked.nickname === raw ? raw : null;
}

/**
 * A player id as the `[playerId]` segment of the dashboard's path.
 * `resolve()` puts a parameter in as it is, so a slash or a question
 * mark in a nickname has to be escaped first.
 */
export function playerParam(playerId: string): string {
	return encodeURIComponent(playerId);
}

/** The query that sends a player from a finished battle to their dashboard: `?after=<battleId>`. */
export function afterQuery(battleId: string): string {
	return `?${AFTER_PARAM}=${encodeURIComponent(battleId)}`;
}

/** The record in numbers: battles decided, and the share won in whole percent (null before the first). */
export function summaryOf(record: PlayerRecord): { decided: number; winRate: number | null } {
	const decided = record.wins + record.losses;

	return { decided, winRate: decided === 0 ? null : Math.round((record.wins / decided) * 100) };
}

/**
 * Whether the dashboard already shows the battle the player just
 * finished. The backend stores a finished battle and counts it in
 * the record after it has told the browser the result (ADR-0003),
 * so a dashboard opened at once can be a moment early: the battle
 * must be in the history, and the record must count every decided
 * battle the history lists (a full history may have pushed some of
 * the counted ones out, so the record can count more).
 *
 * @param afterId The battle just finished; null when there is none to wait for.
 */
export function isSettled(dashboard: Dashboard, playerId: string, afterId: string | null): boolean {
	if (afterId === null) {
		return true;
	}

	if (!dashboard.history.some((entry) => entry.battleId === afterId)) {
		return false;
	}

	const decided = dashboard.history.filter((entry) => {
		const ending = endingOf(entry, playerId);

		return ending === 'won' || ending === 'lost';
	}).length;

	return summaryOf(dashboard.record).decided >= decided;
}

/** One battle of the history as the dashboard lists it. */
export interface HistoryLine {
	battleId: string;
	ending: Ending;
	/** The result in one or two words: Won, Lost, Abandoned, Nobody joined. */
	result: string;
	arenaName: string;
	/** Who the player fought, `vs …`; null when nobody joined. */
	opponent: string | null;
	/** How it ended, in a few words. */
	detail: string;
	completedAt: string;
}

/**
 * One battle of `playerId`'s history, worded from their side.
 *
 * @param arenas The Catalog's arenas, for the arena's name.
 */
export function lineOf(entry: HistoryEntry, playerId: string, arenas: Arena[]): HistoryLine {
	const ending = endingOf(entry, playerId);
	const opponents = entry.participantIds.filter((id) => id !== playerId);

	return {
		battleId: entry.battleId,
		ending,
		...wordsOf(ending, entry.round),
		arenaName: arenas.find((arena) => arena.id === entry.arenaId)?.name ?? entry.arenaId,
		// No player may take the bot's name, so its id tells it apart.
		opponent:
			opponents.length === 0
				? null
				: `vs ${opponents.map((id) => (id === BOT_ID ? 'the bot' : id)).join(', ')}`,
		completedAt: entry.completedAt
	};
}

function endingOf(entry: HistoryEntry, playerId: string): Ending {
	return endingFor([...new Set(entry.participantIds)], entry.winnerId, playerId);
}

function wordsOf(ending: Ending, round: number): { result: string; detail: string } {
	switch (ending) {
		case 'won':
			return { result: 'Won', detail: `Decided in round ${round}` };
		case 'lost':
			return { result: 'Lost', detail: `Decided in round ${round}` };
		case 'abandoned':
			return { result: 'Abandoned', detail: `No move before the deadline in round ${round}` };
		case 'unjoined':
			return { result: 'Nobody joined', detail: 'Closed at its join deadline' };
	}
}

/**
 * When a battle ended, to the minute, in English whatever the
 * browser's language. The server renders the page in UTC and says
 * so; the browser then shows its own time zone. Rendering local time
 * on the server would not match what the browser renders while it
 * hydrates the page.
 *
 * @param timeZone `UTC` on the server; undefined for the browser's own time zone.
 */
export function whenOf(iso: string, timeZone: string | undefined): string {
	const formatted = new Intl.DateTimeFormat('en-GB', {
		day: 'numeric',
		month: 'short',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		timeZone
	}).format(new Date(iso));

	return timeZone === 'UTC' ? `${formatted} UTC` : formatted;
}
