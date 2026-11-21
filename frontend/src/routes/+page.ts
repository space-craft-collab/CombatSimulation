import { loadCatalog } from '#lib/arena/catalog.ts';
import type { PageLoad } from './$types';

export const load: PageLoad = ({ fetch }) => loadCatalog(fetch);
