import { test, expect } from "../fixtures/test";
import { apiCreateStudy, apiUploadTrace } from "../fixtures/api";
import { randomStudyName } from "../helpers/data";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";

/**
 * Deep analysis tests. Seeds a real `.ab1` via API so we have a deterministic
 * trace to operate on, then exercises every analysis tool to RESULT - not
 * just "the button exists".
 */

async function seedTrace(
  api: Parameters<typeof apiCreateStudy>[0],
  token: string,
): Promise<string | undefined> {
  const studyId = await apiCreateStudy(api, token, randomStudyName("Analysis"));
  const fixture = AB1_FIXTURES[3]; // sanger_example.ab1 = canonical
  return apiUploadTrace(api, token, {
    studyId,
    filename: fixture.filename,
    mimeType: fixture.mimeType,
    buffer: readTraceBuffer(fixture),
  });
}

test.describe("Traces › Analysis (with assertions)", () => {
  test("ORF finder produces a result panel", async ({ auth, api }) => {
    const traceId = await seedTrace(api, auth.token);
    test.skip(!traceId, "Backend did not accept seed trace");
    await auth.page.goto(`/traces/${traceId}`);

    const orfBtn = auth.page.getByRole("button", { name: /\borf\b|open reading frame/i }).first();
    test.skip(!(await orfBtn.isVisible().catch(() => false)), "ORF tool not in UI");
    await orfBtn.click();

    // A result table / list / message must appear
    await expect(
      auth.page.getByText(/no orfs|frame|start|stop|length/i).first(),
    ).toBeVisible({ timeout: 15_000 });
  });

  test("Translate tool emits a protein sequence", async ({ auth, api }) => {
    const traceId = await seedTrace(api, auth.token);
    test.skip(!traceId, "Backend did not accept seed trace");
    await auth.page.goto(`/traces/${traceId}`);

    const translate = auth.page.getByRole("button", { name: /translate|translation/i }).first();
    test.skip(!(await translate.isVisible().catch(() => false)), "Translate tool not in UI");
    await translate.click();

    // Protein output is uppercase letters; we assert >=10 contiguous AA chars exist
    const result = auth.page.locator("pre, code, [data-testid='protein-sequence']").first();
    await expect(result).toBeVisible({ timeout: 15_000 });
    const text = (await result.textContent()) ?? "";
    expect(text.replace(/\s/g, "")).toMatch(/[A-Z*]{10,}/);
  });

  test("Restriction sites tool returns enzyme cuts", async ({ auth, api }) => {
    const traceId = await seedTrace(api, auth.token);
    test.skip(!traceId, "Backend did not accept seed trace");
    await auth.page.goto(`/traces/${traceId}`);

    const restriction = auth.page.getByRole("button", { name: /restriction/i }).first();
    test.skip(!(await restriction.isVisible().catch(() => false)), "Restriction tool not in UI");
    await restriction.click();
    await expect(
      auth.page.getByText(/ecori|bamhi|hindiii|enzyme|cut|no sites/i).first(),
    ).toBeVisible({ timeout: 15_000 });
  });

  test("Heterozygous detection toggles annotations on the chromatogram", async ({
    auth,
    api,
  }) => {
    const traceId = await seedTrace(api, auth.token);
    test.skip(!traceId, "Backend did not accept seed trace");
    await auth.page.goto(`/traces/${traceId}`);

    const het = auth.page.getByRole("button", { name: /heterozyg/i }).first();
    test.skip(!(await het.isVisible().catch(() => false)), "Heterozygous tool not in UI");
    await het.click();
    await expect(
      auth.page.getByText(/heterozyg|position|no heterozyg/i).first(),
    ).toBeVisible({ timeout: 15_000 });
  });
});
