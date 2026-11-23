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

/** A request the API refused or could not answer. */
export class ArenaApiError extends Error {
	/**
	 * @param message What went wrong, in words fit to show.
	 * @param status The HTTP status, if the API answered at all.
	 */
	constructor(
		message: string,
		readonly status: number | undefined
	) {
		super(message);
		this.name = 'ArenaApiError';
	}
}

/**
 * Runs a request of the Kiota client, so that whatever it fails with
 * reaches the caller as an `ArenaApiError`.
 */
export async function callApi<T>(request: () => Promise<T>): Promise<T> {
	try {
		return await request();
	} catch (error) {
		throw error instanceof ArenaApiError ? error : toApiError(error);
	}
}

/** What Kiota throws for a failed response: its status and, if the API sent one, a problem document. */
interface FailedResponse {
	responseStatusCode?: number;
	detail?: string | null;
	title?: string | null;
	errors?: { additionalData?: Record<string, unknown> } | null;
}

// The API explains a refusal in a problem document: `detail` for a
// broken rule, `errors` per field for an invalid request.
function toApiError(error: unknown): ArenaApiError {
	const failed = (typeof error === 'object' && error !== null ? error : {}) as FailedResponse;
	const fieldErrors = Object.values(failed.errors?.additionalData ?? {}).flat();
	const message =
		failed.detail ??
		fieldErrors.find((entry) => typeof entry === 'string') ??
		failed.title ??
		(failed.responseStatusCode === undefined
			? 'The arena cannot be reached right now.'
			: `The arena answered with status ${failed.responseStatusCode}.`);

	return new ArenaApiError(message, failed.responseStatusCode);
}
