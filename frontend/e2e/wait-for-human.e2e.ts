import { expect, test, type Browser, type Page } from '@playwright/test';

// The "wait for a human" mode of ADR-0007, against a running frontend
// and AppHost: two players in two browser contexts, and the "play vs
// bot instead" way out of waiting.

const BATTLE_URL = /\/battles\/[0-9a-f-]{36}$/;

/** A visitor of their own, with a nickname already chosen (the solo path tests choosing one). */
async function visitor(browser: Browser, nickname: string): Promise<Page> {
	const context = await browser.newContext();
	await context.addInitScript((name) => localStorage.setItem('arena.nickname', name), nickname);

	return context.newPage();
}

/** Opens Volcano's team picker and fields one monster. */
async function pickTeam(page: Page, speciesId: string): Promise<void> {
	// Network idle: the page is hydrated and its controls react.
	await page.goto('/arenas/volcano', { waitUntil: 'networkidle' });
	await page.getByTestId(`species-${speciesId}`).getByRole('button', { name: /Add/ }).click();
	await expect(page.getByTestId('team-member')).toHaveCount(1);
}

/** Opens a battle that waits for a human, and checks the waiting state. */
async function openBattle(page: Page): Promise<void> {
	await page.getByRole('button', { name: 'Wait for a human' }).click();
	await expect(page).toHaveURL(BATTLE_URL);
	await expect(page.getByTestId('waiting')).toContainText('Waiting for an opponent');
	// The bot offer is there at once, not after a while; and the join
	// deadline (5 minutes by default) counts down.
	await expect(page.getByTestId('bot-instead')).toBeVisible({ timeout: 2_000 });
	await expect(page.getByTestId('join-countdown')).toHaveText(/^\d+:\d\d$/);
}

/** The host's entry in an arena's list of open battles. */
function entryOf(page: Page, hostId: string) {
	return page.getByTestId('open-battle').filter({ hasText: hostId });
}

test('two players meet in an open battle and play it to the end', async ({ browser }) => {
	test.setTimeout(120_000);

	// Names of their own per run, so earlier runs' battles do not matter.
	const stamp = Date.now();
	const hostId = `e2e-host-${stamp}`;
	const guestId = `e2e-guest-${stamp}`;

	// The host opens a battle and waits.
	const host = await visitor(browser, hostId);
	await pickTeam(host, 'ember-fox');
	await openBattle(host);
	const battleUrl = host.url();
	await expect(host.getByTestId('round')).toHaveText('Open battle');
	await expect(host.getByTestId('away-side')).toContainText('Waiting for an opponent…');

	// The host's own battle is not offered to them to join.
	const hostPicker = await host.context().newPage();
	await hostPicker.goto('/arenas/volcano', { waitUntil: 'networkidle' });
	await expect(hostPicker.getByTestId('own-battle')).toHaveCount(1);
	await expect(entryOf(hostPicker, hostId)).toHaveCount(0);
	await hostPicker.close();

	// The guest finds it in the arena's list and joins with a team.
	const guest = await visitor(browser, guestId);
	await pickTeam(guest, 'frost-owl');
	await entryOf(guest, hostId).getByRole('button', { name: /Join/ }).click();
	await expect(guest).toHaveURL(battleUrl);
	await expect(guest.getByTestId('round')).toHaveText('Round 1');
	await expect(guest.getByTestId('away-side').locator('h2')).toHaveText(hostId);

	// BattleStarted moves the waiting host into the battle, without a reload.
	await expect(host.getByTestId('round')).toHaveText('Round 1');
	await expect(host.getByTestId('waiting')).toHaveCount(0);
	await expect(host.getByTestId('away-side').locator('h2')).toHaveText(guestId);
	await expect(host.getByTestId('away-side')).toContainText('Frost Owl');

	const hostRound = host.getByTestId('round');
	const guestRound = guest.getByTestId('round');
	const hostResult = host.getByTestId('battle-result');
	const guestResult = guest.getByTestId('battle-result');

	// One round: the host's move alone does not play it.
	await host.getByTestId('strike').click();
	await expect(host.getByTestId('waiting-for-move')).toContainText(`Waiting for ${guestId}`);
	await expect(host.getByTestId('strike')).toHaveCount(0);
	await expect(hostRound).toHaveText('Round 1');
	await guest.getByTestId('strike').click();
	await expect(hostRound).toHaveText('Round 2');
	await expect(guestRound).toHaveText('Round 2');
	await expect(host.getByTestId('waiting-for-move')).toHaveCount(0);
	// The opponent is named by their nickname, as they wrote it.
	await expect(host.getByTestId('battle-log')).toContainText(`${guestId}'s Frost Owl`);

	// Play it out, both with the default ability. ADR-0012 keeps a
	// battle well under 30 rounds.
	for (let played = 0; played < 30 && !(await hostResult.isVisible()); played++) {
		const before = await hostRound.textContent();
		await host.getByTestId('strike').click();
		await expect(host.getByTestId('waiting-for-move')).toBeVisible();
		await guest.getByTestId('strike').click();
		await expect(async () => {
			const moved = (await hostResult.isVisible()) || (await hostRound.textContent()) !== before;
			expect(moved).toBe(true);
		}).toPass({ timeout: 10_000 });
	}

	// Each side reads the result in its own words; no bot is named.
	await expect(hostResult).toBeVisible();
	await expect(guestResult).toBeVisible();
	const hostVerdict = await hostResult.locator('h2').textContent();
	expect(['You won!', `${guestId} won.`]).toContain(hostVerdict);
	await expect(guestResult.locator('h2')).toHaveText(
		hostVerdict === 'You won!' ? `${hostId} won.` : 'You won!'
	);
});

test('a waiting host plays the bot instead', async ({ browser }) => {
	const hostId = `e2e-waiter-${Date.now()}`;
	const host = await visitor(browser, hostId);
	await pickTeam(host, 'ember-fox');
	await openBattle(host);

	// A reload while waiting picks the open battle up again.
	await host.reload({ waitUntil: 'networkidle' });
	await expect(host.getByTestId('waiting')).toBeVisible();
	await expect(host.getByTestId('bot-instead')).toBeVisible();

	// The bot takes the seat, on the difficulty chosen here.
	await host.getByTestId('bot-offer').getByText('Easy', { exact: true }).click();
	await expect(host.getByRole('radio', { name: 'Easy' })).toBeChecked();
	await host.getByTestId('bot-instead').click();

	await expect(host.getByTestId('round')).toHaveText('Round 1');
	await expect(host.getByTestId('waiting')).toHaveCount(0);
	await expect(host.getByTestId('difficulty')).toHaveText('Easy');
	await expect(host.getByTestId('away-side').locator('h2')).toHaveText('Bot');

	// Against the bot a move plays the round at once.
	await host.getByTestId('strike').click();
	await expect(host.getByTestId('round')).toHaveText('Round 2');
	await expect(host.getByTestId('battle-log')).toContainText(/Your Ember Fox hits the bot's/);
});

test('a player who was too slow to join is told so', async ({ browser }) => {
	const stamp = Date.now();
	const hostId = `e2e-popular-${stamp}`;

	const host = await visitor(browser, hostId);
	await pickTeam(host, 'ember-fox');
	await openBattle(host);

	// Two players see the same open battle.
	const fast = await visitor(browser, `e2e-fast-${stamp}`);
	const slow = await visitor(browser, `e2e-slow-${stamp}`);
	await pickTeam(fast, 'moss-golem');
	await pickTeam(slow, 'lava-crab');
	await expect(entryOf(slow, hostId)).toHaveCount(1);
	// The slow player's list is not read again, so it still shows the
	// battle once it is taken.
	await slow.route('**/arenas/volcano/battles', (route) => route.abort());

	await entryOf(fast, hostId).getByRole('button', { name: /Join/ }).click();
	await expect(fast).toHaveURL(BATTLE_URL);

	await entryOf(slow, hostId).getByRole('button', { name: /Join/ }).click();
	await expect(slow.getByTestId('open-battles').getByRole('alert')).toContainText('no longer open');
	await expect(slow).toHaveURL(/\/arenas\/volcano$/);
	await expect(host.getByTestId('round')).toHaveText('Round 1');
});
