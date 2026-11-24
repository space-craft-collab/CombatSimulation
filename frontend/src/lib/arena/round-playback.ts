// A round played back on the stage one event after another. Pure:
// what the stage shows after any number of a round's events follows
// from the events and the state after the round alone.
//
// An event's damage and healing are the actual change of its
// target's hit points: the backend caps damage at the hit points
// left and healing at the ones missing. So the hit points before an
// event are its `targetHitPoints` plus its damage minus its healing.

import type { Combatant, RoundEvent } from './battle.ts';

/**
 * What a monster does or suffers in one step of a playback; step `k`
 * plays the round's `k`-th event.
 * - `lunge`: it acts on another monster;
 * - `struck`: another monster acts on it;
 * - `pulse`: something it does to itself (a guard, a heal) or its burn.
 */
export type Cue =
	| { kind: 'lunge'; step: number; targetSlot: number }
	| { kind: 'struck'; step: number; damage: number }
	| { kind: 'pulse'; step: number; damage: number; healing: number };

/**
 * The hit points a monster shows once the first `step` events of a
 * round are played: step 0 is the round's start, `events.length` its
 * end.
 *
 * @param combatant The monster as it stands after the round.
 */
export function hitPointsAt(
	events: readonly RoundEvent[],
	step: number,
	combatant: Combatant
): number {
	const played = events.slice(0, step).findLast((event) => event.targetSlot === combatant.slot);

	if (played) {
		return played.targetHitPoints;
	}

	const coming = events.slice(step).find((event) => event.targetSlot === combatant.slot);

	return coming ? coming.targetHitPoints + coming.damage - coming.healing : combatant.hitPoints;
}

/**
 * The latest cue a monster got in the first `step` events of a
 * round, if any. The stage keeps drawing it until the next one, so a
 * damage number floats on after its step.
 */
export function cueOf(events: readonly RoundEvent[], step: number, slot: number): Cue | null {
	for (let index = Math.min(step, events.length) - 1; index >= 0; index--) {
		const cue = cueIn(events[index], index + 1, slot);

		if (cue) {
			return cue;
		}
	}

	return null;
}

function cueIn(event: RoundEvent, step: number, slot: number): Cue | null {
	// A burn is the burning monster's own: no one steps up to it.
	const onItself = event.kind === 'Burn' || event.actorSlot === event.targetSlot;

	if (event.targetSlot === slot) {
		return onItself
			? { kind: 'pulse', step, damage: event.damage, healing: event.healing }
			: { kind: 'struck', step, damage: event.damage };
	}

	return event.actorSlot === slot && !onItself
		? { kind: 'lunge', step, targetSlot: event.targetSlot }
		: null;
}
