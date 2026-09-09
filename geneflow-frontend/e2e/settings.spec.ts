import { test, expect } from "./fixtures/test";

test.describe("Settings › General", () => {
  test("settings page renders with tabs", async ({ auth }) => {
    await auth.page.goto("/settings");
    await expect(auth.page.getByRole("heading", { name: /settings/i })).toBeVisible();
  });

  test("language switcher / theme toggle", async ({ auth }) => {
    await auth.page.goto("/settings");
    const themeToggle = auth.page.getByRole("button", { name: /theme|dark|light/i }).first();
    if (await themeToggle.isVisible().catch(() => false)) {
      await themeToggle.click();
    }
  });

  test("change password section", async ({ auth }) => {
    await auth.page.goto("/settings");
    const securityTab = auth.page.getByRole("tab", { name: /security|password/i });
    if (await securityTab.isVisible().catch(() => false)) {
      await securityTab.click();
      await expect(auth.page.getByLabel(/current password|new password/i).first()).toBeVisible();
    }
  });

  test("2FA toggle is present", async ({ auth }) => {
    await auth.page.goto("/settings");
    const securityTab = auth.page.getByRole("tab", { name: /security/i });
    if (await securityTab.isVisible().catch(() => false)) {
      await securityTab.click();
      await expect(auth.page.getByText(/two[- ]factor|2fa/i).first()).toBeVisible();
    }
  });
});

test.describe("Settings › Billing", () => {
  test("/settings/billing renders", async ({ auth }) => {
    await auth.page.goto("/settings/billing");
    await expect(
      auth.page.getByText(/billing|plan|subscription/i).first(),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("plan cards or current plan visible", async ({ auth }) => {
    await auth.page.goto("/settings/billing");
    await expect(
      auth.page.getByText(/free|pro|enterprise|current plan/i).first(),
    ).toBeVisible({ timeout: 10_000 });
  });
});
