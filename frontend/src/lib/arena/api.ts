import { AnonymousAuthenticationProvider } from '@microsoft/kiota-abstractions';
import { FetchRequestAdapter, HttpClient } from '@microsoft/kiota-http-fetchlibrary';
import { PUBLIC_API_URL } from '$app/env/public';
import { createArenaApiClient, type ArenaApiClient } from '#lib/api/arenaApiClient.ts';

/**
 * Creates the Kiota client for the AppHost's HTTP API.
 *
 * @param fetch The fetch to send requests with. A `load` function
 * passes its own, so SvelteKit can run the call on the server while
 * it renders and hand the response on to the browser.
 */
export function createArenaApi(fetch: typeof globalThis.fetch = globalThis.fetch): ArenaApiClient {
	// The generated client registers its JSON serializer on the
	// adapter's default registries.
	const adapter = new FetchRequestAdapter(
		// No auth before Phase 6.
		new AnonymousAuthenticationProvider(),
		undefined,
		undefined,
		new HttpClient((request, init) => fetchDetached(fetch, request, init))
	);
	adapter.baseUrl = PUBLIC_API_URL;

	return createArenaApiClient(adapter);
}

/** Statuses whose responses must not carry a body. */
const NULL_BODY_STATUSES = new Set([101, 204, 205, 304]);

/**
 * Fetches and returns the response as a plain copy.
 *
 * While a page is rendered on the server, SvelteKit's `fetch`
 * returns a response it observes in order to pass the body on to
 * the browser. Kiota's adapter looks at `body` before it reads the
 * content, which that observed response allows only once. Reading
 * the content here, the one way SvelteKit records, and handing
 * Kiota a copy keeps both working.
 */
async function fetchDetached(
	fetch: typeof globalThis.fetch,
	request: string,
	init: RequestInit
): Promise<Response> {
	const response = await fetch(request, init);
	const body = NULL_BODY_STATUSES.has(response.status) ? null : await response.arrayBuffer();

	return new Response(body, {
		status: response.status,
		statusText: response.statusText,
		headers: response.headers
	});
}
