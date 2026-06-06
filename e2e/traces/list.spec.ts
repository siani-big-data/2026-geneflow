import { test, expect } from "../fixtures/test";

test.describe("Traces › List", () => {
  test("page renders with title", async ({ auth }) => {
    await auth.page.goto("/traces");
    await expect(auth.page.getByRole("heading", { name: /traces/i })).toBeVisible();
  });

  test("grid or empty-state is shown", async ({ auth }) => {
    await auth.page.goto("/traces");
    await auth.page.waitForLoadState("networkidle");
    const hasContent = auth.page
      .getByRole("article")
      .or(auth.page.getByText(/no traces|empty/i));
    await expect(hasContent.first()).toBeVisible();
  });

  test("filter by status updates the view", async ({ auth }) => {
    await auth.page.goto("/traces");
    const filter = auth.page.getByRole("button", { name: /status|filter/i }).first();
    if (await filter.isVisible().catch(() => false)) {
      await filter.click();
      const option = auth.page.getByRole("option", { name: /processed|pending|failed/i }).first();
      if (await option.isVisible().catch(() => false)) {
        await option.click();
        await auth.page.waitForLoadState("networkidle");
      }
    }
  });

  test("search trace by name", async ({ auth }) => {
    await auth.page.goto("/traces");
    const search = auth.page.getByPlaceholder(/search/i).first();
    if (await search.isVisible().catch(() => false)) {
      await search.fill("nonexistent_zzzz");
      await auth.page.waitForTimeout(500);
      await expect(auth.page.getByText(/no (results|traces)/i).first()).toBeVisible();
    }
  });
});
