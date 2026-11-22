import { applyRound, newerOf, relationOf, type Battle, type BattleEvent } from './battle.ts';
import { connectToBattleHub, type BattleHub } from './battle-hub.ts';
import { ArenaApiError, submitStrike } from './battles.ts';

/** What one round did, for the battle log. */
export interface PlayedRound {
	round: number;
	events: BattleEvent['events'];
}

/** Whether round deltas currently arrive. */
export type Feed = 'connecting' | 'live' | 'offline';

/**
 * One battle as it unfolds: its state, kept current by the hub's
 * round deltas, and the visitor's moves.
 *
 * A round reaches the browser twice, in no fixed order: as a delta
 * on the hub and, for the visitor's own move, as the full state in
 * the HTTP response. The state takes whichever is further; the log
 * takes each round's events once.
 */
export class LiveBattle {
	#battle = $state<Battle>() as Battle;
	#rounds = $state<PlayedRound[]>([]);
	#feed = $state<Feed>('connecting');
	#submitting = $state(false);
	#failure = $state<string | null>(null);
	#hub: BattleHub | null = null;
	#disposed = false;

	/**
	 * @param initial The state the page was loaded with.
	 */
	constructor(initial: Battle) {
		this.#battle = initial;
	}

	/** The battle's current state. */
	get battle(): Battle {
		return this.#battle;
	}

	/** The rounds seen since the page was opened, oldest first. */
	get rounds(): readonly PlayedRound[] {
		return this.#rounds;
	}

	/** Whether round deltas currently arrive. */
	get feed(): Feed {
		return this.#feed;
	}

	/** True while a move is on its way. */
	get submitting(): boolean {
		return this.#submitting;
	}

	/** Why the last move was refused, if it was. */
	get failure(): string | null {
		return this.#failure;
	}

	/**
	 * Follows the battle on the hub. Browser only. A completed
	 * battle changes no more and is not followed.
	 */
	async follow(): Promise<void> {
		if (this.#battle.status === 'Completed') {
			this.#feed = 'live';
			return;
		}

		try {
			const hub = await connectToBattleHub();

			if (this.#disposed) {
				await hub.stop();
				return;
			}

			this.#hub = hub;
			hub.onRoundResolved((battleEvent) => this.#onRoundResolved(battleEvent));
			hub.onReconnected(() => void this.#resync());
			await this.#resync();
		} catch {
			this.#feed = 'offline';
		}
	}

	/**
	 * Submits the visitor's move for the current round.
	 */
	async strike(participantId: string, actorSlot: number, targetSlot: number): Promise<void> {
		this.#submitting = true;
		this.#failure = null;

		try {
			const after = await submitStrike(this.#battle.battleId, participantId, actorSlot, targetSlot);
			this.#battle = newerOf(this.#battle, after);
		} catch (error) {
			this.#failure =
				error instanceof ArenaApiError ? error.message : 'The move could not be sent.';
		} finally {
			this.#submitting = false;
		}
	}

	/** Stops following the battle. */
	async dispose(): Promise<void> {
		this.#disposed = true;
		await this.#hub?.stop();
	}

	// Watching returns the full state: the starting point, and the
	// way back after missed rounds.
	async #resync(): Promise<void> {
		if (!this.#hub) {
			return;
		}

		try {
			this.#battle = newerOf(this.#battle, await this.#hub.watch(this.#battle.battleId));
			this.#feed = 'live';
		} catch {
			this.#feed = 'offline';
		}
	}

	#onRoundResolved(battleEvent: BattleEvent): void {
		if (battleEvent.battleId !== this.#battle.battleId) {
			return;
		}

		if (!this.#rounds.some((played) => played.round === battleEvent.round)) {
			this.#rounds.push({ round: battleEvent.round, events: battleEvent.events });
		}

		switch (relationOf(this.#battle, battleEvent)) {
			case 'next':
				this.#battle = applyRound(this.#battle, battleEvent);
				break;
			case 'gap':
				void this.#resync();
				break;
			case 'old':
				// The HTTP response of the visitor's move was faster.
				break;
		}
	}
}
