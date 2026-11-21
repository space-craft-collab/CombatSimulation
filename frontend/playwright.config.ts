import { defineConfig, devices } from '@playwright/test';

// The end-to-end tests drive a browser against a frontend that is
// already running, with the AppHost behind it (see compose.yaml).
export default defineConfig({
	testDir: 'e2e',
	testMatch: '**/*.e2e.ts',
	forbidOnly: !!process.env.CI,
	reporter: 'list',
	use: {
		baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
		trace: 'retain-on-failure'
	},
	projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }]
});
