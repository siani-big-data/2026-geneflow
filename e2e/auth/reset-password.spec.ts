import { test, expect } from "../fixtures/test";
import { strongPassword } from "../helpers/data";

test.describe("Auth › Reset Password", () => {
  test("requires a token in the URL", async ({ page }) => {
    await page.goto("/reset-password");
    await expect(page.getByText(/invalid|missing|token|expired/i).first()).toBeVisible({ timeout: 10_000 });
  });

  test("renders form when token is present", async ({ page }) => {
    await page.goto("/reset-password?token=fake-reset-token");
    await expect(page.locator("input[type='password']").first()).toBeVisible();
  });

  test("rejects mismatched new passwords", async ({ page }) => {
    await page.goto("/reset-password?token=fake-reset-token");
    const inputs = page.locator("input[type='password']");
    await inputs.nth(0).fill(strongPassword);
    await inputs.nth(1).fill("DifferentPass1!");
    await page.getByRole("button", { name: /reset|update|change/i }).click();
    await expect(page.getByText(/match|do not match/i)).toBeVisible();
  });

  test("shows error for invalid/expired token", async ({ page }) => {
    await page.route("**/api/v1/auth/reset-password*", async (route) => {
      await route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ error: { code: "INVALID_TOKEN", message: "Token expired" } }),
      });
    });
    await page.goto("/reset-password?token=expired");
    const inputs = page.locator("input[type='password']");
    await inputs.nth(0).fill(strongPassword);
    await inputs.nth(1).fill(strongPassword);
    await page.getByRole("button", { name: /reset|update|change/i }).click();
    await expect(page.getByText(/expired|invalid/i).first()).toBeVisible({ timeout: 10_000 });
  });
});
