import { error } from '@sveltejs/kit';
import { parseDifficulty } from '#lib/arena/battle.ts';
import { listOpenBattles } from '#lib/arena/battles.ts';
import { loadCatalog } from '#lib/arena/catalog.ts';
import { JOIN_PARAM, type OpenBattle } from '#lib/arena/lobby.ts';
import { DIFFICULTY_PARAM, TEAM_PARAM, parseTeam } from '#lib/arena/team.ts';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ fetch, params, url }) => {
	const [catalog, openBattles] = await Promise.all([
		loadCatalog(fetch),
		// The picker works without the list; the browser reads it
		// again every few seconds anyway.
		listOpenBattles(params.arenaId, fetch).catch((): OpenBattle[] => [])
	]);
	const arena = catalog.arenas.find((entry) => entry.id === params.arenaId);

	if (!arena) {
		error(404, 'There is no such arena.');
	}

	// Only "Fight again" hands a team and a difficulty over; an arena
	// chosen from the list starts with no team, on the default.
	const team = parseTeam(url.searchParams.get(TEAM_PARAM), catalog.species);
	const difficulty = parseDifficulty(url.searchParams.get(DIFFICULTY_PARAM));
	// A waiting battle's page sends a visitor here to pick a team and join it.
	const invitedId = url.searchParams.get(JOIN_PARAM);

	return { arena, species: catalog.species, team, difficulty, openBattles, invitedId };
};
