// What the battle view and the team picker say about abilities and
// elements, and how a move is put together. Pure functions over the
// Catalog's abilities and a battle's monsters (ADR-0012).

import type {
	AbilityCooldown,
	ActiveEffect,
	Combatant,
	Effectiveness,
	Move,
	StatusEffect
} from './battle.ts';
import type { Ability, Element } from './catalog.ts';

/** The basic attack every species knows; what the API assumes without an ability. */
export const STRIKE = 'strike';

/** The element each element beats. */
const BEATS: Record<Element, Element | null> = {
	Fire: 'Nature',
	Nature: 'Frost',
	Frost: 'Fire',
	Neutral: null
};

/**
 * How a hit of `attacking` stands against a monster of `defending`:
 * strong (×1.5) with the advantage, weak (×0.75) against it.
 */
export function effectivenessOf(attacking: Element, defending: Element): Effectiveness {
	if (BEATS[attacking] === defending) {
		return 'Strong';
	}

	return BEATS[defending] === attacking ? 'Weak' : 'Normal';
}

/** The element matchup of a move, worth a word before it is sent. */
export interface MatchupHint {
	effectiveness: 'Strong' | 'Weak';
	/** "strong against Nature", "weak against Frost". */
	text: string;
}

/**
 * The hint shown before a move is sent; null when the element does
 * not matter, also for an ability that deals no damage.
 */
export function matchupHint(ability: Ability, targetElement: Element): MatchupHint | null {
	if (ability.power === 0 || ability.targetsSelf) {
		return null;
	}

	switch (effectivenessOf(ability.element, targetElement)) {
		case 'Strong':
			return { effectiveness: 'Strong', text: `strong against ${targetElement}` };
		case 'Weak':
			return { effectiveness: 'Weak', text: `weak against ${targetElement}` };
		case 'Normal':
			return null;
	}
}

/**
 * One line on what an ability does: "150 % · Fire",
 * "80 % · Fire · burn 4 for 3 rounds", "Guard 50 % for 3 rounds",
 * "Heal 30 %".
 */
export function summaryOf(ability: Ability): string {
	const parts: string[] = [];

	if (ability.power > 0) {
		parts.push(`${ability.power} % · ${ability.element}`);
	}

	const effect = effectText(ability);

	if (effect) {
		parts.push(effect);
	}

	const summary = parts.join(' · ');

	return summary.charAt(0).toUpperCase() + summary.slice(1);
}

function effectText(ability: Ability): string | null {
	const lasting = `for ${rounds(ability.effectRounds)}`;

	switch (ability.effect) {
		case 'Burn':
			return `burn ${ability.effectValue} ${lasting}`;
		case 'Guard':
			return `guard ${ability.effectValue} % ${lasting}`;
		case 'Slow':
			return `slow ${ability.effectValue} % ${lasting}`;
		case 'Heal':
			return `heal ${ability.effectValue} %`;
		case 'None':
			return null;
	}
}

function rounds(count: number): string {
	return count === 1 ? '1 round' : `${count} rounds`;
}

/** "cooldown 2"; null for an ability that is ready every round. */
export function cooldownText(ability: Ability): string | null {
	return ability.cooldown > 0 ? `cooldown ${ability.cooldown}` : null;
}

/** Whether the monster can use the ability this round. */
export function isReady(cooldown: AbilityCooldown): boolean {
	return cooldown.roundsLeft === 0;
}

/** "Ready" or "ready in 2". */
export function readinessText(cooldown: AbilityCooldown): string {
	return isReady(cooldown) ? 'Ready' : `ready in ${cooldown.roundsLeft}`;
}

/**
 * The ability the move uses: the one the viewer picked while the
 * monster knows it and has it ready, otherwise the monster's first
 * ready one (the basic `strike` comes first and is always ready).
 * Undefined only for a monster without a ready ability.
 */
export function chooseAbility(combatant: Combatant, picked: string | null): string | undefined {
	const ready = combatant.abilities.filter(isReady);

	return (ready.find((entry) => entry.abilityId === picked) ?? ready[0])?.abilityId;
}

/** Whether the ability needs a target picked; one used on oneself does not. */
export function needsTarget(ability: Ability): boolean {
	return !ability.targetsSelf;
}

/**
 * The move to send, or null while a target is still missing. An
 * ability the monster uses on itself targets the actor's own slot,
 * as the API requires.
 */
export function moveOf(
	actor: Combatant,
	ability: Ability,
	target: Combatant | undefined
): Move | null {
	if (!needsTarget(ability)) {
		return { actorSlot: actor.slot, targetSlot: actor.slot, abilityId: ability.id };
	}

	return target ? { actorSlot: actor.slot, targetSlot: target.slot, abilityId: ability.id } : null;
}

/**
 * Stands in for an ability the Catalog no longer lists, so a battle
 * that names it still shows something.
 */
export function unknownAbility(id: string): Ability {
	return {
		id,
		name: id,
		element: 'Neutral',
		power: 0,
		cooldown: 0,
		effect: 'None',
		effectValue: 0,
		effectRounds: 0,
		targetsSelf: false
	};
}

const EFFECT_NAMES: Record<StatusEffect, (value: number) => string> = {
	Burn: (value) => `burns for ${value} each round`,
	Guard: (value) => `takes ${value} % less damage`,
	Slow: (value) => `is ${value} % slower`
};

/**
 * An effect on a monster card: a short chip such as "Burn 2" (the
 * rounds left) and the full words for a screen reader and a tooltip.
 */
export function effectLabel(effect: ActiveEffect): { short: string; long: string } {
	return {
		short: `${effect.kind} ${effect.roundsLeft}`,
		long: `${effect.kind}: ${EFFECT_NAMES[effect.kind](effect.value)}, ${rounds(effect.roundsLeft)} left`
	};
}
