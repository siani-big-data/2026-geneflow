import { test, expect } from "../fixtures/test";

test.describe("Auth › Verify Email", () => {
  test("shows error when no token provided", async ({ page }) => {
    await page.goto("/verify-email");
    await expect(page.getByText(/invalid|missing|token/i)).toBeVisible({ timeout: 10_000 });
  });

  test("shows success when token is valid (mocked)", async ({ page }) => {
    await page.route("**/api/v1/auth/verify-email*", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ verified: true }),
      });
    });
    await page.goto("/verify-email?token=valid-token");
    await expect(page.getByText(/verified|success/i)).toBeVisible({ timeout: 10_000 });
  });

  test("shows error for invalid token (mocked)", async ({ page }) => {
    await page.route("**/api/v1/auth/verify-email*", async (route) => {
      await route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ error: { code: "INVALID_TOKEN", message: "Invalid" } }),
      });
    });
    await page.goto("/verify-email?token=bad");
    await expect(page.getByText(/invalid|expired/i)).toBeVisible({ timeout: 10_000 });
  });
});
