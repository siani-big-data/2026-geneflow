import { test, expect } from "../fixtures/test";
import { uiLogout } from "../helpers/ui";

test.describe("Auth › Logout", () => {
  test("logged in user can log out and is redirected", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    await auth.page.waitForLoadState("networkidle");
    await uiLogout(auth.page);
    await expect(auth.page).toHaveURL(/\/(login)?$/);
  });

  test("after logout protected routes redirect to login", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    await uiLogout(auth.page);
    await auth.page.goto("/studies");
    await expect(auth.page).toHaveURL(/\/login/);
  });
});
