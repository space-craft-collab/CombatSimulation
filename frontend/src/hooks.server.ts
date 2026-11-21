import type { Handle, HandleFetch } from '@sveltejs/kit/hooks';
import { API_URL_INTERNAL } from '$app/env/private';
import { PUBLIC_API_URL } from '$app/env/public';
import { toInternalUrl } from '#lib/arena/internal-url.ts';

/**
 * A `load` function calls the API under its public URL, on the
 * server as in the browser. Where the server has to use another
 * address for the same AppHost, the call is redirected here.
 */
export const handleFetch: HandleFetch = ({ request, fetch }) => {
	const url = toInternalUrl(request.url, PUBLIC_API_URL, API_URL_INTERNAL);

	return fetch(url === request.url ? request : new Request(url, request));
};

/**
 * The response headers of a `load` function's API calls that the
 * Kiota client reads: the body's type, and what its retry and
 * redirect handlers look at.
 */
const API_RESPONSE_HEADERS = new Set(['content-type', 'retry-after', 'location']);

/**
 * SvelteKit hides the headers of responses fetched in `load`
 * unless they are named here, since they travel to the browser
 * with the rendered page.
 */
export const handle: Handle = ({ event, resolve }) =>
	resolve(event, {
		filterSerializedResponseHeaders: (name) => API_RESPONSE_HEADERS.has(name)
	});
