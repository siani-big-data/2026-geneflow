/**
 * Random data generators for E2E tests.
 * Ensures each test runs against unique data so they can run in any order
 * against a real backend without collisions.
 */

export const rand = () => Math.random().toString(36).slice(2, 10);

export const randomEmail = (prefix = "e2e") =>
  `${prefix}_${rand()}@geneflow.test`;

export const randomUsername = (prefix = "user") => `${prefix}${rand()}`;

export const randomStudyName = (prefix = "Study") =>
  `${prefix} ${rand()} ${Date.now().toString(36)}`;

export const randomTraceName = (prefix = "Trace") =>
  `${prefix}_${rand()}.ab1`;

export const randomPipelineName = (prefix = "Pipeline") =>
  `${prefix} ${rand()}`;

export const strongPassword = "Passw0rd!Strong";
export const weakPassword = "12345";
