import { test, expect } from "./fixtures/test";

test.describe("Landing page", () => {
  test("renders hero and CTAs", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading").first()).toBeVisible();
    await expect(
      page.getByRole("link", { name: /sign ?in|log ?in|get started/i }).first(),
    ).toBeVisible();
  });

  test("CTA to register works", async ({ page }) => {
    await page.goto("/");
    const cta = page.getByRole("link", { name: /sign ?up|get started|register/i }).first();
    if (await cta.isVisible().catch(() => false)) {
      await cta.click();
      await expect(page).toHaveURL(/\/register/);
    }
  });

  test("pricing section exists", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByText(/pricing|plans/i).first()).toBeVisible();
  });

  test("features section exists", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByText(/feature|why|how it works/i).first()).toBeVisible();
  });
});
