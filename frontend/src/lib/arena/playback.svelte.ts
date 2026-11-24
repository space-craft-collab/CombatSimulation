import type { PlayedRound } from './live-battle.svelte.ts';

/** How long the stage shows each event of a round, in milliseconds. */
export const STEP_MS = 500;

/**
 * The timing of a round played back on the stage: which of its
 * events the stage has shown so far. What each step shows is worked
 * out in `round-playback.ts`.
 *
 * A round that comes in while another plays cuts that one short.
 * With reduced motion asked for, a round is shown played at once.
 */
export class Playback {
	#round = $state<PlayedRound | null>(null);
	#step = $state(0);
	#playing = $state(false);
	#timer: ReturnType<typeof setTimeout> | undefined;
	readonly #reducedMotion: () => boolean;

	/**
	 * @param reducedMotion Whether the visitor asked for less motion;
	 *   the browser's setting by default.
	 */
	constructor(reducedMotion: () => boolean = prefersReducedMotion) {
		this.#reducedMotion = reducedMotion;
	}

	/** The round played last, or being played; null before any. */
	get round(): PlayedRound | null {
		return this.#round;
	}

	/** How many of the round's events the stage has shown. */
	get step(): number {
		return this.#step;
	}

	/** True while the round plays: the stage shows less of it than the battle's state. */
	get playing(): boolean {
		return this.#playing;
	}

	/** Plays a round, from its first event on; a round already played is not played again. */
	play(round: PlayedRound): void {
		if (round.round === this.#round?.round) {
			return;
		}

		this.#stop();
		this.#round = round;

		if (this.#reducedMotion() || round.events.length === 0) {
			this.#step = round.events.length;
			return;
		}

		this.#step = 1;
		this.#playing = true;
		this.#schedule();
	}

	/** Stops playing and forgets the round: the stage shows the battle as it is. */
	clear(): void {
		this.#stop();
		this.#round = null;
		this.#step = 0;
	}

	#stop(): void {
		clearTimeout(this.#timer);
		this.#timer = undefined;
		this.#playing = false;
	}

	// The last step is shown as long as the others before playing ends.
	#schedule(): void {
		this.#timer = setTimeout(() => {
			if (this.#step < (this.#round?.events.length ?? 0)) {
				this.#step++;
				this.#schedule();
			} else {
				this.#stop();
			}
		}, STEP_MS);
	}
}

function prefersReducedMotion(): boolean {
	return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}
