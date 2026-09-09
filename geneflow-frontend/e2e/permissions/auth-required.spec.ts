import { test, expect } from "../fixtures/test";
import { apiCreateStudy } from "../fixtures/api";

/**
 * Every protected endpoint MUST refuse anonymous requests with 401.
 */
test.describe("Permissions › Anonymous access", () => {
  const protectedReads = [
    "/api/v1/studies/mine",
    "/api/v1/invitations",
    "/api/v1/profiles/me",
    "/api/v1/profiles/me/stats",
  ];

  for (const url of protectedReads) {
    test(`GET ${url} returns 401 without token`, async ({ api }) => {
      const res = await api.get(url, { failOnStatusCode: false });
      expect(res.status()).toBe(401);
    });
  }

  test("POST /studies (create) requires auth", async ({ api }) => {
    const res = await api.post("/api/v1/studies", {
      data: { name: "anon study" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });

  test("PUT /profiles/me requires auth", async ({ api }) => {
    const res = await api.put("/api/v1/profiles/me", {
      data: { firstName: "Anon" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });

  test("PUT/DELETE on an existing study require auth", async ({ api, auth }) => {
    const studyId = await apiCreateStudy(api, auth.token, "perm-anon-study");
    const put = await api.put(`/api/v1/studies/${studyId}`, {
      data: { name: "hacked" },
      failOnStatusCode: false,
    });
    expect(put.status()).toBe(401);
    const del = await api.delete(`/api/v1/studies/${studyId}`, {
      failOnStatusCode: false,
    });
    expect(del.status()).toBe(401);
  });

  test("Invalid bearer token is rejected", async ({ api }) => {
    const res = await api.get("/api/v1/studies/mine", {
      headers: { Authorization: "Bearer totally-fake-token.xyz.abc" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });

  test("Malformed Authorization header is rejected", async ({ api }) => {
    const res = await api.get("/api/v1/studies/mine", {
      headers: { Authorization: "NotBearer abc" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });

  test("Public endpoints DO allow anonymous", async ({ api }) => {
    const pub = await api.get("/api/v1/studies/public", { failOnStatusCode: false });
    // 200 if implemented, but never 401
    expect(pub.status()).not.toBe(401);
  });
});
