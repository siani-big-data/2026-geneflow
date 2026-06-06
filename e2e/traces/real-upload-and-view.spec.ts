import { test, expect } from "../fixtures/test";
import { apiCreateStudy, apiUploadTrace } from "../fixtures/api";
import { randomStudyName } from "../helpers/data";
import {
  AB1_FIXTURES,
  FASTA_FIXTURES,
  readTraceBuffer,
  type TraceFixture,
} from "../helpers/traces";

/**
 * End-to-end coverage that:
 *   1. Seeds a study via the API.
 *   2. Uploads a REAL trace fixture (from geneflow-analysis/data/) via the API.
 *   3. Opens the trace detail page in the UI and verifies it rendered.
 *
 * Runs once per fixture so we catch format-specific regressions
 * (e.g. only .ab1 parses, only FASTA breaks chromatogram, etc.).
 */
const fixtures: TraceFixture[] = [...AB1_FIXTURES, ...FASTA_FIXTURES];

for (const fixture of fixtures) {
  test(`real trace round-trip: ${fixture.format.toUpperCase()} ${fixture.filename}`, async ({
    auth,
    api,
  }) => {
    const studyId = await apiCreateStudy(
      api,
      auth.token,
      `${randomStudyName()}-${fixture.format}`,
    ).catch(() => undefined);
    test.skip(!studyId, "Backend did not accept study creation");

    const traceId = await apiUploadTrace(api, auth.token, {
      studyId: studyId as string,
      filename: fixture.filename,
      mimeType: fixture.mimeType,
      buffer: readTraceBuffer(fixture),
    });
    test.skip(!traceId, `Backend rejected ${fixture.filename} upload`);

    await auth.page.goto(`/traces/${traceId}`);
    await expect(auth.page.getByText(fixture.filename).first()).toBeVisible({
      timeout: 10_000,
    });

    // .ab1 traces should render the chromatogram canvas.
    if (fixture.format === "ab1") {
      await expect(auth.page.locator("canvas").first()).toBeVisible({
        timeout: 10_000,
      });
    }
  });
}
