import { test, expect } from "../fixtures/test";
import { apiListExternalLogins } from "../fixtures/api";
import { isClientError, isSuccess } from "../helpers/http";

test.describe("Security › External logins", () => {
  test("Fresh user has no linked OAuth providers", async ({ auth, api }) => {
    const { status, items } = await apiListExternalLogins(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    expect(items.length).toBe(0);
  });

  test("Cannot unlink a provider that isn't linked", async ({ auth, api }) => {
    const res = await api.delete("/api/v1/users/external-logins/Google", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Linking with bogus provider data fails", async ({ auth, api }) => {
    const res = await api.post("/api/v1/users/external-logins", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { provider: "Google", code: "totally-fake-oauth-code" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Linking requires a supported provider name", async ({ auth, api }) => {
    const res = await api.post("/api/v1/users/external-logins", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { provider: "FakeProvider", code: "x" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Anonymous cannot read/link external logins", async ({ api }) => {
    expect(
      (await api.get("/api/v1/users/external-logins", { failOnStatusCode: false })).status(),
    ).toBe(401);
    expect(
      (
        await api.post("/api/v1/users/external-logins", {
          data: { provider: "Google", code: "x" },
          failOnStatusCode: false,
        })
      ).status(),
    ).toBe(401);
  });
});
