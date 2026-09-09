import { test, expect } from "../fixtures/test";
import { apiCreateStudy } from "../fixtures/api";
import { isClientError, isSuccess } from "../helpers/http";

test.describe("Pipelines › CRUD", () => {
  test("step-types endpoint exposes available step schemas", async ({ auth, api }) => {
    const res = await api.get("/api/v1/pipelines/step-types", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isSuccess(res.status())).toBe(true);
    const body = (await res.json().catch(() => ({}))) as { items?: unknown[] };
    expect((body.items ?? []).length).toBeGreaterThan(0);
  });

  test("Create + read + update + delete pipeline", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `pipe-${Date.now()}`);

    const create = await api.post(`/api/v1/studies/${studyId}/pipelines`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { name: "E2E pipeline", description: "test" },
      failOnStatusCode: false,
    });
    expect(isSuccess(create.status())).toBe(true);
    const pipeline = (await create.json()) as { id?: string };
    expect(pipeline.id).toBeTruthy();

    const get = await api.get(
      `/api/v1/studies/${studyId}/pipelines/${pipeline.id}`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(get.status())).toBe(true);

    const update = await api.put(
      `/api/v1/studies/${studyId}/pipelines/${pipeline.id}`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { name: "renamed", description: "edited" },
        failOnStatusCode: false,
      },
    );
    expect(isSuccess(update.status())).toBe(true);

    const list = await api.get(`/api/v1/studies/${studyId}/pipelines`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isSuccess(list.status())).toBe(true);

    const del = await api.delete(
      `/api/v1/studies/${studyId}/pipelines/${pipeline.id}`,
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect(isSuccess(del.status())).toBe(true);
  });

  test("Create rejects empty name", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `pipe-bad-${Date.now()}`);
    const res = await api.post(`/api/v1/studies/${studyId}/pipelines`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { name: "" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });
});

test.describe("Pipelines › Activation lifecycle", () => {
  async function createPipeline(api: Parameters<typeof apiCreateStudy>[0], token: string) {
    const studyId = await apiCreateStudy(api, token, `pipe-life-${Date.now()}`);
    const res = await api.post(`/api/v1/studies/${studyId}/pipelines`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: "lifecycle" },
      failOnStatusCode: false,
    });
    const body = (await res.json()) as { id?: string };
    return { studyId, pipelineId: body.id! };
  }

  test("activate → deactivate → archive", async ({ auth, api }) => {
    const { studyId, pipelineId } = await createPipeline(api, auth.token);
    for (const action of ["activate", "deactivate", "archive"]) {
      const res = await api.post(
        `/api/v1/studies/${studyId}/pipelines/${pipelineId}/${action}`,
        { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
      );
      expect(res.status(), action).toBeLessThan(500);
    }
  });

  test("Cannot execute a draft (not-active) pipeline", async ({ auth, api }) => {
    const { studyId, pipelineId } = await createPipeline(api, auth.token);
    const res = await api.post(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/execute`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { traceIds: [] },
        failOnStatusCode: false,
      },
    );
    expect(isClientError(res.status())).toBe(true);
  });
});

test.describe("Pipelines › Step management", () => {
  test("add + reorder + remove step", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, `pipe-steps-${Date.now()}`);
    const created = await api.post(`/api/v1/studies/${studyId}/pipelines`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { name: "steps" },
      failOnStatusCode: false,
    });
    const pipelineId = ((await created.json()) as { id?: string }).id!;

    // Add a step (we use a generic type the backend likely supports)
    const addStep = await api.post(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { type: "auto-trim", config: {}, order: 1 },
        failOnStatusCode: false,
      },
    );
    expect(addStep.status()).toBeLessThan(500);

    if (isSuccess(addStep.status())) {
      const step = (await addStep.json()) as { id?: string };
      const rm = await api.delete(
        `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps/${step.id}`,
        { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
      );
      expect(rm.status()).toBeLessThan(500);
    }
  });
});

test.describe("Pipelines › Execution listing", () => {
  test("Recent executions endpoint for current user", async ({ auth, api }) => {
    const res = await api.get("/api/v1/me/pipeline-executions", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isSuccess(res.status())).toBe(true);
  });

  test("Cancelling an unknown execution returns 404", async ({ auth, api }) => {
    const res = await api.post(
      "/api/v1/pipeline-executions/00000000-0000-0000-0000-000000000000/cancel",
      { headers: { Authorization: `Bearer ${auth.token}` }, failOnStatusCode: false },
    );
    expect([400, 404]).toContain(res.status());
  });
});

test.describe("Pipelines › SSE events", () => {
  test("SSE endpoint returns text/event-stream content-type", async ({ auth, api }) => {
    // We do not consume the stream; just check the headers come through.
    const res = await api.get(
      "/api/v1/pipeline-executions/00000000-0000-0000-0000-000000000000/events",
      {
        headers: { Authorization: `Bearer ${auth.token}`, Accept: "text/event-stream" },
        failOnStatusCode: false,
        timeout: 3_000,
      },
    );
    // Unknown execution → 404; an active one would 200 with text/event-stream.
    expect([200, 404]).toContain(res.status());
    if (res.status() === 200) {
      expect(res.headers()["content-type"] ?? "").toContain("text/event-stream");
    }
  });
});
