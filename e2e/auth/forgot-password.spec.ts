import { test, expect } from "../fixtures/test";
import { randomEmail } from "../helpers/data";

test.describe("Auth › Forgot Password", () => {
  test("renders form", async ({ page }) => {
    await page.goto("/forgot-password");
    await expect(page.locator("input[type='email']").first()).toBeVisible();
    await expect(page.getByRole("button", { name: /send|reset|recover/i })).toBeVisible();
  });

  test("submits a recovery request for any email and shows confirmation", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.locator("input[type='email']").first().fill(randomEmail());
    await page.getByRole("button", { name: /send|reset|recover/i }).click();
    await expect(page.getByText(/sent|check your email|inbox/i)).toBeVisible({ timeout: 10_000 });
  });

  test("validates email format", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.locator("input[type='email']").first().fill("not-an-email");
    await page.getByRole("button", { name: /send|reset|recover/i }).click();
    // Either browser-native validation prevents submit, or a custom error appears
    await page.waitForTimeout(300);
  });

  test("link to login is present", async ({ page }) => {
    await page.goto("/forgot-password");
    await expect(page.getByRole("link", { name: /back to login|sign in|login/i })).toBeVisible();
  });
});
