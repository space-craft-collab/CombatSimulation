import { describe, expect, it } from 'vitest';
import { toInternalUrl } from './internal-url';

const PUBLIC = 'https://api.example.com';
const INTERNAL = 'http://apphost:8080';

describe('toInternalUrl', () => {
	it('rewrites a URL below the public base onto the internal base', () => {
		expect(toInternalUrl(`${PUBLIC}/arenas/volcano/battles?x=1`, PUBLIC, INTERNAL)).toBe(
			`${INTERNAL}/arenas/volcano/battles?x=1`
		);
	});

	it('rewrites the public base itself', () => {
		expect(toInternalUrl(PUBLIC, PUBLIC, INTERNAL)).toBe(INTERNAL);
	});

	it('leaves a URL of another host alone', () => {
		expect(toInternalUrl('https://elsewhere.example/arenas', PUBLIC, INTERNAL)).toBe(
			'https://elsewhere.example/arenas'
		);
	});

	it('leaves a host that merely starts with the public base alone', () => {
		expect(toInternalUrl('https://api.example.com.evil.test/arenas', PUBLIC, INTERNAL)).toBe(
			'https://api.example.com.evil.test/arenas'
		);
	});

	it.each([undefined, '', PUBLIC])('changes nothing when the internal base is %j', (internal) => {
		expect(toInternalUrl(`${PUBLIC}/species`, PUBLIC, internal)).toBe(`${PUBLIC}/species`);
	});
});
