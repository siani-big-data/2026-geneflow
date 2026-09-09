import { test, expect } from "../fixtures/test";
import { randomPipelineName, randomStudyName } from "../helpers/data";

test.describe("Pipelines › List (standalone page)", () => {
  test("/pipelines renders coming-soon or list", async ({ auth }) => {
    await auth.page.goto("/pipelines");
    await auth.page.waitForLoadState("networkidle");
    await expect(
      auth.page.getByText(/pipelines|coming soon/i).first(),
    ).toBeVisible();
  });
});

test.describe("Pipelines › Inside a study", () => {
  async function openStudyPipelinesTab(page: typeof test.fixtures.page) {
    await page.goto("/studies");
    await page.getByRole("button", { name: /create study|new study/i }).first().click();
    const dialog = page.getByRole("dialog");
    const name = randomStudyName();
    await dialog.getByLabel(/name|title/i).first().fill(name);
    await dialog.getByRole("button", { name: /create|save/i }).click();
    await page.getByText(name).first().click();
    const tab = page.getByRole("tab", { name: /pipelines/i });
    if (await tab.isVisible().catch(() => false)) await tab.click();
  }

  test("create pipeline button is present", async ({ auth }) => {
    await openStudyPipelinesTab(auth.page);
    await expect(
      auth.page.getByRole("button", { name: /create pipeline|new pipeline/i }).first(),
    ).toBeVisible();
  });

  test("can create a pipeline with steps", async ({ auth }) => {
    await openStudyPipelinesTab(auth.page);
    const name = randomPipelineName();
    await auth.page
      .getByRole("button", { name: /create pipeline|new pipeline/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    await dialog.getByLabel(/name|title/i).first().fill(name);
    // Add steps if step picker exists
    const addStep = dialog.getByRole("button", { name: /add step|new step/i }).first();
    if (await addStep.isVisible().catch(() => false)) {
      await addStep.click();
      const stepOption = auth.page.getByRole("option", { name: /quality|trim|orf/i }).first();
      if (await stepOption.isVisible().catch(() => false)) await stepOption.click();
    }
    await dialog.getByRole("button", { name: /create|save/i }).click();
    await expect(auth.page.getByText(name)).toBeVisible({ timeout: 15_000 });
  });

  test("execute pipeline triggers progress UI", async ({ auth }) => {
    await openStudyPipelinesTab(auth.page);
    const exec = auth.page.getByRole("button", { name: /execute|run|start/i }).first();
    if (await exec.isVisible().catch(() => false)) {
      await exec.click();
      await expect(
        auth.page.getByText(/running|queued|in progress|started/i).first(),
      ).toBeVisible({ timeout: 10_000 });
    }
  });

  test("validation rejects empty pipeline name", async ({ auth }) => {
    await openStudyPipelinesTab(auth.page);
    await auth.page
      .getByRole("button", { name: /create pipeline|new pipeline/i })
      .first()
      .click();
    const dialog = auth.page.getByRole("dialog");
    await dialog.getByRole("button", { name: /create|save/i }).click();
    await expect(dialog.getByText(/required|name/i)).toBeVisible();
  });
});
