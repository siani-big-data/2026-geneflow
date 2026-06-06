import type { Page } from "@playwright/test";

/**
 * Re-usable UI flows. Use semantic selectors (roles, labels, ids) so the
 * tests survive small visual refactors.
 */

export async function gotoLogin(page: Page) {
  await page.goto("/login");
  await page.waitForLoadState("networkidle");
}

export async function gotoRegister(page: Page) {
  await page.goto("/register");
  await page.waitForLoadState("networkidle");
}

/**
 * Login via the UI (full flow). Use `auth` fixture when you only need to
 * arrive at a protected page - it's faster (skips UI).
 */
export async function uiLogin(
  page: Page,
  email: string,
  password: string,
): Promise<void> {
  await gotoLogin(page);
  await page.locator("#email").fill(email);
  await page.locator("#password").fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/dashboard/i, { timeout: 15_000 });
}

export async function uiLogout(page: Page): Promise<void> {
  // Header / sidebar usually exposes a user menu with a logout option
  const userMenu = page
    .getByRole("button", { name: /account|profile|user menu/i })
    .first();
  if (await userMenu.isVisible().catch(() => false)) {
    await userMenu.click();
  }
  await page
    .getByRole("menuitem", { name: /log ?out|sign ?out/i })
    .or(page.getByRole("button", { name: /log ?out|sign ?out/i }))
    .first()
    .click();
  await page.waitForURL(/\/(login)?$/i, { timeout: 10_000 });
}

/**
 * Wait until the dashboard finished its initial data load.
 */
export async function waitForDashboard(page: Page): Promise<void> {
  await page.waitForURL(/\/dashboard/i);
  await page.getByRole("heading", { name: /dashboard|welcome/i }).first().waitFor();
}

/**
 * Switch the locale by visiting the language switcher in the header.
 */
export async function switchLocale(page: Page, locale: "en" | "es") {
  // Try the language toggle in header
  const toggle = page.getByRole("button", { name: /language|idioma|en|es/i }).first();
  if (await toggle.isVisible().catch(() => false)) {
    await toggle.click();
    await page.getByRole("menuitem", { name: new RegExp(locale, "i") }).first().click();
  } else {
    // Fallback: navigate directly with the locale prefix
    const current = new URL(page.url());
    const path = current.pathname.replace(/^\/(en|es)/, "");
    await page.goto(`/${locale}${path || "/"}`);
  }
}
