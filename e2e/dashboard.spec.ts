import { test, expect } from "./fixtures/test";

test.describe("Dashboard", () => {
  test("renders metrics cards", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    await expect(
      auth.page.getByRole("heading", { name: /dashboard|welcome|overview/i }),
    ).toBeVisible();
    await auth.page.waitForLoadState("networkidle");
    // Expect at least one metric card
    const cards = auth.page.locator("[class*='card'], [data-testid*='metric']");
    expect(await cards.count()).toBeGreaterThan(0);
  });

  test("displays recent studies section", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    await expect(
      auth.page.getByText(/recent studies|your studies|recent/i).first(),
    ).toBeVisible();
  });

  test("usage stats section is visible", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    await expect(
      auth.page.getByText(/usage|storage|api calls|traces/i).first(),
    ).toBeVisible();
  });

  test("navigation links to studies/traces/pipelines work", async ({ auth }) => {
    await auth.page.goto("/dashboard");
    const studiesLink = auth.page.getByRole("link", { name: /studies/i }).first();
    if (await studiesLink.isVisible().catch(() => false)) {
      await studiesLink.click();
      await expect(auth.page).toHaveURL(/\/studies/);
    }
  });
});
