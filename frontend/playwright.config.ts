import { defineConfig, devices } from '@playwright/test';

// GitHub Actions sets CI; compose.yaml hands it to the e2e container.
const ci = !!process.env.CI;

// The end-to-end tests drive a browser against a frontend that is
// already running, with the AppHost behind it (see compose.yaml).
export default defineConfig({
	testDir: 'e2e',
	testMatch: '**/*.e2e.ts',
	forbidOnly: ci,
	// On CI, one retry absorbs a hiccup of a slow runner. A test that
	// passes only on its retry is still reported as flaky, and the
	// failed attempt's trace is kept.
	retries: ci ? 1 : 0,
	// The github reporter turns failures and flaky tests into
	// annotations on the run.
	reporter: ci ? [['list'], ['github']] : 'list',
	use: {
		baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
		trace: 'retain-on-failure'
	},
	projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }]
});
