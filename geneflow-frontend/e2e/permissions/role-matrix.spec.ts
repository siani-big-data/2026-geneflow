import { test, expect } from "../fixtures/test";
import {
  apiAddStudyPaper,
  apiCancelInvitation,
  apiCreateStudy,
  apiDeleteStudy,
  apiGetStudy,
  apiLeaveStudy,
  apiOnboardCollaborator,
  apiRemoveMember,
  apiSendInvitation,
  apiUpdateStudy,
  apiUploadTrace,
} from "../fixtures/api";
import { isForbidden, isSuccess } from "../helpers/http";
import { AB1_FIXTURES, readTraceBuffer } from "../helpers/traces";

/**
 * Role-by-role authorization matrix. Owner (A) creates a study and onboards
 * a Viewer, Editor and Admin. We then probe what each one can do.
 *
 *  RoleId 2 = Admin, 3 = Editor, 4 = Viewer (per SendInvitationRequest contract)
 */

const TRACE = AB1_FIXTURES[0];

async function seedStudyWithRoles(api: Parameters<typeof apiCreateStudy>[0], ownerToken: string) {
  const studyId = await apiCreateStudy(api, ownerToken, `role-matrix-${Date.now().toString(36)}`);
  const viewer = await apiOnboardCollaborator(api, ownerToken, studyId, 4);
  const editor = await apiOnboardCollaborator(api, ownerToken, studyId, 3);
  const admin = await apiOnboardCollaborator(api, ownerToken, studyId, 2);
  return { studyId, viewer, editor, admin };
}

test.describe("Permissions › Role matrix", () => {
  test("ALL roles can GET the study", async ({ auth, api }) => {
    const { studyId, viewer, editor, admin } = await seedStudyWithRoles(api, auth.token);
    for (const t of [viewer.token, editor.token, admin.token, auth.token]) {
      const res = await apiGetStudy(api, t, studyId);
      expect(isSuccess(res.status), `Got ${res.status} for role token`).toBe(true);
    }
  });

  test("Viewer CANNOT update the study", async ({ auth, api }) => {
    const { studyId, viewer } = await seedStudyWithRoles(api, auth.token);
    const res = await apiUpdateStudy(api, viewer.token, studyId, {
      name: "viewer-tried",
      description: "x",
    });
    expect(isForbidden(res.status)).toBe(true);
  });

  test("Editor CAN update the study", async ({ auth, api }) => {
    const { studyId, editor } = await seedStudyWithRoles(api, auth.token);
    const res = await apiUpdateStudy(api, editor.token, studyId, {
      name: "editor-renamed",
      description: "x",
    });
    expect(isSuccess(res.status), `Editor should be allowed, got ${res.status}`).toBe(true);
  });

  test("Viewer CANNOT add a paper", async ({ auth, api }) => {
    const { studyId, viewer } = await seedStudyWithRoles(api, auth.token);
    const status = await apiAddStudyPaper(api, viewer.token, studyId, {
      title: "blocked",
      doi: "10.1/x",
    });
    expect(isForbidden(status)).toBe(true);
  });

  test("Editor CAN add a paper", async ({ auth, api }) => {
    const { studyId, editor } = await seedStudyWithRoles(api, auth.token);
    const status = await apiAddStudyPaper(api, editor.token, studyId, {
      title: "editor paper",
      doi: "10.1/y",
    });
    expect(isSuccess(status)).toBe(true);
  });

  test("Viewer CANNOT upload a trace", async ({ auth, api }) => {
    const { studyId, viewer } = await seedStudyWithRoles(api, auth.token);
    const traceId = await apiUploadTrace(api, viewer.token, {
      studyId,
      filename: TRACE.filename,
      mimeType: TRACE.mimeType,
      buffer: readTraceBuffer(TRACE),
    });
    expect(traceId, "Viewer should not be able to upload").toBeUndefined();
  });

  test("Editor CAN upload a trace", async ({ auth, api }) => {
    const { studyId, editor } = await seedStudyWithRoles(api, auth.token);
    const traceId = await apiUploadTrace(api, editor.token, {
      studyId,
      filename: TRACE.filename,
      mimeType: TRACE.mimeType,
      buffer: readTraceBuffer(TRACE),
    });
    expect(traceId, "Editor should be allowed to upload").toBeTruthy();
  });

  test("Viewer & Editor CANNOT invite new members", async ({ auth, api }) => {
    const { studyId, viewer, editor } = await seedStudyWithRoles(api, auth.token);
    const v = await apiSendInvitation(api, viewer.token, studyId, `v${Date.now()}@x.test`);
    const e = await apiSendInvitation(api, editor.token, studyId, `e${Date.now()}@x.test`);
    expect(v, "Viewer must not invite").toBeUndefined();
    expect(e, "Editor must not invite").toBeUndefined();
  });

  test("Admin CAN invite new members", async ({ auth, api }) => {
    const { studyId, admin } = await seedStudyWithRoles(api, auth.token);
    const r = await apiSendInvitation(api, admin.token, studyId, `a${Date.now()}@x.test`);
    expect(r, "Admin should be allowed to invite").toBeDefined();
  });

  test("Only Owner can DELETE the study", async ({ auth, api }) => {
    const { studyId, viewer, editor, admin } = await seedStudyWithRoles(api, auth.token);
    for (const [role, token] of [
      ["viewer", viewer.token],
      ["editor", editor.token],
      ["admin", admin.token],
    ] as const) {
      const r = await apiDeleteStudy(api, token, studyId);
      expect(isForbidden(r.status), `${role} should be forbidden, got ${r.status}`).toBe(true);
    }
    const owner = await apiDeleteStudy(api, auth.token, studyId);
    expect(isSuccess(owner.status), `Owner should be allowed, got ${owner.status}`).toBe(true);
  });

  test("Admin CAN remove a Viewer", async ({ auth, api }) => {
    const { studyId, viewer, admin } = await seedStudyWithRoles(api, auth.token);
    const userId = viewer.user.userId;
    test.skip(!userId, "registration did not return userId");
    const status = await apiRemoveMember(api, admin.token, studyId, userId!);
    expect(isSuccess(status)).toBe(true);
    // Viewer can no longer read the study
    const after = await apiGetStudy(api, viewer.token, studyId);
    expect(isForbidden(after.status)).toBe(true);
  });

  test("Viewer CANNOT remove other members", async ({ auth, api }) => {
    const { studyId, viewer, editor } = await seedStudyWithRoles(api, auth.token);
    test.skip(!editor.user.userId);
    const status = await apiRemoveMember(api, viewer.token, studyId, editor.user.userId!);
    expect(isForbidden(status)).toBe(true);
  });

  test("Members can LEAVE on their own", async ({ auth, api }) => {
    const { studyId, editor } = await seedStudyWithRoles(api, auth.token);
    const status = await apiLeaveStudy(api, editor.token, studyId);
    expect(isSuccess(status)).toBe(true);
    const after = await apiGetStudy(api, editor.token, studyId);
    expect(isForbidden(after.status)).toBe(true);
  });

  test("Owner CANNOT remove themselves without transferring ownership", async ({
    auth,
    api,
  }) => {
    const { studyId } = await seedStudyWithRoles(api, auth.token);
    const status = await apiLeaveStudy(api, auth.token, studyId);
    expect(isForbidden(status) || status === 400, `got ${status}`).toBe(true);
  });

  test("Cancelling an invitation: Admin can, Viewer cannot", async ({ auth, api }) => {
    const { studyId, viewer, admin } = await seedStudyWithRoles(api, auth.token);
    const inv = await apiSendInvitation(api, admin.token, studyId, `t${Date.now()}@x.test`);
    test.skip(!inv);

    const denied = await apiCancelInvitation(api, viewer.token, studyId, inv!.id);
    expect(isForbidden(denied)).toBe(true);

    const ok = await apiCancelInvitation(api, admin.token, studyId, inv!.id);
    expect(isSuccess(ok)).toBe(true);
  });
});
