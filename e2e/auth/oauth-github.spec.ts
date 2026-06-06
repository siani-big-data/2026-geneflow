import { test, expect } from "../fixtures/test";
import { gotoLogin } from "../helpers/ui";

/**
 * OAuth GitHub is intrinsically external. We intercept the redirect to
 * github.com and simulate the provider returning a code to our callback.
 */
test.describe("Auth › OAuth GitHub", () => {
  test("clicking GitHub button initiates OAuth flow", async ({ page }) => {
    await gotoLogin(page);

    // Intercept the external redirect to GitHub
    const redirectPromise = page.waitForRequest(
      (req) => /github\.com\/login\/oauth\/authorize/.test(req.url()),
      { timeout: 10_000 },
    );
    await page.getByRole("button", { name: /sign in with github/i }).click();
    const req = await redirectPromise;
    expect(req.url()).toContain("client_id=");
    expect(req.url()).toContain("redirect_uri=");
  });

  test("callback page handles missing code parameter gracefully", async ({ page }) => {
    await page.goto("/auth/callback/github");
    await expect(page.getByText(/error|invalid|missing/i)).toBeVisible({ timeout: 10_000 });
  });

  test("callback page processes successful OAuth response", async ({ page, user, api }) => {
    // Mock the backend OAuth exchange so it returns our test user's session
    await page.route("**/api/v1/auth/oauth/**", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: "fake-token",
          user: { email: user.email, username: user.username },
        }),
      });
    });
    await page.goto("/auth/callback/github?code=mock_oauth_code&state=test");
    // Either redirect to dashboard, or to complete-profile for new OAuth users
    await page.waitForURL(/\/(dashboard|complete-profile)/, { timeout: 15_000 });
    void api;
  });

  test("OAuth GitHub button is visible on register page too", async ({ page }) => {
    await page.goto("/register");
    await expect(
      page.getByRole("button", { name: /sign up with github|sign in with github/i }),
    ).toBeVisible();
  });
});

test.describe("Auth › OAuth Google", () => {
  test("clicking Google button starts OAuth (no provider crash)", async ({ page }) => {
    await gotoLogin(page);
    const button = page.getByRole("button", { name: /sign in with google/i });
    await expect(button).toBeVisible();
    // Either redirects to accounts.google.com or shows a "not configured" alert.
    await button.click();
    // The test passes if the button click doesn't throw a runtime exception.
    await page.waitForTimeout(500);
  });
});
