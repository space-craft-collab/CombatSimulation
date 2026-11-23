import { ArenaApiError } from './api.ts';
import {
	applyRound,
	newerOf,
	relationOf,
	type Battle,
	type BattleEvent,
	type Difficulty,
	type Move
} from './battle.ts';
import { connectToBattleHub, type BattleHub } from './battle-hub.ts';
import { findBattle, seatBot, submitMove } from './battles.ts';

/** What one round did, for the battle log. */
export interface PlayedRound {
	round: number;
	events: BattleEvent['events'];
}

/** Whether round deltas currently arrive. */
export type Feed = 'connecting' | 'live' | 'offline';

/**
 * One battle as it unfolds: its state, kept current by the hub's
 * round deltas, and the visitor's moves. An open battle is followed
 * from the start: the hub says when an opponent joined.
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
	// The round the visitor's move went in for. Against a human the
	// round waits for the other side's move too.
	#movedInRound = $state<number | null>(null);
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
	 * True once the visitor's move for the current round is in and
	 * the round waits for the other side: it resolves when that side
	 * moved too, or at the turn deadline.
	 */
	get moved(): boolean {
		return this.#battle.status === 'InProgress' && this.#movedInRound === this.#battle.round;
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
			hub.onBattleStarted((started) => this.#onBattleStarted(started));
			hub.onReconnected(() => void this.#resync());
			await this.#resync();
		} catch {
			this.#feed = 'offline';
		}
	}

	/**
	 * Submits the visitor's move for the current round.
	 *
	 * @returns Whether the move was accepted; if not, `failure` says why.
	 */
	async act(participantId: string, move: Move): Promise<boolean> {
		const round = this.#battle.round;
		this.#submitting = true;
		this.#failure = null;

		try {
			const after = await submitMove(this.#battle.battleId, participantId, move);
			this.#movedInRound = round;
			this.#battle = newerOf(this.#battle, after);
			return true;
		} catch (error) {
			if (error instanceof ArenaApiError && error.status === 409) {
				// The round already has this side's move (sent before a
				// reload), or the battle moved on meanwhile: the current
				// state says which.
				this.#movedInRound = round;
				void this.#catchUp();
				return false;
			}

			this.#failure =
				error instanceof ArenaApiError ? error.message : 'The move could not be sent.';
			return false;
		} finally {
			this.#submitting = false;
		}
	}

	/**
	 * Seats the bot in the open seat of a battle that waits for
	 * players ("play vs bot instead"); the battle starts.
	 *
	 * @returns Whether the bot took the seat; if not, `failure` says why.
	 */
	async seatBot(difficulty: Difficulty): Promise<boolean> {
		this.#submitting = true;
		this.#failure = null;

		try {
			this.#battle = newerOf(this.#battle, await seatBot(this.#battle.battleId, difficulty));
			return true;
		} catch (error) {
			if (error instanceof ArenaApiError && error.status === 409) {
				// A human joined first, or the join deadline passed.
				// Nothing to explain: the current state shows either.
				void this.#catchUp();
			} else {
				this.#failure =
					error instanceof ArenaApiError ? error.message : 'The bot could not be brought in.';
			}

			return false;
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

	// After a refusal: the battle's state from the hub, or over HTTP
	// while there is no connection.
	async #catchUp(): Promise<void> {
		if (this.#hub) {
			await this.#resync();
			return;
		}

		try {
			const current = await findBattle(globalThis.fetch, this.#battle.battleId);

			if (current) {
				this.#battle = newerOf(this.#battle, current);
			}
		} catch {
			// The page already says that the live connection is down.
		}
	}

	#onRoundResolved(battleEvent: BattleEvent): void {
		if (battleEvent.battleId !== this.#battle.battleId) {
			return;
		}

		// Round 0 is an open battle nobody joined: nothing was played.
		if (
			battleEvent.round > 0 &&
			!this.#rounds.some((played) => played.round === battleEvent.round)
		) {
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

	// An opponent joined the open battle, a human or the bot. The
	// host's own request for the bot answers with the same state.
	#onBattleStarted(started: Battle): void {
		this.#battle = newerOf(this.#battle, started);
	}
}
