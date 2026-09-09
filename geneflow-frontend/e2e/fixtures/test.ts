import {
  test as base,
  type APIRequestContext,
  type Page,
} from "@playwright/test";
import {
  apiLogin,
  apiRegisterUser,
  createApiContext,
  tryConfirmEmail,
  type RegisteredUser,
} from "./api";

/**
 * Custom fixtures for GeneFlow E2E tests.
 *
 * - `api`     : raw backend HTTP client
 * - `user`    : freshly registered + confirmed user (unique per test)
 * - `auth`    : already authenticated page (logged in via API + UI navigation)
 */
type Fixtures = {
  api: APIRequestContext;
  user: RegisteredUser;
  auth: { page: Page; user: RegisteredUser; token: string };
};

/**
 * Frontend default locale is `es` (see app/i18n/config.ts), so visiting
 * `/studies` would redirect to `/es/studies` and break English-only test
 * regexes. We force `NEXT_LOCALE=en` at the cookie level so every
 * `page.goto("...")` call lands on the English variant.
 */
const FRONTEND_ORIGIN = process.env.E2E_BASE_URL ?? "http://localhost:3000";

export const test = base.extend<Fixtures>({
  page: async ({ page }, use) => {
    await page.context().addCookies([
      {
        name: "NEXT_LOCALE",
        value: "en",
        url: FRONTEND_ORIGIN,
      },
    ]);
    await use(page);
  },

  api: async ({}, use) => {
    const api = await createApiContext();
    await use(api);
    await api.dispose();
  },

  user: async ({ api }, use) => {
    const user = await apiRegisterUser(api);
    await tryConfirmEmail(api, user.email);
    await use(user);
    // No teardown - backend should clean test users via TTL or test cleanup job.
  },

  auth: async ({ page, api, user }, use) => {
    const token = await apiLogin(api, user.email, user.password);
    // Inject token before the app boots
    await page.addInitScript((t: string) => {
      try {
        const store = {
          state: {
            accessToken: t,
            isAuthenticated: true,
          },
          version: 0,
        };
        window.localStorage.setItem("auth-storage", JSON.stringify(store));
        window.localStorage.setItem("accessToken", t);
      } catch {
        // ignore
      }
    }, token);
    await use({ page, user, token });
  },
});

export { expect } from "@playwright/test";
