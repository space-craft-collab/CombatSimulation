import { error } from '@sveltejs/kit';
import { findBattle } from '#lib/arena/battles.ts';
import { loadCatalog } from '#lib/arena/catalog.ts';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch, params }) => {
	const [battle, catalog] = await Promise.all([
		findBattle(fetch, params.battleId),
		loadCatalog(fetch)
	]);

	if (!battle) {
		error(404, 'There is no such battle.');
	}

	return { battle, catalog };
};
