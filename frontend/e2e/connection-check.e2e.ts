import { expect, test } from '@playwright/test';

// The walking skeleton of Phase 4: the three ways the frontend
// reaches the AppHost. The solo path replaces these tests once
// the real screens exist.

test('the server renders the Catalog into the page', async ({ request }) => {
	const response = await request.get('/');
	const html = await response.text();

	expect(response.status()).toBe(200);
	expect(html).toContain('Volcano');
	expect(html).toContain('Ember Fox');
});

test('the browser plays a round against the bot over HTTP and SignalR', async ({ page }) => {
	// Network idle: the page is hydrated and the button reacts.
	await page.goto('/', { waitUntil: 'networkidle' });
	await expect(page.getByTestId('arena')).toHaveCount(3);

	await page.getByRole('button', { name: 'Run check' }).click();

	const steps = page.getByTestId('check-steps').getByRole('listitem');
	await expect(steps).toHaveCount(3, { timeout: 15_000 });
	await expect(steps.nth(0)).toContainText('HTTP: started battle');
	await expect(steps.nth(2)).toContainText('round 1 resolved');
	await expect(page.getByTestId('check-failure')).toHaveCount(0);
});
