import { test, expect } from "./fixtures/test";

test.describe("Discover", () => {
  test("page renders", async ({ auth }) => {
    await auth.page.goto("/discover");
    await expect(
      auth.page.getByRole("heading", { name: /discover|explore/i }),
    ).toBeVisible();
  });

  test("public studies grid or empty state", async ({ auth }) => {
    await auth.page.goto("/discover");
    await auth.page.waitForLoadState("networkidle");
    await expect(
      auth.page.getByRole("article").or(auth.page.getByText(/no (public )?studies/i)).first(),
    ).toBeVisible();
  });
});
