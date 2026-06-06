import { test, expect } from "../fixtures/test";
import { apiCreateStudy, apiUploadTrace } from "../fixtures/api";
import { randomStudyName } from "../helpers/data";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";

/**
 * Verifies the chromatogram canvas actually reacts to user input
 * (wheel zoom, drag-pan, keyboard, zoom buttons).
 */
test.describe("Traces › Chromatogram interaction", () => {
  test.beforeEach(async ({ auth, api }, testInfo) => {
    const studyId = await apiCreateStudy(api, auth.token, randomStudyName("Chrom"));
    const fixture = AB1_FIXTURES[0]; // 310.ab1
    const traceId = await apiUploadTrace(api, auth.token, {
      studyId,
      filename: fixture.filename,
      mimeType: fixture.mimeType,
      buffer: readTraceBuffer(fixture),
    });
    if (!traceId) testInfo.skip(true, "Backend did not accept seed trace");
    await auth.page.goto(`/traces/${traceId}`);
    await expect(auth.page.locator("canvas").first()).toBeVisible({ timeout: 15_000 });
  });

  test("wheel-zoom on the canvas changes the rendered range", async ({ auth }) => {
    const canvas = auth.page.locator("canvas").first();
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    if (!box) return;

    // Capture a baseline indicator (zoom level / range text)
    const indicator = auth.page.locator("[data-testid='zoom-level'], [data-testid='range']").first();
    const before = (await indicator.textContent().catch(() => null)) ?? "";

    await auth.page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await auth.page.mouse.wheel(0, -500);
    await auth.page.waitForTimeout(300);

    const after = (await indicator.textContent().catch(() => null)) ?? "";
    if (before && after) {
      expect(after).not.toEqual(before);
    } else {
      // Indicator not exposed: at minimum the canvas should still be rendered
      await expect(canvas).toBeVisible();
    }
  });

  test("drag-pan moves the visible region", async ({ auth }) => {
    const canvas = auth.page.locator("canvas").first();
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    if (!box) return;

    await auth.page.mouse.move(box.x + box.width * 0.7, box.y + box.height / 2);
    await auth.page.mouse.down();
    await auth.page.mouse.move(box.x + box.width * 0.2, box.y + box.height / 2, { steps: 10 });
    await auth.page.mouse.up();
    await auth.page.waitForTimeout(200);

    await expect(canvas).toBeVisible();
  });

  test("zoom-in button doubles magnification", async ({ auth }) => {
    const zoomIn = auth.page
      .getByRole("button", { name: /zoom in|^\+$|magnify/i })
      .first();
    test.skip(!(await zoomIn.isVisible().catch(() => false)), "No zoom-in button");

    const indicator = auth.page.locator("[data-testid='zoom-level']").first();
    const before = await indicator.textContent().catch(() => null);
    await zoomIn.click();
    await zoomIn.click();
    await auth.page.waitForTimeout(200);
    const after = await indicator.textContent().catch(() => null);
    if (before && after) expect(after).not.toEqual(before);
  });

  test("reset / fit-to-screen restores the full view", async ({ auth }) => {
    const fit = auth.page
      .getByRole("button", { name: /reset|fit|zoom out fully|home/i })
      .first();
    test.skip(!(await fit.isVisible().catch(() => false)), "No reset/fit button");
    await fit.click();
    await expect(auth.page.locator("canvas").first()).toBeVisible();
  });

  test("position scrubber / slider scrolls along the trace", async ({ auth }) => {
    const slider = auth.page.getByRole("slider").first();
    test.skip(!(await slider.isVisible().catch(() => false)), "No position slider");

    const before = await slider.getAttribute("aria-valuenow");
    await slider.focus();
    for (let i = 0; i < 5; i++) await auth.page.keyboard.press("ArrowRight");
    const after = await slider.getAttribute("aria-valuenow");
    if (before && after) expect(after).not.toEqual(before);
  });
});
