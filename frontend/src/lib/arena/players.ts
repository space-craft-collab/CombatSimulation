import type { BattleHistoryEntryResponse } from '#lib/api/models/index.ts';
import type { WithPlayerItemRequestBuilder } from '#lib/api/players/item/index.ts';
import { ArenaApiError, callApi, createArenaApi } from './api.ts';
import { isSettled, type Dashboard, type HistoryEntry, type PlayerRecord } from './dashboard.ts';

/** How often the dashboard reads again while a battle just over is not counted yet. */
const SETTLE_ATTEMPTS = 10;

/** The pause between two of those reads. */
const SETTLE_DELAY_MS = 200;

/** A player's dashboard, and whether it already counts the battle it was opened after. */
export interface LoadedDashboard extends Dashboard {
	/** False if the battle just over was still not counted when the dashboard gave up waiting. */
	settled: boolean;
}

/**
 * Reads a player's record and their last battles.
 *
 * A player is known to the API only once a battle of theirs was
 * decided: before that the record is all zero here, not an error.
 *
 * @param fetch The fetch of the calling `load` function.
 * @param afterId The battle the player just finished, if any: it is
 * waited for, briefly, since the backend counts it a moment after
 * it told the browser the result.
 */
export async function loadDashboard(
	fetch: typeof globalThis.fetch,
	playerId: string,
	afterId: string | null
): Promise<LoadedDashboard> {
	for (let attempt = 1; ; attempt++) {
		const dashboard = await readDashboard(fetch, playerId);
		const settled = isSettled(dashboard, playerId, afterId);

		if (settled || attempt === SETTLE_ATTEMPTS) {
			return { ...dashboard, settled };
		}

		await new Promise((done) => setTimeout(done, SETTLE_DELAY_MS));
	}
}

async function readDashboard(fetch: typeof globalThis.fetch, playerId: string): Promise<Dashboard> {
	const player = createArenaApi(fetch).players.byPlayerId(playerId);
	const [record, history] = await Promise.all([
		findRecord(player),
		callApi(async () => (await player.battles.get()) ?? [])
	]);

	return { record, history: history.flatMap(toEntry) };
}

async function findRecord(player: WithPlayerItemRequestBuilder): Promise<PlayerRecord> {
	try {
		const record = await callApi(() => player.get());

		return { wins: record?.wins ?? 0, losses: record?.losses ?? 0 };
	} catch (error) {
		// The stats know a player from their first decided battle on.
		if (error instanceof ArenaApiError && error.status === 404) {
			return { wins: 0, losses: 0 };
		}

		throw error;
	}
}

// Kiota types every property as optional; an entry without its id
// or its end is skipped, the rest gets a harmless default.
function toEntry(response: BattleHistoryEntryResponse): HistoryEntry[] {
	if (!response.battleId || !response.completedAt) {
		return [];
	}

	return [
		{
			battleId: response.battleId,
			arenaId: response.arenaId ?? '',
			completedAt: response.completedAt.toISOString(),
			round: response.round ?? 0,
			winnerId: response.winnerId ?? null,
			participantIds: response.participantIds ?? []
		}
	];
}
