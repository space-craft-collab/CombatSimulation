import glacier from '#lib/assets/arenas/glacier.webp';
import mossGrove from '#lib/assets/arenas/moss-grove.webp';
import volcano from '#lib/assets/arenas/volcano.webp';

// One picture per arena of the Catalog, by its id. The pictures
// ship with the frontend, not the API: they are its look, not
// game data (ADR-0004).
const IMAGES: Readonly<Record<string, string>> = {
	volcano,
	glacier,
	'moss-grove': mossGrove
};

/**
 * The picture of an arena, or `undefined` for an arena that has
 * none yet: a new Catalog entry still shows up, only plainer.
 */
export function arenaImageOf(arenaId: string): string | undefined {
	return Object.hasOwn(IMAGES, arenaId) ? IMAGES[arenaId] : undefined;
}
