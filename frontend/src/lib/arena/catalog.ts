import { createArenaApi } from './api';

/** An arena battles run on. */
export interface Arena {
	id: string;
	name: string;
}

/** A monster species with the base stats a team is picked by. */
export interface Species {
	id: string;
	name: string;
	maxHitPoints: number;
	attack: number;
	defense: number;
	speed: number;
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
			maxHitPoints: entry.maxHitPoints ?? 0,
			attack: entry.attack ?? 0,
			defense: entry.defense ?? 0,
			speed: entry.speed ?? 0
		}))
	};
}
