import { expect, test, type Page } from '@playwright/test';

// The player dashboard, against a running frontend and AppHost: the
// empty state of a new player, and the record and history after a
// battle against the bot.

// A time zone far from UTC: the server renders times in UTC, the
// browser in its own zone, and hydration must not trip over that.
test.use({ timezoneId: 'Pacific/Auckland' });

/** Plays as `nickname` (the solo path tests choosing one), and collects hydration warnings. */
async function visit(page: Page, nickname: string): Promise<string[]> {
	await page
		.context()
		.addInitScript((name) => localStorage.setItem('arena.nickname', name), nickname);

	const warnings: string[] = [];
	page.on('console', (message) => {
		if (/hydrat/i.test(message.text())) {
			warnings.push(message.text());
		}
	});

	return warnings;
}

test('a fresh nickname sees an empty dashboard with a way to start playing', async ({ page }) => {
	// A name of its own per run, so earlier runs' battles do not matter.
	const nickname = `e2e-new-${Date.now()}`;
	const warnings = await visit(page, nickname);

	// Network idle: the page is hydrated and its controls react.
	await page.goto('/', { waitUntil: 'networkidle' });
	await page.getByTestId('playing-as').click();

	await expect(page).toHaveURL(new RegExp(`/players/${nickname}$`));
	await expect(page.getByTestId('player-name')).toHaveText(nickname);
	const empty = page.getByTestId('empty-dashboard');
	await expect(empty).toContainText('No battles yet');
	await expect(empty).toContainText('with your wins and losses');
	await expect(page.getByTestId('record')).toHaveCount(0);

	await empty.getByRole('link', { name: 'Choose an arena' }).click();
	await expect(page.getByRole('heading', { name: 'Choose an arena' })).toBeVisible();
	expect(warnings).toEqual([]);
});

test('an unknown player has an empty dashboard; a name nobody can have is a 404', async ({
	request
}) => {
	const unknown = await request.get(`/players/e2e-nobody-${Date.now()}`);

	expect(unknown.status()).toBe(200);
	expect(await unknown.text()).toContain('has not finished a battle yet');
	expect((await request.get('/players/bot')).status()).toBe(404);
	expect((await request.get('/players/%20padded')).status()).toBe(404);
});

test('after a battle against the bot, the dashboard counts it and leads back to it', async ({
	page
}) => {
	test.setTimeout(90_000);

	const nickname = `e2e-dash-${Date.now()}`;
	const warnings = await visit(page, nickname);

	// One monster; the bot fields as many.
	await page.goto('/arenas/volcano', { waitUntil: 'networkidle' });
	await page.getByTestId('species-ember-fox').getByRole('button', { name: /Add/ }).click();
	await expect(page.getByTestId('team-member')).toHaveCount(1);
	await page.getByRole('button', { name: 'Fight the bot' }).click();
	await expect(page).toHaveURL(/\/battles\/[0-9a-f-]{36}$/);
	const battleUrl = page.url();

	// Play it out with the default ability. ADR-0012 keeps a battle
	// well under 30 rounds.
	const strike = page.getByTestId('strike');
	const round = page.getByTestId('round');
	const result = page.getByTestId('battle-result');
	await expect(round).toHaveText('Round 1');
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
	const verdict = (await result.locator('h2').textContent()) ?? '';
	const won = verdict === 'You won!';
	const decidedIn = (await round.textContent())?.replace('Round ', '') ?? '';

	// Straight from the result: the backend counts the battle a
	// moment after the result reached the page, and the dashboard
	// waits for that.
	await page.getByTestId('record-link').click();
	await expect(page).toHaveURL(new RegExp(`/players/${nickname}\\?after=`));
	await expect(page.getByTestId('player-name')).toHaveText(nickname);
	await expect(page.getByTestId('not-settled')).toHaveCount(0);

	const expectDashboard = async () => {
		await expect(page.getByTestId('wins')).toHaveText(won ? '1' : '0');
		await expect(page.getByTestId('losses')).toHaveText(won ? '0' : '1');
		await expect(page.getByTestId('win-rate')).toHaveText(won ? '100%' : '0%');

		const entries = page.getByTestId('history-entry');
		await expect(entries).toHaveCount(1);
		await expect(entries.getByTestId('history-result')).toHaveText(won ? 'Won' : 'Lost');
		await expect(entries.getByTestId('history-opponent')).toHaveText('vs the bot');
		await expect(entries.getByTestId('history-arena')).toHaveText('Volcano');
		await expect(entries.getByTestId('history-detail')).toHaveText(`Decided in round ${decidedIn}`);
		// In the browser's time zone, not in the UTC the server rendered.
		await expect(entries.locator('time')).toHaveAttribute('datetime', /Z$/);
		await expect(entries.locator('time')).not.toContainText('UTC');
	};

	await expectDashboard();

	// The same, rendered by the server.
	await page.reload({ waitUntil: 'networkidle' });
	await expectDashboard();
	expect(warnings).toEqual([]);

	// The entry leads to the finished battle, now from the cold store.
	await page.getByTestId('history-entry').click();
	await expect(page).toHaveURL(battleUrl);
	await expect(result.locator('h2')).toHaveText(verdict);
});
