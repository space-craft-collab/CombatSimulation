import { expect, test } from '@playwright/test';

// The battle stage on a phone, against a running frontend and
// AppHost: 3 against 3 fits one screen, the strike button in reach.

test.use({ viewport: { width: 390, height: 660 } });

test('on a phone, a 3-vs-3 battle shows the stage, the abilities and the strike button at once', async ({
	page
}) => {
	// A name of its own per run; the solo path tests choosing one.
	const nickname = `e2e-phone-${Date.now()}`;
	await page
		.context()
		.addInitScript((name) => localStorage.setItem('arena.nickname', name), nickname);

	// Network idle: the page is hydrated and its controls react.
	await page.goto('/arenas/volcano', { waitUntil: 'networkidle' });
	for (const speciesId of ['ember-fox', 'lava-crab', 'frost-owl']) {
		await page.getByTestId(`species-${speciesId}`).getByRole('button', { name: /Add/ }).click();
	}
	await expect(page.getByTestId('team-member')).toHaveCount(3);
	await page.getByRole('button', { name: 'Fight the bot' }).click();

	// The bot fields as many monsters as the player.
	await expect(page).toHaveURL(/\/battles\/[0-9a-f-]{36}$/);
	await expect(page.getByTestId('home-side').getByTestId('combatant')).toHaveCount(3);
	await expect(page.getByTestId('away-side').getByTestId('combatant')).toHaveCount(3);

	const stage = page.getByTestId('stage');
	const abilities = page.getByRole('list', { name: 'Abilities' });
	const strike = page.getByTestId('strike');
	await expect(strike).toBeVisible();

	// All of it without scrolling.
	expect(await page.evaluate(() => window.scrollY)).toBe(0);
	await expect(stage).toBeInViewport({ ratio: 1 });
	await expect(abilities).toBeInViewport({ ratio: 1 });
	await expect(strike).toBeInViewport({ ratio: 1 });

	// The strike section sticks to the bottom; it must not cover the abilities.
	const listBottom = await abilities.evaluate((list) => list.getBoundingClientRect().bottom);
	const strikeTop = await page
		.locator('section', { has: strike })
		.evaluate((section) => section.getBoundingClientRect().top);
	expect(listBottom).toBeLessThanOrEqual(strikeTop);
});
