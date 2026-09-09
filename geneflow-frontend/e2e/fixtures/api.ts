import { request, type APIRequestContext } from "@playwright/test";

/**
 * Direct backend API client for E2E test setup / teardown.
 * Bypasses the UI to create test data, seed users, etc.
 */
export const API_BASE = process.env.E2E_API_URL ?? "http://localhost:5145";

export async function createApiContext(): Promise<APIRequestContext> {
  return await request.newContext({ baseURL: API_BASE });
}

export interface RegisteredUser {
  email: string;
  username: string;
  password: string;
  userId?: string;
  accessToken?: string;
}

/**
 * Register a user directly through the API.
 * Returns the credentials so the UI test can log in with them.
 */
export async function apiRegisterUser(
  api: APIRequestContext,
  overrides: Partial<RegisteredUser> = {},
): Promise<RegisteredUser> {
  const suffix = Math.random().toString(36).slice(2, 10);
  const user: RegisteredUser = {
    email: overrides.email ?? `e2e_${suffix}@geneflow.test`,
    username: overrides.username ?? `e2euser${suffix}`,
    password: overrides.password ?? "Passw0rd!Strong",
  };

  const res = await api.post("/api/v1/auth/register", {
    data: {
      email: user.email,
      username: user.username,
      password: user.password,
    },
  });

  if (!res.ok()) {
    throw new Error(
      `Failed to register user via API (${res.status()}): ${await res.text()}`,
    );
  }

  const body = (await res.json().catch(() => ({}))) as {
    userId?: string;
    id?: string;
  };
  user.userId = body.userId ?? body.id;
  return user;
}

/**
 * Login through the API and return the access token.
 * Useful for setting cookies / localStorage so tests can skip the login UI.
 */
export async function apiLogin(
  api: APIRequestContext,
  identifier: string,
  password: string,
): Promise<string> {
  const res = await api.post("/api/v1/auth/login", {
    data: { identifier, password },
  });
  if (!res.ok()) {
    throw new Error(
      `Failed to login via API (${res.status()}): ${await res.text()}`,
    );
  }
  const body = (await res.json()) as {
    accessToken?: string;
    token?: string;
    tokens?: { accessToken?: string };
  };
  const token = body.tokens?.accessToken ?? body.accessToken ?? body.token;
  if (!token) throw new Error("Login response missing access token");
  return token;
}

/**
 * Confirm email of a freshly registered user via the dev-only backend endpoint
 * `POST /api/v1/auth/dev/confirm-email` (registered only when the backend runs
 * with ASPNETCORE_ENVIRONMENT=Development). The endpoint runs the real
 * `User.VerifyEmail()` aggregate path, so the user becomes loginable.
 */
export async function tryConfirmEmail(
  api: APIRequestContext,
  email: string,
): Promise<void> {
  await api.post("/api/v1/auth/dev/confirm-email", {
    data: { email },
    failOnStatusCode: false,
  });
}

/**
 * Create a study via the API and return its id. Used to seed state for trace
 * upload tests so they don't depend on UI being correct.
 */
export async function apiCreateStudy(
  api: APIRequestContext,
  token: string,
  name: string,
): Promise<string> {
  // Backend's CreateStudyRequest requires `title` (5-200 chars) + `researchFieldId` (int>=1).
  // The legacy `name` param is mapped to `title` and padded if too short.
  const title = name.length >= 5 ? name : `study-${name}`;
  const res = await api.post("/api/v1/studies", {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      title,
      description: "E2E trace seed",
      researchFieldId: 1,
    },
  });
  if (!res.ok()) {
    throw new Error(
      `Failed to create study (${res.status()}): ${await res.text()}`,
    );
  }
  const body = (await res.json()) as { id?: string; studyId?: string };
  const id = body.id ?? body.studyId;
  if (!id) throw new Error("Create study response missing id");
  return id;
}

/* -------------------------- Profile helpers -------------------------- */

export interface ProfileSnapshot {
  firstName?: string;
  lastName?: string;
  bio?: string;
  location?: string;
  photoUrl?: string;
  orcidId?: string;
  website?: string;
}

export async function apiGetProfile(
  api: APIRequestContext,
  token: string,
): Promise<ProfileSnapshot | undefined> {
  const res = await api.get("/api/v1/profiles/me", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return undefined;
  return (await res.json().catch(() => undefined)) as ProfileSnapshot | undefined;
}

export async function apiUpdateProfile(
  api: APIRequestContext,
  token: string,
  data: { firstName: string; lastName?: string; bio?: string; location?: string },
): Promise<boolean> {
  const res = await api.put("/api/v1/profiles/me", {
    headers: { Authorization: `Bearer ${token}` },
    data,
    failOnStatusCode: false,
  });
  return res.ok();
}

export async function apiUpdateResearchIdentifiers(
  api: APIRequestContext,
  token: string,
  data: { orcidId?: string; website?: string },
): Promise<{ ok: boolean; status: number; body: string }> {
  const res = await api.put("/api/v1/profiles/me/research-identifiers", {
    headers: { Authorization: `Bearer ${token}` },
    data,
    failOnStatusCode: false,
  });
  return { ok: res.ok(), status: res.status(), body: await res.text() };
}

export async function apiUploadProfilePhoto(
  api: APIRequestContext,
  token: string,
  filename: string,
  mimeType: string,
  buffer: Buffer,
): Promise<{ ok: boolean; status: number; photoUrl?: string }> {
  const res = await api.post("/api/v1/profiles/me/photo/upload", {
    headers: { Authorization: `Bearer ${token}` },
    multipart: { file: { name: filename, mimeType, buffer } },
    failOnStatusCode: false,
  });
  if (!res.ok()) return { ok: false, status: res.status() };
  const body = (await res.json().catch(() => ({}))) as { photoUrl?: string };
  return { ok: true, status: res.status(), photoUrl: body.photoUrl };
}

export async function apiDeleteProfilePhoto(
  api: APIRequestContext,
  token: string,
): Promise<boolean> {
  const res = await api.delete("/api/v1/profiles/me/photo", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.ok();
}

/* ----------------------- Study extra helpers ------------------------- */

export async function apiGetStudy(
  api: APIRequestContext,
  token: string | undefined,
  studyId: string,
): Promise<{ ok: boolean; status: number; body: unknown }> {
  const res = await api.get(`/api/v1/studies/${studyId}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    failOnStatusCode: false,
  });
  return { ok: res.ok(), status: res.status(), body: await res.json().catch(() => null) };
}

export async function apiUpdateStudy(
  api: APIRequestContext,
  token: string | undefined,
  studyId: string,
  data: Record<string, unknown>,
): Promise<{ ok: boolean; status: number }> {
  const res = await api.put(`/api/v1/studies/${studyId}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    data,
    failOnStatusCode: false,
  });
  return { ok: res.ok(), status: res.status() };
}

export async function apiDeleteStudy(
  api: APIRequestContext,
  token: string | undefined,
  studyId: string,
): Promise<{ ok: boolean; status: number }> {
  const res = await api.delete(`/api/v1/studies/${studyId}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    failOnStatusCode: false,
  });
  return { ok: res.ok(), status: res.status() };
}

export async function apiListMyStudies(
  api: APIRequestContext,
  token: string,
): Promise<Array<{ id: string; name?: string }>> {
  const res = await api.get("/api/v1/studies/mine", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return [];
  const body = (await res.json().catch(() => ({}))) as {
    items?: Array<{ id: string; name?: string }>;
  };
  return body.items ?? [];
}

export async function apiAddStudyPaper(
  api: APIRequestContext,
  token: string,
  studyId: string,
  data: { title: string; doi?: string },
): Promise<number> {
  const res = await api.post(`/api/v1/studies/${studyId}/papers`, {
    headers: { Authorization: `Bearer ${token}` },
    data,
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiRemoveMember(
  api: APIRequestContext,
  token: string,
  studyId: string,
  userId: string,
): Promise<number> {
  const res = await api.delete(`/api/v1/studies/${studyId}/members/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiCancelInvitation(
  api: APIRequestContext,
  token: string,
  studyId: string,
  invitationId: string,
): Promise<number> {
  const res = await api.delete(`/api/v1/studies/${studyId}/invitations/${invitationId}`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiStarStudy(
  api: APIRequestContext,
  token: string,
  studyId: string,
): Promise<number> {
  const res = await api.post(`/api/v1/studies/${studyId}/stars`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiLeaveStudy(
  api: APIRequestContext,
  token: string,
  studyId: string,
): Promise<number> {
  const res = await api.post(`/api/v1/studies/${studyId}/members/leave`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiTransferOwnership(
  api: APIRequestContext,
  token: string,
  studyId: string,
  newOwnerUserId: string,
): Promise<number> {
  const res = await api.post(`/api/v1/studies/${studyId}/members/transfer-ownership`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { newOwnerUserId },
    failOnStatusCode: false,
  });
  return res.status();
}

/** Register + login + accept-as-collaborator helper. Returns the collaborator's token. */
export async function apiOnboardCollaborator(
  api: APIRequestContext,
  ownerToken: string,
  studyId: string,
  roleId: 2 | 3 | 4,
): Promise<{ token: string; user: RegisteredUser }> {
  const user = await apiRegisterUser(api);
  await tryConfirmEmail(api, user.email);
  const invite = await apiSendInvitation(api, ownerToken, studyId, user.email, roleId);
  if (!invite) throw new Error(`Owner could not invite role=${roleId}`);
  const token = await apiLogin(api, user.email, user.password);
  const accepted = await apiAcceptInvitation(api, token, invite.token);
  if (!accepted) throw new Error("Collaborator could not accept invitation");
  return { token, user };
}

/* --------------------------- Billing -------------------------------- */

export async function apiListPlans(api: APIRequestContext): Promise<{
  status: number;
  items: Array<{ id: string; name?: string; priceCents?: number }>;
}> {
  const res = await api.get("/api/v1/plans", { failOnStatusCode: false });
  if (!res.ok()) return { status: res.status(), items: [] };
  const body = (await res.json().catch(() => ({}))) as {
    items?: Array<{ id: string; name?: string; priceCents?: number }>;
  };
  return { status: res.status(), items: body.items ?? [] };
}

export async function apiGetCurrentSubscription(
  api: APIRequestContext,
  token: string,
): Promise<{ status: number; body: unknown }> {
  const res = await api.get("/api/v1/subscriptions/current", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

export async function apiCreateSetupIntent(
  api: APIRequestContext,
  token: string,
): Promise<{ status: number; clientSecret?: string }> {
  const res = await api.get("/api/v1/payment-methods/setup-intent", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return { status: res.status() };
  const body = (await res.json().catch(() => ({}))) as { clientSecret?: string };
  return { status: res.status(), clientSecret: body.clientSecret };
}

export async function apiListPaymentMethods(
  api: APIRequestContext,
  token: string,
): Promise<{ status: number; items: unknown[] }> {
  const res = await api.get("/api/v1/payment-methods", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return { status: res.status(), items: [] };
  const body = (await res.json().catch(() => ({}))) as { items?: unknown[] };
  return { status: res.status(), items: body.items ?? [] };
}

/* -------------------------- Usage / quotas -------------------------- */

export async function apiGetBillingUsage(api: APIRequestContext, token: string) {
  const res = await api.get("/api/v1/usage/billing", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

export async function apiGetDashboardUsage(api: APIRequestContext, token: string) {
  const res = await api.get("/api/v1/usage/dashboard", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

/* ------------------------------ 2FA --------------------------------- */

export async function api2faEnableEmail(api: APIRequestContext, token: string) {
  const res = await api.post("/api/v1/users/2fa/enable", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function api2faDisable(api: APIRequestContext, token: string) {
  const res = await api.post("/api/v1/users/2fa/disable", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function api2faSetupTotp(api: APIRequestContext, token: string) {
  const res = await api.get("/api/v1/users/2fa/setup", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return { status: res.status() };
  const body = (await res.json().catch(() => ({}))) as {
    secret?: string;
    qrCodeUri?: string;
  };
  return { status: res.status(), ...body };
}

/* -------------------------- External logins ------------------------- */

export async function apiListExternalLogins(api: APIRequestContext, token: string) {
  const res = await api.get("/api/v1/users/external-logins", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return { status: res.status(), items: [] as string[] };
  const body = (await res.json().catch(() => ({}))) as {
    items?: Array<{ provider: string }>;
  };
  return { status: res.status(), items: (body.items ?? []).map((i) => i.provider) };
}

/* ------------------------- Account lifecycle ------------------------ */

export async function apiDeactivateAccount(api: APIRequestContext, token: string) {
  const res = await api.post("/api/v1/users/me/deactivate", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.status();
}

export async function apiDeleteAccount(
  api: APIRequestContext,
  token: string,
  confirmation: string,
) {
  const res = await api.delete("/api/v1/users/me", {
    headers: { Authorization: `Bearer ${token}` },
    data: { confirmation },
    failOnStatusCode: false,
  });
  return res.status();
}

/* --------------------------- Discovery ------------------------------ */

export async function apiGetPublicStudies(api: APIRequestContext) {
  const res = await api.get("/api/v1/studies/public", { failOnStatusCode: false });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

export async function apiGetFeaturedStudies(api: APIRequestContext) {
  const res = await api.get("/api/v1/studies/featured", { failOnStatusCode: false });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

export async function apiGetResearchFields(api: APIRequestContext) {
  const res = await api.get("/api/v1/studies/research-fields", { failOnStatusCode: false });
  return { status: res.status(), body: await res.json().catch(() => null) };
}

/** Tiny valid 1x1 PNG (87 bytes). Useful for upload tests. */
export const TINY_PNG = Buffer.from(
  "89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000d49444154789c6300010000000500010d0a2db40000000049454e44ae426082",
  "hex",
);

/** Send a study invitation. Returns the invitation id + token (token is needed to accept). */
export async function apiSendInvitation(
  api: APIRequestContext,
  token: string,
  studyId: string,
  email: string,
  roleId: 2 | 3 | 4 = 3,
): Promise<{ id: string; token: string } | undefined> {
  const res = await api.post(`/api/v1/studies/${studyId}/invitations`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { email, roleId, message: "E2E invite" },
    failOnStatusCode: false,
  });
  if (!res.ok()) return undefined;
  const body = (await res.json().catch(() => ({}))) as {
    id?: string;
    token?: string;
  };
  if (!body.id || !body.token) return undefined;
  return { id: body.id, token: body.token };
}

export async function apiListMyInvitations(
  api: APIRequestContext,
  token: string,
): Promise<Array<{ id: string; token: string; studyId: string; status: string }>> {
  const res = await api.get("/api/v1/invitations", {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return [];
  const body = (await res.json().catch(() => ({}))) as {
    items?: Array<{ id: string; token: string; studyId: string; status: string }>;
  };
  return body.items ?? [];
}

export async function apiAcceptInvitation(
  api: APIRequestContext,
  token: string,
  invitationToken: string,
): Promise<boolean> {
  const res = await api.post(`/api/v1/invitations/${invitationToken}/accept`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.ok();
}

export async function apiDeclineInvitation(
  api: APIRequestContext,
  token: string,
  invitationToken: string,
): Promise<boolean> {
  const res = await api.post(`/api/v1/invitations/${invitationToken}/decline`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  return res.ok();
}

export async function apiListStudyMembers(
  api: APIRequestContext,
  token: string,
  studyId: string,
): Promise<Array<{ userId: string; email?: string; role?: string }>> {
  const res = await api.get(`/api/v1/studies/${studyId}/members`, {
    headers: { Authorization: `Bearer ${token}` },
    failOnStatusCode: false,
  });
  if (!res.ok()) return [];
  const body = (await res.json().catch(() => ({}))) as {
    items?: Array<{ userId: string; email?: string; role?: string }>;
  };
  return body.items ?? [];
}

/**
 * Upload a real trace fixture via the API.
 * Returns the resulting traceId so detail/analysis tests can navigate to it.
 */
export async function apiUploadTrace(
  api: APIRequestContext,
  token: string,
  opts: {
    studyId: string;
    filename: string;
    mimeType: string;
    buffer: Buffer;
  },
): Promise<string | undefined> {
  const res = await api.post("/api/v1/traces/upload", {
    headers: { Authorization: `Bearer ${token}` },
    multipart: {
      studyId: opts.studyId,
      file: {
        name: opts.filename,
        mimeType: opts.mimeType,
        buffer: opts.buffer,
      },
    },
    failOnStatusCode: false,
  });
  if (!res.ok()) return undefined;
  const body = (await res.json().catch(() => ({}))) as {
    id?: string;
    traceId?: string;
  };
  return body.id ?? body.traceId;
}
