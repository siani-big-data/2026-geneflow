import { test, expect } from "../fixtures/test";
import {
  apiCreateStudy,
  apiGetBillingUsage,
  apiGetDashboardUsage,
  apiUploadTrace,
} from "../fixtures/api";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";
import { isSuccess } from "../helpers/http";

test.describe("Billing › Usage tracking", () => {
  test("GET /usage/billing returns a counters object for fresh user", async ({ auth, api }) => {
    const { status, body } = await apiGetBillingUsage(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    expect(body, "Body should not be null").not.toBeNull();
    expect(typeof body).toBe("object");
  });

  test("GET /usage/dashboard returns dashboard counters", async ({ auth, api }) => {
    const { status, body } = await apiGetDashboardUsage(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    const b = body as Record<string, unknown>;
    // Expected keys per the dashboard endpoint
    for (const key of ["activeStudies", "processedTraces", "pendingTraces"]) {
      expect(key in b, `dashboard missing ${key}`).toBe(true);
    }
  });

  test("Creating a study increments active-studies counter", async ({ auth, api }) => {
    const before = (await apiGetDashboardUsage(api, auth.token)).body as {
      activeStudies?: number;
    };
    await apiCreateStudy(api, auth.token, `usage-${Date.now()}`);
    const after = (await apiGetDashboardUsage(api, auth.token)).body as {
      activeStudies?: number;
    };
    expect((after.activeStudies ?? 0)).toBeGreaterThanOrEqual((before.activeStudies ?? 0) + 1);
  });

  test("Uploading a trace increments pending-traces counter", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `usage-trace-${Date.now()}`);
    const before = (await apiGetDashboardUsage(api, auth.token)).body as {
      pendingTraces?: number;
      processedTraces?: number;
    };
    const fixture = AB1_FIXTURES[0];
    const uploaded = await apiUploadTrace(api, auth.token, {
      studyId,
      filename: fixture.filename,
      mimeType: fixture.mimeType,
      buffer: readTraceBuffer(fixture),
    });
    test.skip(!uploaded, "Backend did not accept trace upload");
    const after = (await apiGetDashboardUsage(api, auth.token)).body as {
      pendingTraces?: number;
      processedTraces?: number;
    };
    const totalBefore = (before.pendingTraces ?? 0) + (before.processedTraces ?? 0);
    const totalAfter = (after.pendingTraces ?? 0) + (after.processedTraces ?? 0);
    expect(totalAfter).toBeGreaterThanOrEqual(totalBefore + 1);
  });

  test("Billing usage exposes period boundaries", async ({ auth, api }) => {
    const { body } = await apiGetBillingUsage(api, auth.token);
    const b = (body ?? {}) as Record<string, unknown>;
    // Tolerant: any of these field shapes signal a billable period
    const hasPeriod =
      "periodStart" in b ||
      "periodKey" in b ||
      "currentPeriodStart" in b ||
      "billingPeriodStart" in b;
    expect(hasPeriod, "Expected a period marker in billing usage").toBe(true);
  });

  test("Anonymous calls to usage endpoints return 401", async ({ api }) => {
    for (const url of ["/api/v1/usage/billing", "/api/v1/usage/dashboard"]) {
      const res = await api.get(url, { failOnStatusCode: false });
      expect(res.status(), url).toBe(401);
    }
  });

  test("Non-admins cannot hit admin usage endpoints", async ({ auth, api }) => {
    for (const path of [
      `/api/v1/admin/usage/increment/${auth.user.userId ?? "me"}`,
      `/api/v1/admin/usage/sync/${auth.user.userId ?? "me"}`,
      `/api/v1/admin/usage/init/${auth.user.userId ?? "me"}`,
    ]) {
      const res = await api.post(path, {
        headers: { Authorization: `Bearer ${auth.token}` },
        failOnStatusCode: false,
      });
      expect([401, 403, 404]).toContain(res.status());
    }
  });
});
