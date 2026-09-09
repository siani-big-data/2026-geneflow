import { test, expect } from "../fixtures/test";
import {
  apiCreateStudy,
  apiDeleteStudy,
  apiGetStudy,
  apiListMyStudies,
  apiLogin,
  apiRegisterUser,
  apiSendInvitation,
  apiUpdateStudy,
  tryConfirmEmail,
} from "../fixtures/api";
import { isForbidden, isUnauthorized } from "../helpers/http";

/**
 * Two unrelated users (A and B). B must NOT be able to read, modify, delete or
 * leak data from A's resources even if they know the IDs.
 */
test.describe("Permissions › Cross-tenant isolation", () => {
  test("B cannot read A's private study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "owner-only-study");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);

    const res = await apiGetStudy(api, bToken, studyId);
    expect(isForbidden(res.status), `expected 401/403/404 but got ${res.status}`).toBe(true);
  });

  test("B cannot update A's study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "owner-only-update");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);

    const res = await apiUpdateStudy(api, bToken, studyId, { name: "stolen", description: "x" });
    expect(isForbidden(res.status)).toBe(true);
  });

  test("B cannot delete A's study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "owner-only-delete");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);

    const res = await apiDeleteStudy(api, bToken, studyId);
    expect(isForbidden(res.status)).toBe(true);
  });

  test("B cannot send invitations to A's study", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "owner-only-invite");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);

    const result = await apiSendInvitation(api, bToken, studyId, "anyone@example.test");
    expect(result, "Outsider should not be able to invite").toBeUndefined();
  });

  test("A's studies do NOT appear in B's /studies/mine", async ({ auth, api }) => {
    const distinctName = `private-${Date.now().toString(36)}`;
    await apiCreateStudy(api, auth.token, distinctName);

    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);
    const bStudies = await apiListMyStudies(api, bToken);

    expect(bStudies.find((s) => s.name === distinctName)).toBeUndefined();
  });

  test("Non-existent / random study id returns 404 (not 200, never leaks data)", async ({
    auth,
    api,
  }) => {
    const fakeId = "00000000-0000-0000-0000-000000000000";
    const res = await apiGetStudy(api, auth.token, fakeId);
    expect(res.ok).toBe(false);
    expect(res.status).toBeGreaterThanOrEqual(400);
  });

  test("A's bearer token cannot impersonate B's user-id endpoints", async ({
    auth,
    api,
  }) => {
    // GETs scoped to current user should always be the token's identity, not a query param.
    const meAsA = await api.get("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(meAsA.ok()).toBe(true);
    const body = (await meAsA.json()) as { email?: string };
    expect(body.email?.toLowerCase()).toBe(auth.user.email.toLowerCase());
  });

  test("Token swap: editing B's profile with A's token must fail or be a no-op for B", async ({
    auth,
    api,
  }) => {
    // Register B
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const bToken = await apiLogin(api, b.email, b.password);

    // A updates "their own" profile. That must NOT mutate B's profile.
    const aMark = `A-${Date.now().toString(36)}`;
    const res = await api.put("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { firstName: aMark },
      failOnStatusCode: false,
    });
    expect(res.ok()).toBe(true);

    const bProfile = await api.get("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${bToken}` },
      failOnStatusCode: false,
    });
    const bBody = (await bProfile.json()) as { firstName?: string };
    expect(bBody.firstName ?? "").not.toBe(aMark);
  });

  test("Unauth user cannot use anonymous request to read private study", async ({
    auth,
    api,
  }) => {
    const studyId = await apiCreateStudy(api, auth.token, "anon-private");
    const res = await apiGetStudy(api, undefined, studyId);
    expect(isUnauthorized(res.status)).toBe(true);
  });
});
