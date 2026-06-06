import { test, expect } from "../fixtures/test";
import { randomEmail, strongPassword } from "../helpers/data";
import { gotoLogin, uiLogin } from "../helpers/ui";

test.describe("Auth › Login", () => {
  test("renders form fields and links", async ({ page }) => {
    await gotoLogin(page);
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(page.locator("#rememberMe")).toBeVisible();
    await expect(page.getByRole("link", { name: /forgot password/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /sign ?up/i })).toBeVisible();
  });

  test("logs a registered user in and lands on dashboard", async ({ page, user }) => {
    await uiLogin(page, user.email, user.password);
    await expect(page).toHaveURL(/\/dashboard/);
  });

  test("rejects unknown credentials with error alert", async ({ page }) => {
    await gotoLogin(page);
    await page.locator("#email").fill(randomEmail());
    await page.locator("#password").fill(strongPassword);
    await page.locator('button[type="submit"]').click();
    await expect(page.getByRole("alert").or(page.getByText(/invalid|incorrect|not found/i))).toBeVisible({
      timeout: 10_000,
    });
  });

  test("rejects wrong password for existing user", async ({ page, user }) => {
    await gotoLogin(page);
    await page.locator("#email").fill(user.email);
    await page.locator("#password").fill("WrongPass!1");
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(/invalid|incorrect|wrong/i)).toBeVisible({ timeout: 10_000 });
  });

  test("validates email format on submit", async ({ page }) => {
    await gotoLogin(page);
    await page.locator("#email").fill("not-an-email");
    await page.locator("#password").fill(strongPassword);
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(/email/i)).toBeVisible();
  });

  test("password visibility toggle works", async ({ page }) => {
    await gotoLogin(page);
    const pw = page.locator("#password");
    await pw.fill("secret");
    await expect(pw).toHaveAttribute("type", "password");
    await page.getByRole("button", { name: /show password|hide password/i }).click();
    await expect(pw).toHaveAttribute("type", "text");
  });

  test("'Forgot password' link navigates correctly", async ({ page }) => {
    await gotoLogin(page);
    await page.getByRole("link", { name: /forgot password/i }).click();
    await expect(page).toHaveURL(/\/forgot-password/);
  });

  test("'Sign up' link navigates to register", async ({ page }) => {
    await gotoLogin(page);
    await page.getByRole("link", { name: /sign ?up/i }).click();
    await expect(page).toHaveURL(/\/register/);
  });

  test("redirects to returnUrl after successful login", async ({ page, user }) => {
    await page.goto("/login?returnUrl=%2Fstudies");
    await page.locator("#email").fill(user.email);
    await page.locator("#password").fill(user.password);
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(/\/studies/);
  });

  test("already authenticated user is redirected away from /login", async ({ auth }) => {
    await auth.page.goto("/login");
    await expect(auth.page).toHaveURL(/\/dashboard/);
  });
});

test.describe("Auth › 2FA", () => {
  test("shows 2FA screen when account requires it", async ({ page }) => {
    // Intercept login response to simulate 2FA required
    await page.route("**/api/v1/auth/login", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ requiresTwoFactor: true, twoFactorMethod: "email" }),
      });
    });
    await gotoLogin(page);
    await page.locator("#email").fill("twofa@geneflow.test");
    await page.locator("#password").fill(strongPassword);
    await page.locator('button[type="submit"]').click();
    await expect(page.locator("#twoFactorCode")).toBeVisible();
    await expect(page.getByRole("button", { name: /verify/i })).toBeVisible();
  });

  test("rejects malformed 2FA code", async ({ page }) => {
    await page.route("**/api/v1/auth/login", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ requiresTwoFactor: true }),
      });
    });
    await gotoLogin(page);
    await page.locator("#email").fill("twofa@geneflow.test");
    await page.locator("#password").fill(strongPassword);
    await page.locator('button[type="submit"]').click();
    await page.locator("#twoFactorCode").fill("12");
    await page.getByRole("button", { name: /verify/i }).click();
    await expect(page.getByText(/invalid|code/i)).toBeVisible();
  });
});
