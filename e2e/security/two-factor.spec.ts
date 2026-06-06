import { test, expect } from "../fixtures/test";
import {
  api2faDisable,
  api2faEnableEmail,
  api2faSetupTotp,
} from "../fixtures/api";
import { isClientError, isSuccess } from "../helpers/http";

test.describe("Security › 2FA email channel", () => {
  test("enable returns 2xx", async ({ auth, api }) => {
    const status = await api2faEnableEmail(api, auth.token);
    expect(isSuccess(status)).toBe(true);
  });

  test("enable is idempotent: enabling twice is not 5xx", async ({ auth, api }) => {
    await api2faEnableEmail(api, auth.token);
    const second = await api2faEnableEmail(api, auth.token);
    expect(second).toBeLessThan(500);
  });

  test("disable works after enable", async ({ auth, api }) => {
    await api2faEnableEmail(api, auth.token);
    const status = await api2faDisable(api, auth.token);
    expect(isSuccess(status)).toBe(true);
  });

  test("disable when not enabled is not a 5xx", async ({ auth, api }) => {
    const status = await api2faDisable(api, auth.token);
    expect(status).toBeLessThan(500);
  });

  test("anonymous cannot toggle 2FA", async ({ api }) => {
    for (const url of ["/api/v1/users/2fa/enable", "/api/v1/users/2fa/disable"]) {
      const res = await api.post(url, { failOnStatusCode: false });
      expect(res.status(), url).toBe(401);
    }
  });
});

test.describe("Security › 2FA TOTP (authenticator app)", () => {
  test("setup returns a base32 secret and a provisioning URI", async ({ auth, api }) => {
    const { status, secret, qrCodeUri } = await api2faSetupTotp(api, auth.token);
    expect(isSuccess(status)).toBe(true);
    expect(secret, "TOTP secret required").toBeTruthy();
    expect(secret!).toMatch(/^[A-Z2-7]{16,}$/); // base32
    expect(qrCodeUri ?? "").toMatch(/^otpauth:\/\/totp\//);
  });

  test("confirm rejects an obviously wrong code", async ({ auth, api }) => {
    await api2faSetupTotp(api, auth.token);
    const res = await api.post("/api/v1/users/2fa/confirm", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { code: "000000" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("confirm without setup is rejected", async ({ auth, api }) => {
    // Fresh test user, never called /setup
    const res = await api.post("/api/v1/users/2fa/confirm", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { code: "123456" },
      failOnStatusCode: false,
    });
    expect(isClientError(res.status())).toBe(true);
  });

  test("confirm requires a 6-digit code", async ({ auth, api }) => {
    await api2faSetupTotp(api, auth.token);
    for (const bad of ["", "abc", "12345", "1234567"]) {
      const res = await api.post("/api/v1/users/2fa/confirm", {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { code: bad },
        failOnStatusCode: false,
      });
      expect(isClientError(res.status()), `code="${bad}"`).toBe(true);
    }
  });

  test("anonymous cannot setup or confirm TOTP", async ({ api }) => {
    expect((await api.get("/api/v1/users/2fa/setup", { failOnStatusCode: false })).status()).toBe(401);
    expect(
      (await api.post("/api/v1/users/2fa/confirm", { data: { code: "000000" }, failOnStatusCode: false }))
        .status(),
    ).toBe(401);
  });
});

test.describe("Security › 2FA UI", () => {
  test("Settings exposes a 2FA panel", async ({ auth }) => {
    await auth.page.goto("/settings/security");
    await auth.page.waitForLoadState("networkidle");
    const visible = await auth.page
      .getByText(/two[- ]?factor|2fa|authenticator|otp/i)
      .first()
      .isVisible()
      .catch(() => false);
    expect(visible).toBe(true);
  });
});
