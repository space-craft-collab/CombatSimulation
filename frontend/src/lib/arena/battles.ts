import type { BattleResponse } from '#lib/api/models/index.ts';
import { ArenaApiError, callApi, createArenaApi } from './api.ts';
import type { Battle, BattleStatus, Difficulty, Move } from './battle.ts';
import { openBattlesOf, type OpenBattle } from './lobby.ts';

/**
 * Starts a battle against the bot.
 *
 * @param arenaId The arena to fight on.
 * @param playerId The visitor's nickname.
 * @param speciesIds The team, in slot order.
 * @param difficulty How hard the bot is; Easy weakens its monsters.
 */
export function startBattleAgainstBot(
	arenaId: string,
	playerId: string,
	speciesIds: string[],
	difficulty: Difficulty = 'Normal'
): Promise<Battle> {
	return callApi(async () =>
		toBattle(
			await createArenaApi()
				.arenas.byArenaId(arenaId)
				.battles.post({ playerId, monsterSpeciesIds: speciesIds, opponent: 'Bot', difficulty })
		)
	);
}

/**
 * Opens a battle that waits for a human opponent (ADR-0007). It is
 * listed by its arena until someone joins, the bot takes the open
 * seat, or its join deadline passes.
 *
 * @param arenaId The arena to fight on.
 * @param playerId The visitor's nickname.
 * @param speciesIds The team, in slot order.
 * @returns The battle, waiting for players.
 */
export function openBattle(
	arenaId: string,
	playerId: string,
	speciesIds: string[]
): Promise<Battle> {
	return callApi(async () =>
		toBattle(
			await createArenaApi()
				.arenas.byArenaId(arenaId)
				.battles.post({ playerId, monsterSpeciesIds: speciesIds, opponent: 'Human' })
		)
	);
}

/**
 * Reads the battles of an arena that wait for a second player.
 *
 * @param fetch The fetch of the calling `load` function, if any.
 */
export function listOpenBattles(
	arenaId: string,
	fetch: typeof globalThis.fetch = globalThis.fetch
): Promise<OpenBattle[]> {
	return callApi(async () => {
		const summaries = (await createArenaApi(fetch).arenas.byArenaId(arenaId).battles.get()) ?? [];

		// Kiota types every property as optional; an entry without
		// its id or state is skipped.
		return openBattlesOf(
			summaries.flatMap((summary) =>
				summary.battleId && summary.status
					? [
							{
								battleId: summary.battleId,
								status: summary.status satisfies BattleStatus,
								participantIds: summary.participantIds ?? []
							}
						]
					: []
			)
		);
	});
}

/**
 * Joins an open battle as its second player; the battle starts.
 *
 * @param playerId The visitor's nickname; it must not be the host's.
 * @param speciesIds The team, in slot order.
 * @returns The battle's first state. A battle that no longer waits
 * is refused with status 409.
 */
export function joinBattle(
	battleId: string,
	playerId: string,
	speciesIds: string[]
): Promise<Battle> {
	return callApi(async () =>
		toBattle(
			await createArenaApi()
				.battles.byBattleId(battleId)
				.participants.post({ playerId, monsterSpeciesIds: speciesIds })
		)
	);
}

/**
 * Seats the bot in an open battle ("play vs bot instead"); the
 * battle starts.
 *
 * @param difficulty How hard the bot is; Easy weakens its monsters.
 * @returns The battle's first state. A battle that no longer waits
 * is refused with status 409.
 */
export function seatBot(battleId: string, difficulty: Difficulty = 'Normal'): Promise<Battle> {
	return callApi(async () =>
		toBattle(
			await createArenaApi()
				.battles.byBattleId(battleId)
				.bot.post({ queryParameters: { difficulty } })
		)
	);
}

/**
 * Reads a battle, live or completed.
 *
 * @param fetch The fetch of the calling `load` function.
 * @returns The battle, or null if there is none with this id.
 */
export async function findBattle(
	fetch: typeof globalThis.fetch,
	battleId: string
): Promise<Battle | null> {
	try {
		return await callApi(async () =>
			toBattle(await createArenaApi(fetch).battles.byBattleId(battleId).get())
		);
	} catch (error) {
		if (error instanceof ArenaApiError && error.status === 404) {
			return null;
		}

		throw error;
	}
}

/**
 * Submits a participant's move for the current round: one of its
 * monsters uses one of its abilities on a target.
 *
 * @returns The battle after the move; against the bot the round is played by then.
 */
export function submitMove(battleId: string, participantId: string, move: Move): Promise<Battle> {
	return callApi(async () =>
		toBattle(
			await createArenaApi()
				.battles.byBattleId(battleId)
				.actions.post({ participantId, ...move })
		)
	);
}

// Kiota types every property as optional; a battle without its id
// or state is no answer at all, the rest gets a harmless default.
function toBattle(response: BattleResponse | undefined): Battle {
	if (!response?.battleId || !response.status) {
		throw new ArenaApiError('The API answered without a battle.', undefined);
	}

	return {
		battleId: response.battleId,
		arenaId: response.arenaId ?? '',
		status: response.status satisfies BattleStatus,
		round: response.round ?? 0,
		winnerId: response.winnerId ?? null,
		turnDeadline: response.turnDeadline?.toISOString() ?? null,
		// A battle from an API without difficulties was a Normal one.
		difficulty: (response.difficulty ?? 'Normal') satisfies Difficulty,
		combatants: (response.combatants ?? []).map((combatant) => ({
			slot: combatant.slot ?? 0,
			participantId: combatant.participantId ?? '',
			isBot: combatant.isBot ?? false,
			speciesId: combatant.speciesId ?? '',
			hitPoints: combatant.hitPoints ?? 0,
			maxHitPoints: combatant.maxHitPoints ?? 0,
			abilities: (combatant.abilities ?? []).map((ability) => ({
				abilityId: ability.abilityId ?? '',
				roundsLeft: ability.roundsLeft ?? 0
			})),
			effects: (combatant.effects ?? []).flatMap((effect) =>
				effect.kind
					? [{ kind: effect.kind, value: effect.value ?? 0, roundsLeft: effect.roundsLeft ?? 0 }]
					: []
			)
		}))
	};
}
