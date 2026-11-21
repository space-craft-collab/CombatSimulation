import { browser } from '$app/env';
import { validateNickname } from './nickname.ts';

const STORAGE_KEY = 'arena.nickname';

/**
 * Who the visitor plays as: a nickname kept in `localStorage`
 * (ADR-0004). It is the player id the API takes; there are no
 * accounts before Phase 6.
 *
 * The server never knows it. There this stays empty, and it is
 * never written, since a module's state is shared by all requests.
 */
class Player {
	#nickname = $state<string | null>(null);
	#loaded = $state(false);

	/** The chosen nickname; null on the server and before one is chosen. */
	get nickname(): string | null {
		return this.#nickname;
	}

	/** True once the browser has looked for a stored nickname. */
	get loaded(): boolean {
		return this.#loaded;
	}

	/** Reads the stored nickname. Browser only; call it once the app is mounted. */
	load(): void {
		if (!browser || this.#loaded) {
			return;
		}

		const stored = validateNickname(readStored() ?? '');
		this.#nickname = 'nickname' in stored ? stored.nickname : null;
		this.#loaded = true;
	}

	/** Plays as `nickname` from now on; it must have passed `validateNickname`. */
	choose(nickname: string): void {
		this.#nickname = nickname;
		writeStored(nickname);
	}

	/** Forgets the nickname, so another one can be chosen. */
	forget(): void {
		this.#nickname = null;
		writeStored(null);
	}
}

// Storage can be unavailable (private mode, blocked by a policy).
// The nickname then lasts for this visit only.
function readStored(): string | null {
	try {
		return localStorage.getItem(STORAGE_KEY);
	} catch {
		return null;
	}
}

function writeStored(nickname: string | null): void {
	try {
		if (nickname === null) {
			localStorage.removeItem(STORAGE_KEY);
		} else {
			localStorage.setItem(STORAGE_KEY, nickname);
		}
	} catch {
		// See above: the choice still holds in memory.
	}
}

export const player = new Player();
