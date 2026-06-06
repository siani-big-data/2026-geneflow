import { test, expect } from "../fixtures/test";
import { randomStudyName } from "../helpers/data";

test.describe("Studies › Create", () => {
  test("opens the create-study modal", async ({ auth }) => {
    await auth.page.goto("/studies");
    await auth.page
      .getByRole("button", { name: /create study|new study/i })
      .first()
      .click();
    await expect(auth.page.getByRole("dialog")).toBeVisible();
    await expect(auth.page.getByLabel(/name|title/i).first()).toBeVisible();
  });

  test("creates a study with valid data", async ({ auth }) => {
    const name = randomStudyName();
    await auth.page.goto("/studies");
    await auth.page
      .getByRole("button", { name: /create study|new study/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    await dialog.getByLabel(/name|title/i).first().fill(name);
    const desc = dialog.getByLabel(/description|summary/i).first();
    if (await desc.isVisible().catch(() => false)) {
      await desc.fill("E2E created study");
    }
    await dialog.getByRole("button", { name: /create|save|submit/i }).click();
    await expect(auth.page.getByText(name)).toBeVisible({ timeout: 15_000 });
  });

  test("rejects empty study name", async ({ auth }) => {
    await auth.page.goto("/studies");
    await auth.page
      .getByRole("button", { name: /create study|new study/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    await dialog.getByRole("button", { name: /create|save|submit/i }).click();
    await expect(dialog.getByText(/required|empty|name/i)).toBeVisible();
  });

  test("cancel button closes the modal without creating", async ({ auth }) => {
    await auth.page.goto("/studies");
    await auth.page
      .getByRole("button", { name: /create study|new study/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    await dialog.getByRole("button", { name: /cancel|close/i }).first().click();
    await expect(dialog).toBeHidden();
  });
});
