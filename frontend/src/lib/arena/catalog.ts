import type { AbilityResponse } from '#lib/api/models/index.ts';
import { createArenaApi } from './api.ts';
import type { AbilityEffect } from './battle.ts';

/** An arena battles run on. */
export interface Arena {
	id: string;
	name: string;
}

/**
 * The element of a species or of an ability. Fire beats Nature,
 * Nature beats Frost, Frost beats Fire; neutral is neither strong
 * nor weak. Only abilities are neutral.
 */
export type Element = 'Neutral' | 'Fire' | 'Frost' | 'Nature';

/** Something a monster can do in its move, as the Catalog describes it. */
export interface Ability {
	id: string;
	name: string;
	/** Counts for its damage only, not the element of the monster using it. */
	element: Element;
	/** Damage in percent of the monster's attack; 0 for an ability that deals none. */
	power: number;
	/** Rounds it cannot be used after the round it was used in. */
	cooldown: number;
	effect: AbilityEffect;
	/** Hit points per round for a burn; percent for a guard, a slow and a heal. */
	effectValue: number;
	/** Rounds an effect lasts, the one it is applied in included; 0 for a heal. */
	effectRounds: number;
	/** True for an ability the monster uses on itself: it needs no target. */
	targetsSelf: boolean;
}

/** A monster species with the base stats a team is picked by. */
export interface Species {
	id: string;
	name: string;
	element: Element;
	maxHitPoints: number;
	attack: number;
	defense: number;
	speed: number;
	/** In their order, the basic `strike` first. */
	abilities: Ability[];
}

/** What the Catalog offers a visitor. */
export interface Catalog {
	arenas: Arena[];
	species: Species[];
}

/**
 * Reads the arenas and the species.
 *
 * Kiota types every property as optional, whatever the OpenAPI
 * document requires, so the generated models are mapped onto the
 * strict types above once, here, and not checked in every component.
 *
 * @param fetch The fetch of the calling `load` function.
 */
export async function loadCatalog(fetch: typeof globalThis.fetch): Promise<Catalog> {
	const api = createArenaApi(fetch);
	const [arenas, species] = await Promise.all([api.arenas.get(), api.species.get()]);

	return {
		arenas: (arenas ?? []).map((arena) => ({
			id: arena.id ?? '',
			name: arena.name ?? ''
		})),
		species: (species ?? []).map((entry) => ({
			id: entry.id ?? '',
			name: entry.name ?? '',
			element: (entry.element ?? 'Neutral') satisfies Element,
			maxHitPoints: entry.maxHitPoints ?? 0,
			attack: entry.attack ?? 0,
			defense: entry.defense ?? 0,
			speed: entry.speed ?? 0,
			abilities: (entry.abilities ?? []).map(toAbility)
		}))
	};
}

function toAbility(ability: AbilityResponse): Ability {
	return {
		id: ability.id ?? '',
		name: ability.name ?? ability.id ?? '',
		element: (ability.element ?? 'Neutral') satisfies Element,
		power: ability.power ?? 0,
		cooldown: ability.cooldown ?? 0,
		effect: (ability.effect ?? 'None') satisfies AbilityEffect,
		effectValue: ability.effectValue ?? 0,
		effectRounds: ability.effectRounds ?? 0,
		targetsSelf: ability.targetsSelf ?? false
	};
}

/**
 * Every ability any species knows, by its id. A battle names its
 * monsters' abilities by id only; the rest comes from here.
 */
export function abilitiesById(species: Species[]): Map<string, Ability> {
	return new Map(
		species.flatMap((entry) => entry.abilities.map((ability) => [ability.id, ability]))
	);
}
