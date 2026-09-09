import { test, expect } from "../fixtures/test";
import {
  apiGetCurrentSubscription,
  apiListPlans,
} from "../fixtures/api";
import { isClientError, isForbidden, isSuccess } from "../helpers/http";

test.describe("Billing › Plans (public catalog)", () => {
  test("GET /plans returns at least one plan", async ({ api }) => {
    const { status, items } = await apiListPlans(api);
    expect(isSuccess(status)).toBe(true);
    expect(items.length).toBeGreaterThan(0);
  });

  test("Each plan exposes name + price", async ({ api }) => {
    const { items } = await apiListPlans(api);
    for (const plan of items) {
      expect(plan.id).toBeTruthy();
      expect(plan.name, `Plan ${plan.id} missing name`).toBeTruthy();
      expect(typeof plan.priceCents, `Plan ${plan.name} priceCents`).toBe("number");
      expect(plan.priceCents).toBeGreaterThanOrEqual(0);
    }
  });

  test("Unknown planId returns 404", async ({ api }) => {
    const res = await api.get("/api/v1/plans/does-not-exist", { failOnStatusCode: false });
    expect(res.status()).toBe(404);
  });

  test("Plan list is anonymous-accessible", async ({ api }) => {
    const res = await api.get("/api/v1/plans", { failOnStatusCode: false });
    expect(res.status()).not.toBe(401);
  });
});

test.describe("Billing › Subscription state", () => {
  test("Fresh user has a current subscription (free tier or none)", async ({ auth, api }) => {
    const { status, body } = await apiGetCurrentSubscription(api, auth.token);
    // Either 200 with a free-tier subscription object, or 404 if free is implicit.
    expect([200, 204, 404]).toContain(status);
    if (status === 200) {
      expect(body, "Subscription response should not be null").toBeTruthy();
    }
  });

  test("Anonymous user cannot read subscription", async ({ api }) => {
    const res = await api.get("/api/v1/subscriptions/current", { failOnStatusCode: false });
    expect(res.status()).toBe(401);
  });

  test("Subscription history requires auth", async ({ api }) => {
    const res = await api.get("/api/v1/subscriptions/history", { failOnStatusCode: false });
    expect(res.status()).toBe(401);
  });

  test("Cannot subscribe to non-existent plan", async ({ auth, api }) => {
    const res = await api.post("/api/v1/subscriptions", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { planId: "ghost-plan", paymentMethodId: "pm_test" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Cannot cancel when there is no active paid subscription", async ({ auth, api }) => {
    const res = await api.post("/api/v1/subscriptions/cancel", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    // Either 400 (nothing to cancel) or 200 (idempotent no-op). Should never be 5xx.
    expect(res.status()).toBeLessThan(500);
  });

  test("Change-plan requires a target planId", async ({ auth, api }) => {
    const res = await api.post("/api/v1/subscriptions/change-plan", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: {},
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("Change-plan rejects unknown planId", async ({ auth, api }) => {
    const res = await api.post("/api/v1/subscriptions/change-plan", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { planId: "nope" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("All subscription routes require auth", async ({ api }) => {
    const urls: [string, "get" | "post"][] = [
      ["/api/v1/subscriptions/current", "get"],
      ["/api/v1/subscriptions/history", "get"],
      ["/api/v1/subscriptions", "post"],
      ["/api/v1/subscriptions/cancel", "post"],
      ["/api/v1/subscriptions/change-plan", "post"],
    ];
    for (const [url, method] of urls) {
      const res = await api[method](url, { failOnStatusCode: false, data: {} });
      expect(res.status(), `${method.toUpperCase()} ${url}`).toBe(401);
    }
  });
});

test.describe("Billing › UI surfaces", () => {
  test("Settings page exposes a billing / subscription section", async ({ auth }) => {
    await auth.page.goto("/settings");
    await auth.page.waitForLoadState("networkidle");
    const visible = await auth.page
      .getByText(/billing|subscription|plan|payment/i)
      .first()
      .isVisible()
      .catch(() => false);
    expect(visible).toBe(true);
  });

  test("Plans page lists at least one plan card", async ({ auth, api }) => {
    const { items } = await apiListPlans(api);
    test.skip(items.length === 0);
    await auth.page.goto("/pricing");
    await auth.page.waitForLoadState("networkidle");
    const planName = items[0].name!;
    await expect(auth.page.getByText(planName).first()).toBeVisible({ timeout: 10_000 });
  });
});

test.describe("Billing › Free tier limits (smoke)", () => {
  test("Free user can create at least one study", async ({ auth, api }) => {
    const res = await api.post("/api/v1/studies", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { name: "free-tier-test" },
      failOnStatusCode: false,
    });
    expect(isSuccess(res.status())).toBe(true);
  });

  test("Anonymous user is forbidden from creating studies (auth check, not billing)", async ({
    api,
  }) => {
    const res = await api.post("/api/v1/studies", {
      data: { name: "x" },
      failOnStatusCode: false,
    });
    expect(isForbidden(res.status())).toBe(true);
  });
});
