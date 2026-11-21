import { defineEnvVars } from '@sveltejs/kit/env';
import { building } from '$app/env';

/**
 * Reads a base URL and drops a trailing slash, so paths can be
 * appended with a plain `/`.
 */
function baseUrl(value: string): string {
	const url = new URL(value);

	if (url.protocol !== 'http:' && url.protocol !== 'https:') {
		throw new Error('must be an http(s) URL');
	}

	return value.replace(/\/+$/, '');
}

export const variables = defineEnvVars({
	PUBLIC_API_URL: {
		public: true,
		schema: (value) => {
			if (!value) {
				// The build runs the app once to analyse its routes,
				// without the environment it will later start in.
				if (building) {
					return '';
				}

				throw new Error('is required');
			}

			return baseUrl(value);
		},
		description: 'Where the browser reaches the AppHost: its HTTP API and its SignalR hub.'
	},
	API_URL_INTERNAL: {
		schema: (value) => (value ? baseUrl(value) : undefined),
		description:
			'Where this server reaches the AppHost while it renders a page. ' +
			'Only needed when that differs from PUBLIC_API_URL, e.g. inside a container network.'
	}
});
