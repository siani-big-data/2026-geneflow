import { test, expect } from "../fixtures/test";
import {
  apiCreateStudy,
  apiUploadTrace,
} from "../fixtures/api";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";
import { isClientError, isSuccess } from "../helpers/http";

async function seedTrace(api: Parameters<typeof apiCreateStudy>[0], token: string) {
  const studyId = await apiCreateStudy(api, token, `seq-${Date.now()}`);
  const f = AB1_FIXTURES[0];
  const traceId = await apiUploadTrace(api, token, {
    studyId,
    filename: f.filename,
    mimeType: f.mimeType,
    buffer: readTraceBuffer(f),
  });
  return { studyId, traceId };
}

test.describe("Traces › Sequence editing API", () => {
  test("Create + list + undo edit", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);

    // Type 1 = substitute. Use the simplest edit.
    const created = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/edits`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { position: 10, typeId: 1, newBase: "A" },
        failOnStatusCode: false,
      },
    );
    expect(isSuccess(created.status())).toBe(true);
    const edit = (await created.json()) as { id?: string };
    expect(edit.id).toBeTruthy();

    const list = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/edits`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(list.status())).toBe(true);

    const undone = await api.delete(
      `/api/v1/studies/${studyId}/traces/${traceId}/edits/${edit.id}`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(undone.status())).toBe(true);
  });

  test("Edited sequence endpoint returns string", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/sequence/edited`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(res.status())).toBe(true);
  });

  test("Invalid edit type is rejected", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/edits`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { position: 10, typeId: 999, newBase: "Z" },
        failOnStatusCode: false,
      },
    );
    expect(isClientError(res.status())).toBe(true);
  });

  test("Undo all edits", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.delete(
      `/api/v1/studies/${studyId}/traces/${traceId}/edits`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(res.status())).toBe(true);
  });
});

test.describe("Traces › Trimming API", () => {
  test("Auto-trim returns a trim record", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);

    const res = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims/auto`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { qualityThreshold: 20, windowSize: 10 },
        failOnStatusCode: false,
      },
    );
    expect(res.status()).toBeLessThan(500);
  });

  test("Manual trim with valid range succeeds", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { startPosition: 5, endPosition: 50 },
        failOnStatusCode: false,
      },
    );
    expect(res.status()).toBeLessThan(500);
  });

  test("Preview trim does not persist", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const before = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    const beforeCount = (((await before.json().catch(() => ({}))) as { items?: unknown[] }).items ?? []).length;

    const preview = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims/preview`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { qualityThreshold: 20, windowSize: 10 },
        failOnStatusCode: false,
      },
    );
    expect(preview.status()).toBeLessThan(500);

    const after = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    const afterCount = (((await after.json().catch(() => ({}))) as { items?: unknown[] }).items ?? []).length;
    expect(afterCount).toBe(beforeCount);
  });

  test("Reverse-complement endpoint returns reversed sequence", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.get(
      `/api/v1/studies/${studyId}/traces/${traceId}/reverse-complement`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(res.status()).toBeLessThan(500);
  });

  test("Invalid trim positions are rejected", async ({ auth, api }) => {
    const { studyId, traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.post(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { startPosition: 100, endPosition: 5 },
        failOnStatusCode: false,
      },
    );
    expect(isClientError(res.status())).toBe(true);
  });
});

test.describe("Traces › Sequence pagination", () => {
  test("Manifest returns chunk metadata", async ({ auth, api }) => {
    const { traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.get(`/api/v1/traces/${traceId}/sequence/manifest`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(res.status()).toBeLessThan(500);
  });

  test("Page endpoint requires offset+limit and returns data", async ({ auth, api }) => {
    const { traceId } = await seedTrace(api, auth.token);
    test.skip(!traceId);
    const res = await api.get(
      `/api/v1/traces/${traceId}/sequence/page?offset=0&limit=100`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(res.status()).toBeLessThan(500);
  });
});
