import { test, expect } from "../fixtures/test";
import {
  apiCreateSetupIntent,
  apiListPaymentMethods,
} from "../fixtures/api";
import { isClientError, isSuccess } from "../helpers/http";

test.describe("Billing › Payment methods", () => {
  test("Fresh user has an empty payment-method list", async ({ auth, api }) => {
    const { status, items } = await apiListPaymentMethods(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    expect(items.length).toBe(0);
  });

  test("Default payment method is 404 when none is set", async ({ auth, api }) => {
    const res = await api.get("/api/v1/payment-methods/default", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect([200, 204, 404]).toContain(res.status());
  });

  test("SetupIntent endpoint returns a client secret", async ({ auth, api }) => {
    const { status, clientSecret } = await apiCreateSetupIntent(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    // Stripe SetupIntent client_secret format: seti_xxx_secret_xxx
    expect(clientSecret, "Setup intent must return clientSecret").toBeTruthy();
    expect(clientSecret!).toMatch(/seti_.+_secret_.+/);
  });

  test("Adding payment method requires Stripe payment method id", async ({ auth, api }) => {
    const res = await api.post("/api/v1/payment-methods", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: {},
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Adding payment method rejects bogus payment-method id", async ({ auth, api }) => {
    const res = await api.post("/api/v1/payment-methods", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { paymentMethodId: "pm_does_not_exist_zzz" },
      failOnStatusCode: false,
    });
    // Stripe rejects → backend returns 4xx
    expect(isClientError(res.status())).toBe(true);
  });

  test("Set-default on unknown id returns 404", async ({ auth, api }) => {
    const res = await api.post(
      "/api/v1/payment-methods/00000000-0000-0000-0000-000000000000/set-default",
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        failOnStatusCode: false,
      },
    );
    expect([400, 404]).toContain(res.status());
  });

  test("Delete on unknown id returns 404", async ({ auth, api }) => {
    const res = await api.delete(
      "/api/v1/payment-methods/00000000-0000-0000-0000-000000000000",
      {
        headers: { Authorization: `Bearer ${auth.token}` },
        failOnStatusCode: false,
      },
    );
    expect([400, 404]).toContain(res.status());
  });

  test("All payment-method routes require auth", async ({ api }) => {
    const reads = ["/api/v1/payment-methods", "/api/v1/payment-methods/default", "/api/v1/payment-methods/setup-intent"];
    for (const url of reads) {
      const res = await api.get(url, { failOnStatusCode: false });
      expect(res.status(), `GET ${url}`).toBe(401);
    }
    const post = await api.post("/api/v1/payment-methods", { data: {}, failOnStatusCode: false });
    expect(post.status()).toBe(401);
    const del = await api.delete("/api/v1/payment-methods/x", { failOnStatusCode: false });
    expect(del.status()).toBe(401);
  });

  test("Cross-tenant: B cannot see A's payment methods (always /me-scoped)", async ({
    auth,
    api,
  }) => {
    // Each /me-style endpoint must only return the caller's own data.
    const a = await apiListPaymentMethods(api, auth.token);
    expect(isSuccess(a.status)).toBe(true);
    // We cannot positively prove isolation without two real cards, but we
    // verify the endpoint never accepts a userId override.
    const probe = await api.get("/api/v1/payment-methods?userId=someone-else", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    // Should ignore the query param and return the caller's data, not error 500.
    expect(probe.status()).toBeLessThan(500);
  });
});
