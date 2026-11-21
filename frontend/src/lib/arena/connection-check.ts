import { createArenaApi } from './api';
import { connectToBattleHub, type BattleEvent } from './battle-hub';

/** The player the check plays as; its battles are abandoned after one round. */
const PLAYER_ID = 'connection-check';

/** How long the check waits for the hub to report the round. */
const ROUND_TIMEOUT_MS = 10_000;

/**
 * Proves the three ways the browser talks to the AppHost: it starts
 * a battle against the bot over HTTP, follows it on the SignalR hub,
 * submits one move and waits for the hub to report the round.
 * Browser only.
 *
 * @param arenaId The arena to fight on.
 * @param speciesId The one species to field.
 * @param report Called with a line per finished step.
 */
export async function runConnectionCheck(
	arenaId: string,
	speciesId: string,
	report: (step: string) => void
): Promise<void> {
	const api = createArenaApi();

	const battle = await api.arenas.byArenaId(arenaId).battles.post({
		playerId: PLAYER_ID,
		monsterSpeciesIds: [speciesId]
	});

	if (!battle?.battleId) {
		throw new Error('The API started no battle.');
	}

	report(`HTTP: started battle ${battle.battleId} against the bot`);

	const hub = await connectToBattleHub();

	try {
		const snapshot = await hub.watch(String(battle.battleId));
		const mine = snapshot.combatants.find((combatant) => !combatant.isBot);
		const theirs = snapshot.combatants.find((combatant) => combatant.isBot);

		if (!mine || !theirs) {
			throw new Error('The battle lacks a side.');
		}

		report(`SignalR: watching, ${snapshot.combatants.length} monsters on the field`);

		const resolved = new Promise<BattleEvent>((resolve, reject) => {
			hub.onRoundResolved(resolve);
			setTimeout(() => reject(new Error('The hub reported no round in time.')), ROUND_TIMEOUT_MS);
		});

		await api.battles.byBattleId(battle.battleId).actions.post({
			participantId: PLAYER_ID,
			actorSlot: mine.slot,
			targetSlot: theirs.slot
		});

		const round = await resolved;

		report(`SignalR: round ${round.round} resolved with ${round.strikes.length} strikes`);
	} finally {
		await hub.stop();
	}
}
