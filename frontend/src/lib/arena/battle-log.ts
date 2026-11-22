// The battle log's sentences. Pure: the page hands in how it names
// monsters and abilities, so the wording is tested without a page.

import type { Combatant, RoundEvent } from './battle.ts';

/** How the log names the things an event refers to by id. */
export interface LogNames {
	/** The monster in a slot, as the viewer says it: "your Ember Fox", "the bot's Lava Crab". */
	monster(slot: number): string;
	/** An ability's display name. */
	ability(abilityId: string): string;
}

/**
 * A monster as the viewer says it: "your Ember Fox" for the viewer's
 * own, "the bot's Lava Crab", "ada's Frost Owl".
 *
 * @param viewerId The viewer's side; null for a spectator.
 */
export function ownedName(
	combatant: Combatant,
	viewerId: string | null,
	speciesName: string
): string {
	const owner =
		combatant.participantId === viewerId
			? 'your'
			: combatant.isBot
				? "the bot's"
				: `${combatant.participantId}'s`;

	return `${owner} ${speciesName}`;
}

/**
 * One event as a sentence: "Your Ember Fox hits the bot's Moss Golem
 * with Flame Dash for 15 and sets it on fire. It's super effective."
 */
export function sentenceOf(event: RoundEvent, names: LogNames): string {
	const actor = capitalized(names.monster(event.actorSlot));
	const defeated = event.targetHitPoints === 0;

	if (event.kind === 'Burn') {
		return `${actor} takes ${event.damage} from its burn${defeated ? ' and is defeated' : ''}.`;
	}

	// An event of the old shape has no ability; the basic attack is what it was.
	const ability = names.ability(event.abilityId ?? 'strike');

	if (event.effect === 'Heal') {
		return event.healing > 0
			? `${actor} uses ${ability} and heals ${event.healing}.`
			: `${actor} uses ${ability}, but has nothing to heal.`;
	}

	if (event.effect === 'Guard') {
		return `${actor} uses ${ability} and guards itself.`;
	}

	const target = names.monster(event.targetSlot);
	const action =
		event.damage > 0
			? `${actor} hits ${target} with ${ability} for ${event.damage}`
			: `${actor} uses ${ability} on ${target}`;
	// A fallen monster's new effect does not matter any more.
	const consequence = defeated
		? 'defeats it'
		: event.effect === 'Burn'
			? 'sets it on fire'
			: event.effect === 'Slow'
				? 'slows it'
				: null;

	return `${action}${consequence ? ` and ${consequence}` : ''}.${effectivenessNote(event)}`;
}

function effectivenessNote(event: RoundEvent): string {
	if (event.damage === 0) {
		return '';
	}

	switch (event.effectiveness) {
		case 'Strong':
			return " It's super effective.";
		case 'Weak':
			return " It's not very effective.";
		case 'Normal':
			return '';
	}
}

function capitalized(text: string): string {
	return text.charAt(0).toUpperCase() + text.slice(1);
}
