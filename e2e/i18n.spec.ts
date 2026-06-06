import { test, expect } from "./fixtures/test";

test.describe("Internationalization", () => {
  test("English locale loads English copy on login", async ({ page }) => {
    await page.goto("/en/login");
    await expect(page.getByText(/sign in|welcome/i).first()).toBeVisible({ timeout: 10_000 });
  });

  test("Spanish locale loads Spanish copy on login", async ({ page }) => {
    await page.goto("/es/login");
    await expect(page.getByText(/iniciar sesi[oó]n|bienvenid/i).first()).toBeVisible({
      timeout: 10_000,
    });
  });

  test("locale prefix in URL is preserved across navigation", async ({ page }) => {
    await page.goto("/es/login");
    const registerLink = page.getByRole("link", { name: /reg[ií]strate|sign ?up/i }).first();
    if (await registerLink.isVisible().catch(() => false)) {
      await registerLink.click();
      await expect(page).toHaveURL(/\/es\/register/);
    }
  });
});
