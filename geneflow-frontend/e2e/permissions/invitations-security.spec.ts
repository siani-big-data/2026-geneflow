import { test, expect } from "../fixtures/test";
import {
  apiAcceptInvitation,
  apiCancelInvitation,
  apiCreateStudy,
  apiDeclineInvitation,
  apiGetStudy,
  apiLogin,
  apiRegisterUser,
  apiSendInvitation,
  tryConfirmEmail,
} from "../fixtures/api";
import { isForbidden, isSuccess } from "../helpers/http";

test.describe("Permissions › Invitation security", () => {
  test("Token guessing: random invitation token must not grant access", async ({
    auth,
    api,
  }) => {
    const studyId = await apiCreateStudy(api, auth.token, "tok-guess");
    const fakeToken = "fake-token-" + Math.random().toString(36).slice(2);

    const res = await api.post(`/api/v1/invitations/${fakeToken}/accept`, {
      headers: { Authorization: `Bearer ${auth.token}` },
      failOnStatusCode: false,
    });
    expect(res.ok()).toBe(false);
    expect([400, 404]).toContain(res.status());

    // And the study is still owner-only
    const owned = await apiGetStudy(api, auth.token, studyId);
    expect(isSuccess(owned.status)).toBe(true);
  });

  test("Wrong invitee: another user's invitation cannot be accepted by C", async ({
    auth,
    api,
  }) => {
    const studyId = await apiCreateStudy(api, auth.token, "wrong-invitee");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!invite, "Invitation send rejected");

    // Outsider C tries to accept B's token
    const c = await apiRegisterUser(api);
    await tryConfirmEmail(api, c.email);
    const cToken = await apiLogin(api, c.email, c.password);

    const accepted = await apiAcceptInvitation(api, cToken, invite!.token);
    expect(accepted, "C should not be able to accept B's invitation").toBe(false);

    // C still has no access
    const view = await apiGetStudy(api, cToken, studyId);
    expect(isForbidden(view.status)).toBe(true);
  });

  test("Double-accept: same invitation cannot be accepted twice", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "double-accept");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!invite);
    const bToken = await apiLogin(api, b.email, b.password);

    const first = await apiAcceptInvitation(api, bToken, invite!.token);
    expect(first).toBe(true);
    const second = await apiAcceptInvitation(api, bToken, invite!.token);
    expect(second, "Second accept should fail").toBe(false);
  });

  test("Cancelled invitation cannot be accepted", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "cancelled-inv");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!invite);

    const cancel = await apiCancelInvitation(api, auth.token, studyId, invite!.id);
    expect(isSuccess(cancel)).toBe(true);

    const bToken = await apiLogin(api, b.email, b.password);
    const accepted = await apiAcceptInvitation(api, bToken, invite!.token);
    expect(accepted, "Cancelled invitation should not be acceptable").toBe(false);
  });

  test("Decline then accept fails", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "decline-then-accept");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!invite);

    const bToken = await apiLogin(api, b.email, b.password);
    const declined = await apiDeclineInvitation(api, bToken, invite!.token);
    expect(declined).toBe(true);

    const accepted = await apiAcceptInvitation(api, bToken, invite!.token);
    expect(accepted, "Cannot accept after declining").toBe(false);
  });

  test("Invalid role ID is rejected", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "bad-role");
    // Roles 2,3,4 are valid. 1 (Owner) and 99 are not assignable via invite.
    const tries = [0, 1, 5, 99];
    for (const roleId of tries) {
      const res = await api.post(`/api/v1/studies/${studyId}/invitations`, {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { email: `r${roleId}@x.test`, roleId, message: null },
        failOnStatusCode: false,
      });
      expect(res.ok(), `roleId=${roleId} should be rejected`).toBe(false);
    }
  });

  test("Invitation email must be a real email format", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "bad-email");
    for (const email of ["notanemail", "@x.test", "x@", "x@x", ""]) {
      const res = await api.post(`/api/v1/studies/${studyId}/invitations`, {
        headers: { Authorization: `Bearer ${auth.token}` },
        data: { email, roleId: 3, message: null },
        failOnStatusCode: false,
      });
      expect(res.ok(), `email="${email}" should be rejected`).toBe(false);
    }
  });

  test("Invitee cannot cancel their own invitation (only owner/admin)", async ({
    auth,
    api,
  }) => {
    const studyId = await apiCreateStudy(api, auth.token, "invitee-cancel");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!invite);
    const bToken = await apiLogin(api, b.email, b.password);

    const status = await apiCancelInvitation(api, bToken, studyId, invite!.id);
    expect(isForbidden(status)).toBe(true);
  });

  test("Cannot invite the same email twice while pending", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "dup-invite");
    const email = `dup-${Date.now()}@x.test`;
    const first = await apiSendInvitation(api, auth.token, studyId, email);
    expect(first).toBeDefined();
    const second = await apiSendInvitation(api, auth.token, studyId, email);
    expect(second, "Second invite to same email should be rejected").toBeUndefined();
  });

  test("Cannot invite an existing member", async ({ auth, api }) => {
    const studyId = await apiCreateStudy(api, auth.token, "already-member");
    const b = await apiRegisterUser(api);
    await tryConfirmEmail(api, b.email);
    const first = await apiSendInvitation(api, auth.token, studyId, b.email);
    test.skip(!first);
    const bToken = await apiLogin(api, b.email, b.password);
    await apiAcceptInvitation(api, bToken, first!.token);

    const dup = await apiSendInvitation(api, auth.token, studyId, b.email);
    expect(dup, "Re-inviting an active member should be rejected").toBeUndefined();
  });
});
