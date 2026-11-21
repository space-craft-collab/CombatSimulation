/**
 * Maps a URL below the public API base onto the internal one. The
 * server renders pages with the same API calls the browser makes,
 * but may have to reach the AppHost under another address.
 *
 * @param url The URL a request was made to.
 * @param publicBase The API base URL the browser uses.
 * @param internalBase The API base URL the server uses, if it differs.
 * @returns The URL to fetch on the server; `url` itself when nothing applies.
 */
export function toInternalUrl(
	url: string,
	publicBase: string,
	internalBase: string | undefined
): string {
	if (!internalBase || internalBase === publicBase) {
		return url;
	}

	// "http://api" must not capture "http://api.example".
	const isBelowPublicBase = url === publicBase || url.startsWith(`${publicBase}/`);

	return isBelowPublicBase ? internalBase + url.slice(publicBase.length) : url;
}
