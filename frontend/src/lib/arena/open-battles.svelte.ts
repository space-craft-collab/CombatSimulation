import { listOpenBattles } from './battles.ts';
import { LOBBY_REFRESH_MS, type OpenBattle } from './lobby.ts';

/**
 * The battles of one arena that wait for a second player, kept
 * current while the picker is open. The API has no push for the
 * list, so it is read again every few seconds while the page is
 * visible, and at once when it becomes visible again.
 */
export class OpenBattleList {
	#battles = $state<OpenBattle[]>([]);
	#arenaId: string;
	// Answers can overtake each other; only the latest request's counts.
	#latest = 0;

	/**
	 * @param arenaId The arena whose list this is.
	 * @param initial The list the page was loaded with.
	 */
	constructor(arenaId: string, initial: OpenBattle[]) {
		this.#arenaId = arenaId;
		this.#battles = initial;
	}

	/** The open battles as last read. */
	get battles(): readonly OpenBattle[] {
		return this.#battles;
	}

	/** Reads the list again. A failed read keeps the last list; the next one tries again. */
	async refresh(): Promise<void> {
		const request = ++this.#latest;

		try {
			const battles = await listOpenBattles(this.#arenaId);

			if (request === this.#latest) {
				this.#battles = battles;
			}
		} catch {
			// See above: an arena that cannot be reached right now
			// shows the battles it listed last.
		}
	}

	/**
	 * Keeps the list current. Browser only.
	 *
	 * @returns Stops it again.
	 */
	poll(intervalMs: number = LOBBY_REFRESH_MS): () => void {
		const tick = () => {
			if (document.visibilityState === 'visible') {
				void this.refresh();
			}
		};
		const timer = setInterval(tick, intervalMs);
		document.addEventListener('visibilitychange', tick);

		return () => {
			clearInterval(timer);
			document.removeEventListener('visibilitychange', tick);
		};
	}
}
