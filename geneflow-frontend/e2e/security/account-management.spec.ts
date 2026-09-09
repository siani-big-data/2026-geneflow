import { test, expect } from "../fixtures/test";
import {
  apiDeactivateAccount,
  apiDeleteAccount,
  apiLogin,
  apiRegisterUser,
  tryConfirmEmail,
} from "../fixtures/api";
import { isClientError, isSuccess } from "../helpers/http";

test.describe("Security › Account deactivation", () => {
  test("deactivated user cannot login again until reactivation", async ({ api }) => {
    const u = await apiRegisterUser(api);
    await tryConfirmEmail(api, u.email);
    const token = await apiLogin(api, u.email, u.password);
    const status = await apiDeactivateAccount(api, token);
    expect(isSuccess(status)).toBe(true);

    const res = await api.post("/api/v1/auth/login", {
      data: { identifier: u.email, password: u.password },
      failOnStatusCode: false,
    });
    expect(res.ok(), "Deactivated user should not be able to login").toBe(false);
    expect([400, 401, 403]).toContain(res.status());
  });

  test("anonymous cannot deactivate", async ({ api }) => {
    const res = await api.post("/api/v1/users/me/deactivate", { failOnStatusCode: false });
    expect(res.status()).toBe(401);
  });
});

test.describe("Security › Account deletion", () => {
  test("delete requires literal DELETE confirmation", async ({ api }) => {
    const u = await apiRegisterUser(api);
    await tryConfirmEmail(api, u.email);
    const token = await apiLogin(api, u.email, u.password);

    for (const bad of ["", "delete", "delete me", "yes", "confirm"]) {
      const status = await apiDeleteAccount(api, token, bad);
      expect(isClientError(status), `confirmation="${bad}"`).toBe(true);
    }
  });

  test("delete with confirmation removes the account", async ({ api }) => {
    const u = await apiRegisterUser(api);
    await tryConfirmEmail(api, u.email);
    const token = await apiLogin(api, u.email, u.password);

    const status = await apiDeleteAccount(api, token, "DELETE");
    expect(isSuccess(status)).toBe(true);

    // Old token should no longer work
    const me = await api.get("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${token}` },
      failOnStatusCode: false,
    });
    expect([401, 404]).toContain(me.status());

    // And login should fail
    const login = await api.post("/api/v1/auth/login", {
      data: { identifier: u.email, password: u.password },
      failOnStatusCode: false,
    });
    expect(login.ok()).toBe(false);
  });

  test("anonymous cannot delete account", async ({ api }) => {
    const res = await api.delete("/api/v1/users/me", {
      data: { confirmation: "DELETE" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });
});
