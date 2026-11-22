import type { BattleResponse } from '#lib/api/models/index.ts';
import { createArenaApi } from './api.ts';
import type { Battle, BattleStatus, Move } from './battle.ts';

/** A request the API refused or could not answer. */
export class ArenaApiError extends Error {
	/**
	 * @param message What went wrong, in words fit to show.
	 * @param status The HTTP status, if the API answered at all.
	 */
	constructor(
		message: string,
		readonly status: number | undefined
	) {
		super(message);
		this.name = 'ArenaApiError';
	}
}

/**
 * Starts a battle against the bot.
 *
 * @param arenaId The arena to fight on.
 * @param playerId The visitor's nickname.
 * @param speciesIds The team, in slot order.
 */
export function startBattleAgainstBot(
	arenaId: string,
	playerId: string,
	speciesIds: string[]
): Promise<Battle> {
	return call(async () =>
		toBattle(
			await createArenaApi()
				.arenas.byArenaId(arenaId)
				.battles.post({ playerId, monsterSpeciesIds: speciesIds, opponent: 'Bot' })
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
		return await call(async () =>
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
	return call(async () =>
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

async function call<T>(request: () => Promise<T>): Promise<T> {
	try {
		return await request();
	} catch (error) {
		throw error instanceof ArenaApiError ? error : toApiError(error);
	}
}

/** What Kiota throws for a failed response: its status and, if the API sent one, a problem document. */
interface FailedResponse {
	responseStatusCode?: number;
	detail?: string | null;
	title?: string | null;
	errors?: { additionalData?: Record<string, unknown> } | null;
}

// The API explains a refusal in a problem document: `detail` for a
// broken rule, `errors` per field for an invalid request.
function toApiError(error: unknown): ArenaApiError {
	const failed = (typeof error === 'object' && error !== null ? error : {}) as FailedResponse;
	const fieldErrors = Object.values(failed.errors?.additionalData ?? {}).flat();
	const message =
		failed.detail ??
		fieldErrors.find((entry) => typeof entry === 'string') ??
		failed.title ??
		(failed.responseStatusCode === undefined
			? 'The arena cannot be reached right now.'
			: `The arena answered with status ${failed.responseStatusCode}.`);

	return new ArenaApiError(message, failed.responseStatusCode);
}
