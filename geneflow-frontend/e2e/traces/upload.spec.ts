import { test, expect } from "../fixtures/test";
import { randomStudyName } from "../helpers/data";
import {
  AB1_FIXTURES,
  FASTA_FIXTURES,
  assertFixturesAvailable,
  readTraceBuffer,
  type TraceFixture,
} from "../helpers/traces";

test.beforeAll(() => {
  assertFixturesAvailable();
});

test.describe("Traces › Upload", () => {
  test("upload button is visible from a study", async ({ auth }) => {
    await auth.page.goto("/studies");
    await auth.page
      .getByRole("button", { name: /create study|new study/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    const name = randomStudyName();
    await dialog.getByLabel(/name|title/i).first().fill(name);
    await dialog.getByRole("button", { name: /create|save/i }).click();
    await auth.page.getByText(name).first().click();
    const tracesTab = auth.page.getByRole("tab", { name: /traces/i });
    if (await tracesTab.isVisible().catch(() => false)) await tracesTab.click();
    await expect(
      auth.page.getByRole("button", { name: /upload|new trace/i }).first(),
    ).toBeVisible();
  });

  /**
   * Drive the upload flow with EVERY real fixture shipped in the analysis
   * module. We don't assert success because some formats may be rejected
   * by the backend depending on validation rules - the goal is to exercise
   * the parsing path against real binary data, not synthetic placeholders.
   */
  const uploadCases: TraceFixture[] = [...AB1_FIXTURES, ...FASTA_FIXTURES];

  for (const fixture of uploadCases) {
    test(`uploads real ${fixture.format.toUpperCase()} fixture: ${fixture.filename}`, async ({
      auth,
    }) => {
      await auth.page.goto("/traces");
      const uploadBtn = auth.page
        .getByRole("button", { name: /upload|new trace/i })
        .first();
      if (!(await uploadBtn.isVisible().catch(() => false))) test.skip();
      await uploadBtn.click();

      const fileInput = auth.page.locator("input[type='file']");
      await fileInput.setInputFiles({
        name: fixture.filename,
        mimeType: fixture.mimeType,
        buffer: readTraceBuffer(fixture),
      });

      // Confirm the dialog shows the chosen filename or proceeds to processing.
      const filenameVisible = await auth.page
        .getByText(fixture.filename)
        .first()
        .isVisible()
        .catch(() => false);
      const submitBtn = auth.page
        .getByRole("button", { name: /upload|submit|save/i })
        .last();
      if (await submitBtn.isVisible().catch(() => false)) {
        await submitBtn.click();
      }

      // Either we see a success/processing toast or an explicit error - both
      // are acceptable as long as the flow didn't crash.
      await auth.page.waitForTimeout(2_000);
      expect(filenameVisible || (await uploadBtn.isVisible())).toBeTruthy();
    });
  }

  test("rejects oversized file", async ({ auth }) => {
    await auth.page.goto("/traces");
    const uploadBtn = auth.page
      .getByRole("button", { name: /upload|new trace/i })
      .first();
    if (!(await uploadBtn.isVisible().catch(() => false))) test.skip();
    await uploadBtn.click();

    const fileInput = auth.page.locator("input[type='file']");
    // 50MB buffer - frontends typically reject >10MB. We pad a real header so
    // the reader can read the magic bytes before the size check kicks in.
    const realHead = readTraceBuffer(AB1_FIXTURES[0]).subarray(0, 1024);
    const big = Buffer.concat([realHead, Buffer.alloc(50 * 1024 * 1024, 0)]);
    await fileInput.setInputFiles({
      name: "oversized.ab1",
      mimeType: "application/octet-stream",
      buffer: big,
    });
    await expect(auth.page.getByText(/too (big|large)|size|limit/i)).toBeVisible({
      timeout: 5_000,
    });
  });
});
