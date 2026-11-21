import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr';
import { PUBLIC_API_URL } from '$app/env/public';

// The hub's messages, written by hand: Kiota generates the HTTP
// client only. They mirror `Battles/Features/WatchBattle` and
// `Battles/Features/GetBattle` in the backend.

export type BattleStatus = 'Created' | 'WaitingForPlayers' | 'InProgress' | 'Completed';

/** One monster on the field. */
export interface Combatant {
	slot: number;
	participantId: string;
	isBot: boolean;
	speciesId: string;
	hitPoints: number;
	maxHitPoints: number;
}

/** A battle's full state: what `Watch` returns and `BattleStarted` carries. */
export interface BattleSnapshot {
	battleId: string;
	arenaId: string;
	status: BattleStatus;
	round: number;
	winnerId: string | null;
	/** ISO 8601, or null when no turn is pending. */
	turnDeadline: string | null;
	combatants: Combatant[];
}

/** One hit within a round. */
export interface Strike {
	actorSlot: number;
	targetSlot: number;
	abilityId: string;
	damage: number;
	targetHitPoints: number;
}

/** What one resolved round changed: the `RoundResolved` message. */
export interface BattleEvent {
	battleId: string;
	round: number;
	status: BattleStatus;
	winnerId: string | null;
	turnDeadline: string | null;
	strikes: Strike[];
}

/** The hub's route on the AppHost. */
const HUB_PATH = '/hubs/battles';

/** A connection to the battle hub, typed with the hub's contract. */
export interface BattleHub {
	/** Starts following a battle and returns its current state. */
	watch(battleId: string): Promise<BattleSnapshot>;
	/** Stops following a battle. */
	unwatch(battleId: string): Promise<void>;
	/** Calls `handler` after every resolved round of a watched battle. */
	onRoundResolved(handler: (battleEvent: BattleEvent) => void): void;
	/** Calls `handler` when a second player joined a watched open battle. */
	onBattleStarted(handler: (battle: BattleSnapshot) => void): void;
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
		watch: (battleId) => connection.invoke<BattleSnapshot>('Watch', battleId),
		unwatch: (battleId) => connection.invoke('Unwatch', battleId),
		onRoundResolved: (handler) => connection.on('RoundResolved', handler),
		onBattleStarted: (handler) => connection.on('BattleStarted', handler),
		stop: () => connection.stop()
	};
}
