import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr';
import { PUBLIC_API_URL } from '$app/env/public';
import type { Battle, BattleEvent } from './battle.ts';

// The hub's contract, written by hand: Kiota generates the HTTP
// client only. Method and message names mirror `BattleHub` in the
// backend; the payload types are in `battle.ts`.

/** The hub's route on the AppHost. */
const HUB_PATH = '/hubs/battles';

/** A connection to the battle hub. */
export interface BattleHub {
	/** Starts following a battle and returns its current state. */
	watch(battleId: string): Promise<Battle>;
	/** Calls `handler` after every resolved round of a watched battle. */
	onRoundResolved(handler: (battleEvent: BattleEvent) => void): void;
	/** Calls `handler` when a second player joined a watched open battle. */
	onBattleStarted(handler: (battle: Battle) => void): void;
	/**
	 * Calls `handler` when a lost connection is back. The hub has
	 * forgotten what was watched, and rounds may have been missed.
	 */
	onReconnected(handler: () => void): void;
	/** Closes the connection. */
	stop(): Promise<void>;
}

/**
 * Opens a connection to the battle hub. Browser only.
 */
export async function connectToBattleHub(): Promise<BattleHub> {
	const connection: HubConnection = new HubConnectionBuilder()
		// The AppHost's CORS policy admits this origin without
		// credentials; nothing is authenticated by a cookie.
		.withUrl(PUBLIC_API_URL + HUB_PATH, { withCredentials: false })
		.withAutomaticReconnect()
		.build();

	await connection.start();

	return {
		watch: (battleId) => connection.invoke<Battle>('Watch', battleId),
		onRoundResolved: (handler) => connection.on('RoundResolved', handler),
		onBattleStarted: (handler) => connection.on('BattleStarted', handler),
		onReconnected: (handler) => connection.onreconnected(handler),
		stop: () => connection.stop()
	};
}
