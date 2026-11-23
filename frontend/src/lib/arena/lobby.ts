// The open battles of an arena as the team picker offers them: the
// ones a visitor can join, and the ones they opened themselves.
// Pure functions; the HTTP calls are in `battles.ts`.

import type { BattleStatus } from './battle.ts';

/** How often the team picker reads the arena's open battles again; the API has no push for them. */
export const LOBBY_REFRESH_MS = 4000;

/** The query parameter a waiting battle's page hands its id to the picker in. */
export const JOIN_PARAM = 'join';

/** One battle in an arena's list: the `GET /arenas/{arenaId}/battles` entry. */
export interface BattleSummary {
	battleId: string;
	status: BattleStatus;
	/** The sides that have joined so far, the host first. */
	participantIds: string[];
}

/** A battle that waits for a second player. */
export interface OpenBattle {
	battleId: string;
	/** The player who opened it and waits. */
	hostId: string;
}

/** The open battles of an arena, seen by one visitor. */
export interface Lobby {
	/** The visitor's own open battles: to go back to, not to join. */
	own: OpenBattle[];
	/** The battles the visitor can join; the one they were sent to first. */
	joinable: OpenBattle[];
}

/**
 * The battles of an arena's list that wait for a second player.
 * The list also holds the running ones, which nobody can join.
 */
export function openBattlesOf(summaries: readonly BattleSummary[]): OpenBattle[] {
	return summaries.flatMap((summary) =>
		summary.status === 'WaitingForPlayers' && summary.participantIds.length > 0
			? [{ battleId: summary.battleId, hostId: summary.participantIds[0] }]
			: []
	);
}

/**
 * Splits the open battles into the visitor's own and the ones they
 * can join: the API refuses a player who already takes part.
 *
 * @param nickname The visitor's player id; null before one is chosen,
 * when every battle counts as joinable.
 * @param invitedId The battle the visitor was sent to from its page,
 * listed first if it is still open.
 */
export function lobbyOf(
	open: readonly OpenBattle[],
	nickname: string | null,
	invitedId: string | null = null
): Lobby {
	const own = open.filter((battle) => battle.hostId === nickname);
	const joinable = open
		.filter((battle) => battle.hostId !== nickname)
		.toSorted((a, b) => Number(b.battleId === invitedId) - Number(a.battleId === invitedId));

	return { own, joinable };
}

/**
 * The query that sends a visitor from a waiting battle's page to the
 * picker, so that battle is listed first: `?join=<battleId>`.
 */
export function inviteQuery(battleId: string): string {
	return `?${JOIN_PARAM}=${encodeURIComponent(battleId)}`;
}

/**
 * Why joining an open battle failed, in words fit to show.
 *
 * @param status The HTTP status of the refusal, if the API answered.
 * @param message What the API said.
 */
export function joinRefusal(status: number | undefined, message: string): string {
	switch (status) {
		case 409:
			// The battle no longer waits: someone else was faster, or
			// its join deadline passed.
			return 'That battle is no longer open: someone joined first, or it closed. Pick another one.';
		case 404:
			return 'That battle does not exist any more.';
		default:
			return message;
	}
}
