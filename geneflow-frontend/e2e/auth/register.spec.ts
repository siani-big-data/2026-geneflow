import { test, expect } from "../fixtures/test";
import { randomEmail, randomUsername, strongPassword, weakPassword } from "../helpers/data";
import { gotoRegister } from "../helpers/ui";

test.describe("Auth › Register", () => {
  test("renders form and shows OAuth options", async ({ page }) => {
    await gotoRegister(page);
    await expect(page.locator("#username")).toBeVisible();
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(page.locator("#confirmPassword")).toBeVisible();
    await expect(page.getByRole("button", { name: /sign up with google/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /sign up with github/i })).toBeVisible();
  });

  test("registers a new user with valid credentials", async ({ page }) => {
    const email = randomEmail();
    const username = randomUsername();
    await gotoRegister(page);
    await page.locator("#username").fill(username);
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill(strongPassword);
    await page.locator("#acceptTerms").check();
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(email)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole("button", { name: /back to login/i })).toBeVisible();
  });

  test("shows validation errors for empty submit", async ({ page }) => {
    await gotoRegister(page);
    await page.locator("#acceptTerms").check();
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(/username must be at least/i)).toBeVisible();
    await expect(page.getByText(/email/i).first()).toBeVisible();
  });

  test("rejects weak password and reveals requirements list", async ({ page }) => {
    await gotoRegister(page);
    await page.locator("#password").fill(weakPassword);
    await expect(page.getByText(/at least 8 characters|at least 12 characters/i)).toBeVisible();
    await expect(page.getByText(/contains uppercase/i)).toBeVisible();
  });

  test("rejects mismatching passwords", async ({ page }) => {
    await gotoRegister(page);
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill("DifferentPass1!");
    await expect(page.getByText(/passwords.*match|match/i)).toBeVisible();
  });

  test("requires accepting terms before submit", async ({ page }) => {
    await gotoRegister(page);
    await page.locator("#username").fill(randomUsername());
    await page.locator("#email").fill(randomEmail());
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill(strongPassword);
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(/terms/i)).toBeVisible();
  });

  test("rejects duplicate email", async ({ page, user }) => {
    await gotoRegister(page);
    await page.locator("#username").fill(randomUsername());
    await page.locator("#email").fill(user.email);
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill(strongPassword);
    await page.locator("#acceptTerms").check();
    await page.locator('button[type="submit"]').click();
    await expect(page.getByText(/already (exists|registered|in use)/i)).toBeVisible({ timeout: 10_000 });
  });

  test("resend verification email button cycles state", async ({ page }) => {
    const email = randomEmail();
    await gotoRegister(page);
    await page.locator("#username").fill(randomUsername());
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill(strongPassword);
    await page.locator("#acceptTerms").check();
    await page.locator('button[type="submit"]').click();
    const resend = page.getByRole("button", { name: /resend/i });
    await expect(resend).toBeVisible();
    await resend.click();
    await expect(page.getByText(/sent|sending/i)).toBeVisible();
  });

  test("'Try again' link goes back to the register form", async ({ page }) => {
    const email = randomEmail();
    await gotoRegister(page);
    await page.locator("#username").fill(randomUsername());
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(strongPassword);
    await page.locator("#confirmPassword").fill(strongPassword);
    await page.locator("#acceptTerms").check();
    await page.locator('button[type="submit"]').click();
    await page.getByRole("button", { name: /try again/i }).click();
    await expect(page.locator("#username")).toBeVisible();
  });
});
