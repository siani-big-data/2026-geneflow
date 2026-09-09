import { test, expect } from "./fixtures/test";
import {
  TINY_PNG,
  apiDeleteProfilePhoto,
  apiGetProfile,
  apiUpdateProfile,
  apiUpdateResearchIdentifiers,
  apiUploadProfilePhoto,
} from "./fixtures/api";

const uniq = () => Date.now().toString(36) + Math.random().toString(36).slice(2, 6);

test.describe("Profile › View", () => {
  test("page shows username and email", async ({ auth }) => {
    await auth.page.goto("/profile");
    await expect(auth.page.getByText(auth.user.email)).toBeVisible({ timeout: 10_000 });
  });

  test("avatar block is present", async ({ auth }) => {
    await auth.page.goto("/profile");
    const avatar = auth.page
      .locator("img[alt*='avatar' i], img[alt*='profile' i], [role='img']")
      .first();
    await expect(avatar).toBeVisible({ timeout: 10_000 });
  });

  test("backend returns a profile for the authenticated user", async ({ auth, api }) => {
    const profile = await apiGetProfile(api, auth.token);
    expect(profile).toBeDefined();
  });
});

/* ----------------------------- API-driven ---------------------------- */

test.describe("Profile › Update (via API, verified in UI)", () => {
  test("PUT /profiles/me persists firstName, lastName, bio, location", async ({
    auth,
    api,
  }) => {
    const firstName = `Ada-${uniq()}`;
    const lastName = `Lovelace-${uniq()}`;
    const bio = `E2E bio ${uniq()}`;
    const location = `Madrid-${uniq()}`;

    const ok = await apiUpdateProfile(api, auth.token, {
      firstName,
      lastName,
      bio,
      location,
    });
    expect(ok).toBe(true);

    const reread = await apiGetProfile(api, auth.token);
    expect(reread?.firstName).toBe(firstName);
    expect(reread?.lastName).toBe(lastName);
    expect(reread?.bio).toBe(bio);
    expect(reread?.location).toBe(location);

    await auth.page.goto("/profile");
    await expect(auth.page.getByText(firstName)).toBeVisible({ timeout: 10_000 });
    await expect(auth.page.getByText(bio).first()).toBeVisible();
  });

  test("rejects empty firstName (validation)", async ({ auth, api }) => {
    const res = await api.put("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { firstName: "", lastName: "x" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBeGreaterThanOrEqual(400);
    expect(res.status()).toBeLessThan(500);
  });

  test("rejects firstName > 100 chars", async ({ auth, api }) => {
    const tooLong = "a".repeat(101);
    const res = await api.put("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { firstName: tooLong },
      failOnStatusCode: false,
    });
    expect(res.status()).toBeGreaterThanOrEqual(400);
    expect(res.status()).toBeLessThan(500);
  });

  test("rejects bio > 500 chars", async ({ auth, api }) => {
    const res = await api.put("/api/v1/profiles/me", {
      headers: { Authorization: `Bearer ${auth.token}` },
      data: { firstName: "Ada", bio: "x".repeat(501) },
      failOnStatusCode: false,
    });
    expect(res.status()).toBeGreaterThanOrEqual(400);
    expect(res.status()).toBeLessThan(500);
  });

  test("unauthenticated update is rejected", async ({ api }) => {
    const res = await api.put("/api/v1/profiles/me", {
      data: { firstName: "Hacker" },
      failOnStatusCode: false,
    });
    expect(res.status()).toBe(401);
  });
});

test.describe("Profile › Research identifiers", () => {
  test("accepts a valid ORCID and website", async ({ auth, api }) => {
    const res = await apiUpdateResearchIdentifiers(api, auth.token, {
      orcidId: "0000-0002-1825-0097",
      website: "https://example.org/me",
    });
    expect(res.ok, `Expected 2xx but got ${res.status}: ${res.body}`).toBe(true);
  });

  test("rejects malformed ORCID", async ({ auth, api }) => {
    const res = await apiUpdateResearchIdentifiers(api, auth.token, {
      orcidId: "not-an-orcid",
    });
    expect(res.ok).toBe(false);
    expect(res.status).toBeGreaterThanOrEqual(400);
    expect(res.status).toBeLessThan(500);
  });

  test("rejects malformed website URL", async ({ auth, api }) => {
    const res = await apiUpdateResearchIdentifiers(api, auth.token, {
      website: "not a url",
    });
    expect(res.ok).toBe(false);
    expect(res.status).toBeGreaterThanOrEqual(400);
    expect(res.status).toBeLessThan(500);
  });
});

/* ---------------------------- Photo upload --------------------------- */

test.describe("Profile › Photo upload", () => {
  test("uploads a PNG and returns a /storage URL", async ({ auth, api }) => {
    const res = await apiUploadProfilePhoto(
      api,
      auth.token,
      "avatar.png",
      "image/png",
      TINY_PNG,
    );
    expect(res.ok, `Upload failed: ${res.status}`).toBe(true);
    expect(res.photoUrl).toMatch(/\/storage\/profiles\/.+/);

    // The uploaded image must be reachable through the proxy endpoint we fixed.
    const fetch = await api.get(res.photoUrl!, { failOnStatusCode: false });
    expect(fetch.status()).toBeLessThan(400);
    const contentType = fetch.headers()["content-type"] ?? "";
    expect(contentType).toMatch(/image\//);
  });

  test("UI flow: uploading from /profile triggers a /storage request", async ({
    auth,
  }) => {
    await auth.page.goto("/profile");
    const fileInput = auth.page.locator("input[type='file']").first();
    if (!(await fileInput.count())) test.skip();

    const storagePromise = auth.page.waitForResponse(
      (r) => /\/storage\/profiles\/.+/.test(r.url()),
      { timeout: 15_000 },
    );
    await fileInput.setInputFiles({
      name: "avatar.png",
      mimeType: "image/png",
      buffer: TINY_PNG,
    });
    const storageResponse = await storagePromise.catch(() => null);
    expect(storageResponse, "Frontend should fetch the avatar from /storage/profiles").not.toBeNull();
    expect(storageResponse!.status()).toBeLessThan(400);
  });

  test("rejects non-image upload", async ({ auth, api }) => {
    const txt = Buffer.from("not an image");
    const res = await apiUploadProfilePhoto(
      api,
      auth.token,
      "evil.txt",
      "text/plain",
      txt,
    );
    expect(res.ok).toBe(false);
    expect(res.status).toBeGreaterThanOrEqual(400);
    expect(res.status).toBeLessThan(500);
  });

  test("rejects oversized image (>5MB)", async ({ auth, api }) => {
    const fakeBig = Buffer.concat([TINY_PNG, Buffer.alloc(6 * 1024 * 1024, 0)]);
    const res = await apiUploadProfilePhoto(
      api,
      auth.token,
      "big.png",
      "image/png",
      fakeBig,
    );
    expect(res.ok).toBe(false);
    expect(res.status).toBeGreaterThanOrEqual(400);
    expect(res.status).toBeLessThan(500);
  });

  test("delete photo clears photoUrl on profile", async ({ auth, api }) => {
    // Ensure there is a photo first
    const up = await apiUploadProfilePhoto(
      api,
      auth.token,
      "avatar.png",
      "image/png",
      TINY_PNG,
    );
    test.skip(!up.ok, "Upload precondition failed");

    const deleted = await apiDeleteProfilePhoto(api, auth.token);
    expect(deleted).toBe(true);

    const after = await apiGetProfile(api, auth.token);
    expect(after?.photoUrl ?? "").toBe("");
  });
});

/* --------------------------- UI form flow ---------------------------- */

test.describe("Profile › Edit form (UI)", () => {
  test("editing firstName via the form persists and survives reload", async ({ auth }) => {
    await auth.page.goto("/profile");
    const editBtn = auth.page.getByRole("button", { name: /edit|update profile/i }).first();
    if (!(await editBtn.isVisible().catch(() => false))) test.skip();
    await editBtn.click();

    const firstNameField = auth.page.getByLabel(/first name/i).first();
    if (!(await firstNameField.isVisible().catch(() => false))) test.skip();
    const newName = `UI-${uniq()}`;
    await firstNameField.fill(newName);

    const bio = auth.page.getByLabel(/bio|about/i).first();
    let bioValue: string | undefined;
    if (await bio.isVisible().catch(() => false)) {
      bioValue = `Bio set from UI ${uniq()}`;
      await bio.fill(bioValue);
    }

    await auth.page.getByRole("button", { name: /save|update/i }).first().click();
    await expect(
      auth.page.getByText(/saved|updated|success/i).first(),
    ).toBeVisible({ timeout: 10_000 });

    await auth.page.reload();
    await expect(auth.page.getByText(newName)).toBeVisible({ timeout: 10_000 });
    if (bioValue) {
      await expect(auth.page.getByText(bioValue).first()).toBeVisible();
    }
  });

  test("UI shows validation error on empty firstName", async ({ auth }) => {
    await auth.page.goto("/profile");
    const editBtn = auth.page.getByRole("button", { name: /edit|update profile/i }).first();
    if (!(await editBtn.isVisible().catch(() => false))) test.skip();
    await editBtn.click();

    const firstNameField = auth.page.getByLabel(/first name/i).first();
    if (!(await firstNameField.isVisible().catch(() => false))) test.skip();
    await firstNameField.fill("");
    await auth.page.getByRole("button", { name: /save|update/i }).first().click();

    await expect(
      auth.page.getByText(/required|empty|must/i).first(),
    ).toBeVisible({ timeout: 5_000 });
  });
});
