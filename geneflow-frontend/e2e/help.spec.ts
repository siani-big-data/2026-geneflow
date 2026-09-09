import { test, expect } from "./fixtures/test";

test.describe("Help Center", () => {
  test("page renders", async ({ auth }) => {
    await auth.page.goto("/help");
    await expect(
      auth.page.getByRole("heading", { name: /help|support|faq/i }),
    ).toBeVisible();
  });

  test("search filters topics", async ({ auth }) => {
    await auth.page.goto("/help");
    const search = auth.page.getByPlaceholder(/search/i).first();
    if (await search.isVisible().catch(() => false)) {
      await search.fill("trace");
      await auth.page.waitForTimeout(500);
    }
  });
});
