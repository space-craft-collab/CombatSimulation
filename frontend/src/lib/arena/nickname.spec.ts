import { describe, expect, it } from 'vitest';
import { MAX_NICKNAME_LENGTH, validateNickname } from './nickname';

describe('validateNickname', () => {
	it('accepts a plain name', () => {
		expect(validateNickname('ada')).toEqual({ nickname: 'ada' });
	});

	it('drops surrounding whitespace', () => {
		expect(validateNickname('  Ada Lovelace \n')).toEqual({ nickname: 'Ada Lovelace' });
	});

	it.each(['', '   '])('rejects the empty name %j', (input) => {
		expect(validateNickname(input)).toHaveProperty('error');
	});

	it("rejects the bot's name", () => {
		expect(validateNickname('bot')).toHaveProperty('error');
	});

	it('matches names exactly, so another casing of the bot name passes', () => {
		expect(validateNickname('Bot')).toEqual({ nickname: 'Bot' });
	});

	it('accepts the longest name and rejects one character more', () => {
		expect(validateNickname('a'.repeat(MAX_NICKNAME_LENGTH))).toHaveProperty('nickname');
		expect(validateNickname('a'.repeat(MAX_NICKNAME_LENGTH + 1))).toHaveProperty('error');
	});
});
