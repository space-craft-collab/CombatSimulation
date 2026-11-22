import { expect, test } from '@playwright/test';

// The solo path of Phase 4, against a running frontend and AppHost.

test('the server renders the arenas and the species into its pages', async ({ request }) => {
	const home = await request.get('/');
	const arena = await request.get('/arenas/volcano');

	expect(home.status()).toBe(200);
	expect(await home.text()).toContain('Volcano');
	expect(arena.status()).toBe(200);
	expect(await arena.text()).toContain('Ember Fox');
});

test('an unknown arena is a 404', async ({ request }) => {
	expect((await request.get('/arenas/no-such-arena')).status()).toBe(404);
});

test('a visitor plays a battle against the bot, from the nickname to the result', async ({
	page
}) => {
	// A name of its own per run, so the stats of earlier runs do not matter.
	const nickname = `e2e-${Date.now()}`;

	// Network idle: the page is hydrated and its controls react.
	await page.goto('/', { waitUntil: 'networkidle' });

	// First visit: the nickname.
	await page.getByLabel('Nickname').fill(`  ${nickname}  `);
	await page.getByRole('button', { name: 'Save' }).click();
	await expect(page.getByTestId('playing-as')).toContainText(nickname);

	// Arena browser, then the team picker.
	await page.getByRole('link', { name: /Volcano/ }).click();
	await expect(page.getByRole('heading', { name: 'Volcano' })).toBeVisible();
	await page.getByTestId('species-ember-fox').getByRole('button', { name: /Add/ }).click();
	await expect(page.getByTestId('team-member')).toHaveCount(1);
	await page.getByRole('button', { name: 'Fight the bot' }).click();

	// The battle. The bot's team is its own business: its size and
	// its species are not assumed here.
	await expect(page).toHaveURL(/\/battles\/[0-9a-f-]{36}$/);
	await expect(page.getByTestId('home-side').getByTestId('combatant')).toHaveCount(1);
	await expect(page.getByTestId('away-side').getByTestId('combatant').first()).toBeVisible();
	await expect(page.getByTestId('round')).toHaveText('Round 1');
	await expect(page.getByTestId('countdown')).toBeVisible();

	// The fox's abilities, the basic attack picked by default.
	await expect(page.getByTestId('ability-strike')).toHaveAttribute('aria-pressed', 'true');
	await expect(page.getByTestId('ability-flame-dash')).toBeEnabled();

	const strike = page.getByTestId('strike');
	const round = page.getByTestId('round');
	const result = page.getByTestId('battle-result');

	// One round, as a delta from the hub.
	await expect(strike).toContainText('Ember Fox uses Strike on');
	await strike.click();
	await expect(round).toHaveText('Round 2');
	await expect(page.getByTestId('battle-log')).toContainText(
		/Your Ember Fox hits the bot's [A-Za-z ]+ with Strike for \d+\./
	);

	// A reload picks the battle up where it stands.
	await page.reload({ waitUntil: 'networkidle' });
	await expect(round).toHaveText('Round 2');
	await expect(page.getByTestId('playing-as')).toContainText(nickname);

	// Play it out with the default ability. ADR-0012 keeps a battle
	// well under 30 rounds.
	for (let played = 0; played < 30 && !(await result.isVisible()); played++) {
		const before = await round.textContent();
		await strike.click();
		await expect(async () => {
			const moved = (await result.isVisible()) || (await round.textContent()) !== before;
			expect(moved).toBe(true);
		}).toPass({ timeout: 10_000 });
	}

	// Who wins depends on the bot's team.
	await expect(result).toContainText(/You won!|The bot won\./);
	await expect(strike).toHaveCount(0);
	const verdict = await result.locator('h2').textContent();

	// The finished battle is still there after a reload, now from the cold store.
	await page.reload({ waitUntil: 'networkidle' });
	await expect(result.locator('h2')).toHaveText(verdict ?? '');
});
