import { error } from '@sveltejs/kit';
import { loadCatalog } from '#lib/arena/catalog.ts';
import { AFTER_PARAM, parsePlayerId } from '#lib/arena/dashboard.ts';
import { loadDashboard } from '#lib/arena/players.ts';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch, params, url }) => {
	// Any nickname a player could have chosen gets a dashboard, empty
	// until a battle of theirs is over: the API cannot tell a new
	// player from one nobody plays as. One nobody can choose is a 404.
	const playerId = parsePlayerId(params.playerId);

	if (playerId === null) {
		error(404, 'No player can have this name.');
	}

	const [catalog, dashboard] = await Promise.all([
		loadCatalog(fetch),
		// The result panel names the battle just over, so it is
		// counted by the time the dashboard shows.
		loadDashboard(fetch, playerId, url.searchParams.get(AFTER_PARAM))
	]);

	return { playerId, arenas: catalog.arenas, ...dashboard };
};
