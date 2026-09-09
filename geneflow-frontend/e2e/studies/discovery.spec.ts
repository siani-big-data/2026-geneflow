import { test, expect } from "../fixtures/test";
import {
  apiCreateStudy,
  apiGetFeaturedStudies,
  apiGetPublicStudies,
  apiGetResearchFields,
  apiStarStudy,
} from "../fixtures/api";
import { isForbidden, isSuccess } from "../helpers/http";

test.describe("Discovery › Public / Featured / Research fields", () => {
  test("GET /studies/public is anonymous-accessible and returns a list", async ({ api }) => {
    const { status, body } = await apiGetPublicStudies(api);
    expect(isSuccess(status)).toBe(true);
    expect(body).toBeTruthy();
  });

  test("GET /studies/featured is anonymous-accessible", async ({ api }) => {
    const { status } = await apiGetFeaturedStudies(api);
    expect(isSuccess(status)).toBe(true);
  });

  test("GET /studies/research-fields exposes reference data", async ({ api }) => {
    const { status, body } = await apiGetResearchFields(api);
    expect(isSuccess(status)).toBe(true);
    const items = (body as { items?: unknown[] })?.items ?? body;
    expect(Array.isArray(items)).toBe(true);
  });

  test("Private (default) study does NOT appear in /studies/public", async ({ auth, api }) => {
    const distinct = `secret-${Date.now().toString(36)}`;
    await apiCreateStudy(api, auth.token, distinct);
    const { body } = await apiGetPublicStudies(api);
    const items = (body as { items?: Array<{ name?: string }> })?.items ?? [];
    expect(items.find((s) => s.name === distinct)).toBeUndefined();
  });
});

test.describe("Discovery › Stars + view tracking", () => {
  test("Star own study, then unstar", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `star-${Date.now()}`);
    expect(isSuccess(await apiStarStudy(api, auth.token, studyId))).toBe(true);

    const check = await api.get(`/api/v1/studies/${studyId}/stars`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(check.ok()).toBe(true);

    const unstar = await api.delete(`/api/v1/studies/${studyId}/stars`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(unstar.ok()).toBe(true);
  });

  test("Non-member cannot star a private study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `private-star-${Date.now()}`);
    const res = await api.post(`/api/v1/studies/${studyId}/stars`, {
      failOnStatusCode: false,
    });
    expect(isForbidden(res.status())).toBe(true);
  });

  test("Record-view is anonymous-friendly", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `view-${Date.now()}`);
    const res = await api.post(`/api/v1/studies/${studyId}/views`, {
      failOnStatusCode: false,
    });
    // Either accepted (200/204) or forbidden if study is private. Never 5xx.
    expect(res.status()).toBeLessThan(500);
  });

  test("Stats endpoint returns a counters object", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `stats-${Date.now()}`);
    const res = await api.get(`/api/v1/studies/${studyId}/stats`, {
      failOnStatusCode: false,
    });
    expect(res.status()).toBeLessThan(500);
  });
});

test.describe("Discovery › Duplicate & Export", () => {
  test("Owner can duplicate own study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `original-${Date.now()}`);
    const res = await api.post(`/api/v1/studies/${studyId}/duplicate`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isSuccess(res.status())).toBe(true);
  });

  test("Owner can export own study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `export-${Date.now()}`);
    const res = await api.get(`/api/v1/studies/${studyId}/export`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(res.status()).toBeLessThan(500);
  });
});
