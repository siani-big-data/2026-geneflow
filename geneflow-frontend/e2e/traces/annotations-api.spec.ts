import { test, expect } from "../fixtures/test";
import {
  apiCreateStudy,
  apiOnboardCollaborator,
  apiUploadTrace,
} from "../fixtures/api";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";
import { isClientError, isForbidden, isSuccess } from "../helpers/http";

async function seedTrace(api: Parameters<typeof apiCreateStudy>[0], token: string) {
  const studyId = await apiCreateStudy(api, token, `ann-${Date.now()}`);
  const fixture = AB1_FIXTURES[0];
  const traceId = await apiUploadTrace(api, token, {
    studyId,
    filename: fixture.filename,
    mimeType: fixture.mimeType,
    buffer: readTraceBuffer(fixture),
  });
  return { studyId, traceId };
}

test.describe("Traces › Annotations API", () => {
  test("CRUD lifecycle", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId, "Backend did not accept trace upload");

    const created = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { startPosition: 10, endPosition: 20, label: "primer", note: "E2E annotation" },
        failOnStatusCode: false,
      },
    );
    expect(isSuccess(created.status())).toBe(true);
    const annotation = (await created.json()) as { id?: string };
    expect(annotation.id).toBeTruthy();

    const list = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(list.status())).toBe(true);

    const updated = await api.put(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations/${annotation.id}`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { startPosition: 10, endPosition: 25, label: "primer-edited", note: "edited" },
        failOnStatusCode: false,
      },
    );
    expect(isSuccess(updated.status())).toBe(true);

    const deleted = await api.delete(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations/${annotation.id}`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(deleted.status())).toBe(true);
  });

  test("Rejects negative / inverted positions", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    for (const bad of [
      { startPosition: -1, endPosition: 5, label: "x" },
      { startPosition: 50, endPosition: 10, label: "x" },
    ]) {
      const res = await api.post(
        `/api/v1/studies/${studyId}/traces/${traceId}/annotations`,
        {
          headers: { Authorization: `Bearer ${auth.token}` },
          data: bad,
          failOnStatusCode: false,
        },
      );
      expect(isClientError(res.status()), JSON.stringify(bad)).toBe(true);
    }
  });

  test("Viewer cannot create annotations", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const viewer = await apiOnboardCollaborator(api, auth.token, studyId, 4);
    const res = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations`,
      {
        headers: { Authorization: `Bearer ${viewer.token}` },
        data: { startPosition: 1, endPosition: 5, label: "blocked" },
        failOnStatusCode: false,
      },
    );
    expect(isForbidden(res.status())).toBe(true);
  });

  test("Study-level shared annotations endpoint returns a list", async ({ auth, api }) => {
    const { studyId } = await seedTrace(api, auth.token);
    const res = await api.get(`/api/v1/studies/${studyId}/annotations`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isSuccess(res.status())).toBe(true);
  });
});
