import { defineConfig, devices } from "@playwright/test";

/**
 * Playwright E2E configuration for GeneFlow frontend.
 *
 * Tests assume the backend API is running on http://localhost:5145
 * and the frontend dev server on http://localhost:3000.
 *
 * Run with:
 *   npm run e2e             - headless, all browsers
 *   npm run e2e:ui          - Playwright UI mode
 *   npm run e2e:headed      - headed mode
 *   npm run e2e:debug       - debug mode
 *   npm run e2e:report      - open last HTML report
 */
export default defineConfig({
  testDir: "./e2e",
  testMatch: /.*\.spec\.ts/,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false, // backend has shared state; run files in sequence
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [
    ["list"],
    ["html", { outputFolder: "playwright-report", open: "never" }],
  ],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3000",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    locale: "en-US",
    timezoneId: "Europe/Madrid",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: process.env.E2E_SKIP_WEBSERVER
    ? undefined
    : {
        command: "npm run dev",
        url: "http://localhost:3000",
        reuseExistingServer: true,
        timeout: 120_000,
        stdout: "ignore",
        stderr: "pipe",
      },
});
