/** The longest player id the API takes (`PlayerIds.MaxLength`). */
export const MAX_NICKNAME_LENGTH = 64;

/** The participant id of the bot; no player may use it. */
export const BOT_ID = 'bot';

/** A nickname that passed, or the reason it did not. */
export type NicknameResult = { nickname: string } | { error: string };

/**
 * Checks a nickname against the API's rules for a player id, so a
 * bad one is caught before a battle is started with it. Surrounding
 * whitespace is dropped rather than rejected.
 *
 * A nickname is also part of an address: the player's dashboard and
 * `GET /players/{playerId}` have it in their path. The API takes a
 * slash in a player id, but its routing leaves an escaped one
 * escaped, so it would never find that player; and a path segment
 * of `.` or `..` is resolved away by every browser. Those are
 * refused here, where the nickname is chosen.
 */
export function validateNickname(input: string): NicknameResult {
	const nickname = input.trim();

	if (nickname === '') {
		return { error: 'Enter a nickname.' };
	}

	if (nickname === BOT_ID) {
		return { error: `'${BOT_ID}' is the bot's name. Pick another one.` };
	}

	if (nickname.length > MAX_NICKNAME_LENGTH) {
		return { error: `A nickname has at most ${MAX_NICKNAME_LENGTH} characters.` };
	}

	if (nickname.includes('/')) {
		return { error: 'A nickname cannot have a slash in it.' };
	}

	if (nickname === '.' || nickname === '..') {
		return { error: 'A nickname needs more than dots.' };
	}

	return { nickname };
}
