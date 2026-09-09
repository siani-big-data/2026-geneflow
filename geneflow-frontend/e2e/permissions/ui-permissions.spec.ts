import { test, expect } from "../fixtures/test";
import { apiCreateStudy, apiOnboardCollaborator } from "../fixtures/api";

/**
 * UI-level permission checks. As Viewer (lowest role) the page should NOT
 * surface destructive controls: delete, edit, invite, upload.
 */
test.describe("Permissions › UI as Viewer", () => {
  test("Viewer does not see Edit / Delete / Invite buttons", async ({ auth, api, page }) => {
    const studyId = await apiCreateStudy(api, auth.token, `viewer-ui-${Date.now()}`);
    const viewer = await apiOnboardCollaborator(api, auth.token, studyId, 4);

    await page.addInitScript((t: string) => {
      window.localStorage.setItem(
        "auth-storage",
        JSON.stringify({ state: { accessToken: t, isAuthenticated: true }, version: 0 }),
      );
      window.localStorage.setItem("accessToken", t);
    }, viewer.token);

    await page.goto(`/studies/${studyId}`);
    await page.waitForLoadState("networkidle");

    // None of these destructive actions should be visible to a Viewer
    const forbiddenButtons = [/delete study/i, /archive study/i, /edit study/i, /invite/i];
    for (const re of forbiddenButtons) {
      const btn = page.getByRole("button", { name: re }).first();
      const visible = await btn.isVisible().catch(() => false);
      expect(visible, `Viewer should NOT see button matching ${re}`).toBe(false);
    }
  });

  test("Viewer cannot reach /studies/:id/edit via direct URL", async ({ auth, api, page }) => {
    const studyId = await apiCreateStudy(api, auth.token, `viewer-edit-${Date.now()}`);
    const viewer = await apiOnboardCollaborator(api, auth.token, studyId, 4);

    await page.addInitScript((t: string) => {
      window.localStorage.setItem(
        "auth-storage",
        JSON.stringify({ state: { accessToken: t, isAuthenticated: true }, version: 0 }),
      );
      window.localStorage.setItem("accessToken", t);
    }, viewer.token);

    await page.goto(`/studies/${studyId}/edit`);
    await page.waitForLoadState("networkidle");
    // Either redirected, or a clear permission message - never an enabled save button.
    const save = page.getByRole("button", { name: /save|update/i }).first();
    const saveEnabled =
      (await save.isVisible().catch(() => false)) && (await save.isEnabled().catch(() => false));
    expect(saveEnabled, "Save button must not be enabled for Viewer").toBe(false);
  });

  test("Non-member visiting /studies/:id sees not-found or permission error", async ({
    auth,
    api,
    page,
  }) => {
    const studyId = await apiCreateStudy(api, auth.token, `secret-${Date.now()}`);

    // Anonymous: clear storage and go
    await page.context().clearCookies();
    await page.addInitScript(() => {
      try {
        window.localStorage.clear();
      } catch {
        /* noop */
      }
    });
    await page.goto(`/studies/${studyId}`);
    await page.waitForLoadState("networkidle");
    const url = page.url();
    const onLogin = /\/login/.test(url);
    const errorVisible = await page
      .getByText(/not found|forbidden|permission|access denied|404|sign ?in/i)
      .first()
      .isVisible()
      .catch(() => false);
    expect(onLogin || errorVisible, "Should redirect to login or show error").toBe(true);
  });
});
