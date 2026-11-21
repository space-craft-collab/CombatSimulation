import { error } from '@sveltejs/kit';
import { loadCatalog } from '#lib/arena/catalog.ts';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch, params }) => {
	const catalog = await loadCatalog(fetch);
	const arena = catalog.arenas.find((entry) => entry.id === params.arenaId);

	if (!arena) {
		error(404, 'There is no such arena.');
	}

	return { arena, species: catalog.species };
};
