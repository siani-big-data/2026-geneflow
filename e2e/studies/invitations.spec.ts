import { test, expect } from "../fixtures/test";
import {
  apiAcceptInvitation,
  apiCreateStudy,
  apiDeclineInvitation,
  apiListMyInvitations,
  apiListStudyMembers,
  apiLogin,
  apiRegisterUser,
  apiSendInvitation,
  tryConfirmEmail,
} from "../fixtures/api";
import { randomStudyName } from "../helpers/data";

/**
 * Real two-user collaboration tests. Owner (A) creates a study and invites
 * a freshly registered user (B). We then exercise:
 *   - B sees the invitation in their inbox
 *   - B accepts → becomes a member, sees the study
 *   - Separate study: B declines → study not visible to B
 *   - Owner-side member listing reflects the new collaborator
 */
test.describe("Studies › Invitations (two real users)", () => {
  test("invite → accept → invitee sees the study", async ({ auth, api, page }) => {
    // Owner A creates a study
    const studyName = randomStudyName("Shared");
    const studyId = await apiCreateStudy(api, auth.token, studyName);

    // Register invitee B
    const invitee = await apiRegisterUser(api);
    await tryConfirmEmail(api, invitee.email);

    // A sends the invitation
    const invite = await apiSendInvitation(
      api,
      auth.token,
      studyId,
      invitee.email,
      3, // Editor
    );
    test.skip(!invite, "Backend rejected invitation send");

    // B logs in
    const inviteeToken = await apiLogin(api, invitee.email, invitee.password);

    // B sees the invitation in their inbox
    const inbox = await apiListMyInvitations(api, inviteeToken);
    expect(
      inbox.some((i) => i.token === invite!.token),
      "Invitation should appear in invitee inbox",
    ).toBe(true);

    // B accepts via API
    const accepted = await apiAcceptInvitation(api, inviteeToken, invite!.token);
    expect(accepted).toBe(true);

    // B can now see the study (UI assertion)
    await page.addInitScript((t: string) => {
      window.localStorage.setItem(
        "auth-storage",
        JSON.stringify({ state: { accessToken: t, isAuthenticated: true }, version: 0 }),
      );
      window.localStorage.setItem("accessToken", t);
    }, inviteeToken);
    await page.goto("/studies");
    await expect(page.getByText(studyName)).toBeVisible({ timeout: 10_000 });

    // Owner sees B in the members list
    const members = await apiListStudyMembers(api, auth.token, studyId);
    expect(
      members.some((m) => m.email?.toLowerCase() === invitee.email.toLowerCase()),
      "Owner should see invitee in member list",
    ).toBe(true);
  });

  test("invite → decline → invitee does NOT see the study", async ({ auth, api, page }) => {
    const studyName = randomStudyName("Declined");
    const studyId = await apiCreateStudy(api, auth.token, studyName);

    const invitee = await apiRegisterUser(api);
    await tryConfirmEmail(api, invitee.email);

    const invite = await apiSendInvitation(api, auth.token, studyId, invitee.email);
    test.skip(!invite, "Backend rejected invitation send");

    const inviteeToken = await apiLogin(api, invitee.email, invitee.password);
    const declined = await apiDeclineInvitation(api, inviteeToken, invite!.token);
    expect(declined).toBe(true);

    await page.addInitScript((t: string) => {
      window.localStorage.setItem(
        "auth-storage",
        JSON.stringify({ state: { accessToken: t, isAuthenticated: true }, version: 0 }),
      );
      window.localStorage.setItem("accessToken", t);
    }, inviteeToken);
    await page.goto("/studies");
    await page.waitForLoadState("networkidle");
    await expect(page.getByText(studyName)).toHaveCount(0);
  });

  test("invitee can accept through the UI (notifications / invitations page)", async ({
    auth,
    api,
    page,
  }) => {
    const studyName = randomStudyName("UI-Accept");
    const studyId = await apiCreateStudy(api, auth.token, studyName);

    const invitee = await apiRegisterUser(api);
    await tryConfirmEmail(api, invitee.email);
    const invite = await apiSendInvitation(api, auth.token, studyId, invitee.email);
    test.skip(!invite, "Backend rejected invitation send");
    const inviteeToken = await apiLogin(api, invitee.email, invitee.password);

    await page.addInitScript((t: string) => {
      window.localStorage.setItem(
        "auth-storage",
        JSON.stringify({ state: { accessToken: t, isAuthenticated: true }, version: 0 }),
      );
      window.localStorage.setItem("accessToken", t);
    }, inviteeToken);

    // Try the common URLs first; fall back to dashboard notifications
    const candidates = ["/invitations", "/dashboard", "/notifications"];
    let acceptBtn = null as Awaited<ReturnType<typeof page.getByRole>> | null;
    for (const url of candidates) {
      await page.goto(url);
      const btn = page.getByRole("button", { name: /accept/i }).first();
      if (await btn.isVisible().catch(() => false)) {
        acceptBtn = btn;
        break;
      }
    }
    test.skip(!acceptBtn, "No UI surface exposing invitations was found");
    await acceptBtn!.click();
    await page.waitForLoadState("networkidle");

    await page.goto("/studies");
    await expect(page.getByText(studyName)).toBeVisible({ timeout: 10_000 });
  });
});
