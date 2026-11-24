import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { RoundEvent } from './battle';
import type { PlayedRound } from './live-battle.svelte';
import { Playback, STEP_MS } from './playback.svelte';

function hit(actorSlot: number, targetSlot: number): RoundEvent {
	return {
		kind: 'Ability',
		actorSlot,
		targetSlot,
		abilityId: 'strike',
		damage: 10,
		healing: 0,
		targetHitPoints: 30,
		effectiveness: 'Normal',
		effect: 'None'
	};
}

function played(round: number, events: RoundEvent[] = [hit(0, 3), hit(3, 0)]): PlayedRound {
	return { round, events };
}

describe('Playback', () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it('shows a round event by event, then stops playing', () => {
		const playback = new Playback(() => false);

		playback.play(played(1));
		expect([playback.step, playback.playing]).toEqual([1, true]);

		vi.advanceTimersByTime(STEP_MS);
		expect([playback.step, playback.playing]).toEqual([2, true]);

		vi.advanceTimersByTime(STEP_MS);
		expect([playback.step, playback.playing]).toEqual([2, false]);
	});

	it('cuts a round short for the next one', () => {
		const playback = new Playback(() => false);
		playback.play(played(1));

		playback.play(played(2));

		expect(playback.round?.round).toBe(2);
		expect([playback.step, playback.playing]).toEqual([1, true]);
		vi.advanceTimersByTime(STEP_MS);
		expect(playback.step).toBe(2);
	});

	it('does not play a round again', () => {
		const playback = new Playback(() => false);
		playback.play(played(1));
		vi.advanceTimersByTime(STEP_MS);

		playback.play(played(1));

		expect(playback.step).toBe(2);
	});

	it('shows a round played at once with reduced motion', () => {
		const playback = new Playback(() => true);

		playback.play(played(1));

		expect([playback.step, playback.playing]).toEqual([2, false]);
	});

	it('has nothing to play in an empty round', () => {
		const playback = new Playback(() => false);

		playback.play(played(1, []));

		expect([playback.step, playback.playing]).toEqual([0, false]);
	});

	it('forgets the round and its timer when cleared', () => {
		const playback = new Playback(() => false);
		playback.play(played(1));

		playback.clear();
		vi.advanceTimersByTime(STEP_MS * 3);

		expect([playback.round, playback.step, playback.playing]).toEqual([null, 0, false]);
	});
});
