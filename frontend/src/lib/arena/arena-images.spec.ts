import { describe, expect, it } from 'vitest';
import { arenaImageOf } from './arena-images';

describe('arenaImageOf', () => {
	it.each(['volcano', 'glacier', 'moss-grove'])('gives the arena %s its picture', (arenaId) => {
		expect(arenaImageOf(arenaId)).toMatch(/\.webp/);
	});

	it('gives each arena a picture of its own', () => {
		const pictures = ['volcano', 'glacier', 'moss-grove'].map(arenaImageOf);

		expect(new Set(pictures).size).toBe(3);
	});

	it('leaves an arena without a picture plain', () => {
		expect(arenaImageOf('no-such-arena')).toBeUndefined();
	});

	it('does not take an inherited property for a picture', () => {
		expect(arenaImageOf('toString')).toBeUndefined();
	});
});
