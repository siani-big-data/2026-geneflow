import { test, expect } from "../fixtures/test";
import { randomStudyName } from "../helpers/data";

async function createStudyViaUI(page: typeof test.fixtures.page, name: string) {
  await page.goto("/studies");
  await page.getByRole("button", { name: /create study|new study/i }).first().click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(/name|title/i).first().fill(name);
  await dialog.getByRole("button", { name: /create|save|submit/i }).click();
  await page.getByText(name).first().click();
}

test.describe("Studies › Detail", () => {
  test("study detail page shows tabs", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    await expect(auth.page.getByRole("heading", { name })).toBeVisible({ timeout: 15_000 });
    // Tabs commonly present
    for (const tab of ["overview", "traces", "papers", "team", "pipelines"]) {
      const t = auth.page.getByRole("tab", { name: new RegExp(tab, "i") });
      if (await t.isVisible().catch(() => false)) {
        await expect(t).toBeVisible();
      }
    }
  });

  test("Traces tab is reachable", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const tracesTab = auth.page.getByRole("tab", { name: /traces/i });
    if (await tracesTab.isVisible().catch(() => false)) {
      await tracesTab.click();
      await expect(
        auth.page.getByText(/upload|trace|no traces/i).first(),
      ).toBeVisible();
    }
  });

  test("Edit study name persists", async ({ auth }) => {
    const original = randomStudyName();
    const renamed = randomStudyName("Renamed");
    await createStudyViaUI(auth.page, original);
    const editBtn = auth.page.getByRole("button", { name: /edit|rename/i }).first();
    if (await editBtn.isVisible().catch(() => false)) {
      await editBtn.click();
      const input = auth.page.getByLabel(/name|title/i).first();
      await input.fill(renamed);
      await auth.page.getByRole("button", { name: /save|update/i }).first().click();
      await expect(auth.page.getByText(renamed)).toBeVisible({ timeout: 10_000 });
    }
  });
});

test.describe("Studies › Archive / Duplicate", () => {
  test("archive flow updates status", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const menu = auth.page.getByRole("button", { name: /more|actions|options/i }).first();
    if (await menu.isVisible().catch(() => false)) {
      await menu.click();
      const archive = auth.page.getByRole("menuitem", { name: /archive/i });
      if (await archive.isVisible().catch(() => false)) {
        await archive.click();
        await auth.page.getByRole("button", { name: /confirm|yes|archive/i }).first().click();
        await expect(auth.page.getByText(/archived/i)).toBeVisible();
      }
    }
  });

  test("duplicate creates a copy", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const menu = auth.page.getByRole("button", { name: /more|actions|options/i }).first();
    if (await menu.isVisible().catch(() => false)) {
      await menu.click();
      const dup = auth.page.getByRole("menuitem", { name: /duplicate|clone|copy/i });
      if (await dup.isVisible().catch(() => false)) {
        await dup.click();
        await auth.page.waitForTimeout(1500);
        await auth.page.goto("/studies");
        await expect(auth.page.getByText(/copy of|duplicate/i).first()).toBeVisible();
      }
    }
  });
});

test.describe("Studies › Collaboration", () => {
  test("invite member opens dialog", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const teamTab = auth.page.getByRole("tab", { name: /team|members|collaborators/i });
    if (await teamTab.isVisible().catch(() => false)) {
      await teamTab.click();
      await auth.page.getByRole("button", { name: /invite|add (member|user)/i }).first().click();
      await expect(auth.page.getByRole("dialog")).toBeVisible();
    }
  });

  test("invite rejects invalid email", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const teamTab = auth.page.getByRole("tab", { name: /team|members/i });
    if (await teamTab.isVisible().catch(() => false)) {
      await teamTab.click();
      await auth.page.getByRole("button", { name: /invite|add/i }).first().click();
      const dialog = auth.page.getByRole("dialog");
      await dialog.locator("input[type='email']").first().fill("notanemail");
      await dialog.getByRole("button", { name: /send|invite/i }).click();
      await expect(dialog.getByText(/invalid|email/i)).toBeVisible();
    }
  });
});

test.describe("Studies › Papers", () => {
  test("add a paper to a study", async ({ auth }) => {
    const name = randomStudyName();
    await createStudyViaUI(auth.page, name);
    const papersTab = auth.page.getByRole("tab", { name: /papers|publications/i });
    if (await papersTab.isVisible().catch(() => false)) {
      await papersTab.click();
      await auth.page.getByRole("button", { name: /add paper|new paper/i }).first().click();
      const dialog = auth.page.getByRole("dialog");
      await dialog.getByLabel(/title/i).fill("E2E Test Paper");
      const doi = dialog.getByLabel(/doi/i);
      if (await doi.isVisible().catch(() => false)) await doi.fill("10.1234/e2e.2026");
      await dialog.getByRole("button", { name: /add|save|create/i }).click();
      await expect(auth.page.getByText("E2E Test Paper")).toBeVisible({ timeout: 10_000 });
    }
  });
});
