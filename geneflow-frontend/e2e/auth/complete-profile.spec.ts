import { test, expect } from "../fixtures/test";

test.describe("Auth › Complete Profile (post-OAuth)", () => {
  test("redirects unauthenticated users to login", async ({ page }) => {
    await page.goto("/complete-profile");
    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
  });

  test("authenticated user sees the profile completion form", async ({ auth }) => {
    await auth.page.goto("/complete-profile");
    // Form should have at least name / surname / phone fields
    const fields = auth.page.locator("input:not([type='hidden'])");
    await expect(fields.first()).toBeVisible({ timeout: 10_000 });
    await expect(auth.page.getByRole("button", { name: /save|continue|complete/i })).toBeVisible();
  });
});
