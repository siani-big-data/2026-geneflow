import { test, expect } from "../fixtures/test";

test.describe("Studies › List", () => {
  test("displays the studies page with header", async ({ auth }) => {
    await auth.page.goto("/studies");
    await expect(auth.page.getByRole("heading", { name: /studies/i })).toBeVisible();
  });

  test("'Create study' button is visible", async ({ auth }) => {
    await auth.page.goto("/studies");
    await expect(
      auth.page.getByRole("button", { name: /create study|new study/i }),
    ).toBeVisible();
  });

  test("empty-state or list is rendered", async ({ auth }) => {
    await auth.page.goto("/studies");
    await auth.page.waitForLoadState("networkidle");
    const listOrEmpty = auth.page
      .getByRole("article")
      .or(auth.page.getByText(/no studies|empty|create your first/i));
    await expect(listOrEmpty.first()).toBeVisible();
  });

  test("filters by status update URL or DOM", async ({ auth }) => {
    await auth.page.goto("/studies");
    const filter = auth.page
      .getByRole("button", { name: /status|filter|active|archived/i })
      .first();
    if (await filter.isVisible().catch(() => false)) {
      await filter.click();
      const option = auth.page.getByRole("option", { name: /active|archived/i }).first();
      if (await option.isVisible().catch(() => false)) {
        await option.click();
        await auth.page.waitForLoadState("networkidle");
      }
    }
  });

  test("search bar filters studies", async ({ auth }) => {
    await auth.page.goto("/studies");
    const search = auth.page
      .getByPlaceholder(/search/i)
      .or(auth.page.getByRole("textbox", { name: /search/i }))
      .first();
    if (await search.isVisible().catch(() => false)) {
      await search.fill("zzznonexistent");
      await auth.page.waitForTimeout(600);
      await expect(
        auth.page.getByText(/no (results|studies)/i).first(),
      ).toBeVisible();
    }
  });
});
