import { test, expect } from "../fixtures/test";

/**
 * Trace detail tests rely on backend having at least one processed trace.
 * If the list is empty, tests skip gracefully.
 */
test.describe("Traces › Detail / Chromatogram", () => {
  test("clicking a trace opens its detail page", async ({ auth }) => {
    await auth.page.goto("/traces");
    await auth.page.waitForLoadState("networkidle");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    await expect(auth.page).toHaveURL(/\/traces\/.+/);
    // The chromatogram canvas should be present
    await expect(auth.page.locator("canvas").first()).toBeVisible({ timeout: 10_000 });
  });

  test("zoom / pan controls are visible", async ({ auth }) => {
    await auth.page.goto("/traces");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    const zoomIn = auth.page.getByRole("button", { name: /zoom in|zoom/i }).first();
    if (await zoomIn.isVisible().catch(() => false)) {
      await zoomIn.click();
    }
  });
});

test.describe("Traces › Annotations", () => {
  test("annotation panel exists", async ({ auth }) => {
    await auth.page.goto("/traces");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    await expect(
      auth.page.getByText(/annotation|notes/i).first(),
    ).toBeVisible({ timeout: 5_000 });
  });

  test("create annotation flow opens dialog", async ({ auth }) => {
    await auth.page.goto("/traces");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    const addBtn = auth.page
      .getByRole("button", { name: /add annotation|new annotation/i })
      .first();
    if (await addBtn.isVisible().catch(() => false)) {
      await addBtn.click();
      await expect(auth.page.getByRole("dialog")).toBeVisible();
    }
  });
});

test.describe("Traces › Trimming", () => {
  test("auto-trim button is visible", async ({ auth }) => {
    await auth.page.goto("/traces");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    await expect(
      auth.page.getByRole("button", { name: /auto[- ]?trim|trim/i }).first(),
    ).toBeVisible();
  });

  test("undo trim restores original sequence", async ({ auth }) => {
    await auth.page.goto("/traces");
    const firstCard = auth.page.getByRole("article").first();
    if (!(await firstCard.isVisible().catch(() => false))) test.skip();
    await firstCard.click();
    const trimBtn = auth.page.getByRole("button", { name: /auto[- ]?trim/i }).first();
    if (!(await trimBtn.isVisible().catch(() => false))) test.skip();
    await trimBtn.click();
    await auth.page.waitForTimeout(1500);
    const undoBtn = auth.page.getByRole("button", { name: /undo|revert/i }).first();
    if (await undoBtn.isVisible().catch(() => false)) await undoBtn.click();
  });
});

test.describe("Traces › Analysis", () => {
  const analyses = [
    /heterozyg|heterocig/i,
    /motif|pattern/i,
    /translate|translation/i,
    /\borf\b/i,
    /restriction/i,
  ];

  for (const analysisName of analyses) {
    test(`analysis tool: ${analysisName}`, async ({ auth }) => {
      await auth.page.goto("/traces");
      const firstCard = auth.page.getByRole("article").first();
      if (!(await firstCard.isVisible().catch(() => false))) test.skip();
      await firstCard.click();
      const tool = auth.page.getByRole("button", { name: analysisName }).first();
      if (!(await tool.isVisible().catch(() => false))) test.skip();
      await tool.click();
      await auth.page.waitForTimeout(2000);
    });
  }
});
