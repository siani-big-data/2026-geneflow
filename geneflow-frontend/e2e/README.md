# GeneFlow Frontend - E2E Tests

End-to-end tests with [Playwright](https://playwright.dev/) covering every
user-facing flow in the platform.

## Prerequisites

The tests run against the **real backend** and a real frontend dev server:

- Backend API on `http://localhost:5145`
- Frontend on `http://localhost:3000` (auto-started by Playwright)
- PostgreSQL, Redis and MinIO running (see `docker/docker-compose.yml`)

If your API runs elsewhere, set `E2E_API_URL` and `E2E_BASE_URL`.

## Running

```bash
# All tests (Chromium, headless)
npm run e2e

# Visual UI mode (recommended while developing tests)
npm run e2e:ui

# A single feature
npm run e2e:auth
npm run e2e:studies
npm run e2e:traces
npm run e2e:pipelines

# Debug a single test interactively
npm run e2e:debug -- e2e/auth/login.spec.ts

# Open the HTML report from the last run
npm run e2e:report
```

## Structure

```
e2e/
├── fixtures/
│   ├── api.ts        # Direct HTTP client (register users, login, upload traces, etc.)
│   └── test.ts       # Extended `test` with `user` + `auth` fixtures
├── helpers/
│   ├── data.ts       # Random data generators (unique per test)
│   ├── traces.ts     # Real trace fixtures from geneflow-analysis/data/
│   └── ui.ts         # Reusable UI flows (login, logout, locale switch)
├── auth/
│   ├── register.spec.ts
│   ├── login.spec.ts
│   ├── oauth-github.spec.ts
│   ├── forgot-password.spec.ts
│   ├── reset-password.spec.ts
│   ├── verify-email.spec.ts
│   ├── complete-profile.spec.ts
│   └── logout.spec.ts
├── studies/
│   ├── list.spec.ts
│   ├── create.spec.ts
│   └── detail.spec.ts   # detail, edit, archive, duplicate, collaboration, papers
├── traces/
│   ├── list.spec.ts
│   ├── upload.spec.ts
│   └── detail.spec.ts   # chromatogram, annotations, trimming, analysis
├── pipelines/
│   └── pipelines.spec.ts
├── dashboard.spec.ts
├── profile.spec.ts       # incl. photo upload via /storage proxy endpoint
├── settings.spec.ts      # general, security, 2FA, billing
├── discover.spec.ts
├── help.spec.ts
├── i18n.spec.ts          # en/es locale switching
└── landing.spec.ts
```

## Fixtures

- `api` - raw HTTP client against the backend (used for setup/teardown).
- `user` - registers a brand-new user via API and yields their credentials.
  Each test gets a unique user, so tests are isolated and parallel-safe.
- `auth` - same as `user`, but the `page` already has a valid session injected
  in `localStorage`. Use it whenever you need to start on a protected page.

```ts
import { test, expect } from "../fixtures/test";

test("dashboard works", async ({ auth }) => {
  await auth.page.goto("/dashboard");
  await expect(auth.page.getByRole("heading", { name: /dashboard/i })).toBeVisible();
});
```

## Conventions

- **Stable selectors first**: `getByRole`, `getByLabel`, `#id`. Avoid
  CSS classes - they change.
- **Unique data per test**: use the generators in `helpers/data.ts`.
- **Graceful skips**: when a feature depends on backend state that may not
  exist (e.g. trace detail without uploaded traces), use `test.skip()`
  instead of failing - we're testing presence of functionality, not
  pre-populated data.
- **OAuth & email flows**: external providers and email links are mocked
  at the network layer via `page.route()` - everything else hits the real
  backend.

## CI

Tests are configured for sequential execution (`workers: 1`) because the
backend has shared state. In CI:

- Retries: 2
- Trace + video kept on failure
- HTML report exported to `playwright-report/`

## Trace fixtures

Upload tests use **real** Sanger chromatograms and FASTA references shipped
in `geneflow-analysis/data/`:

| Format | Files |
| ------ | ----- |
| `.ab1` | `310.ab1`, `3100.ab1`, `3730.ab1`, `sanger_example.ab1` |
| `.fasta` | `BRCA1_mRNA.fasta`, `TP53_mRNA.fasta`, `ecoli_16s.fasta`, `human_mitochondrion.fasta`, `lambda_phage.fasta` |

They're loaded through `helpers/traces.ts`, which validates their presence
in `beforeAll`. If you wipe the analysis data dir, restore them from the
old backend's `sample-data/` folder.

## Known limitations

1. OAuth callback tests mock the provider response. A full OAuth test
   would need a real GitHub/Google sandbox app.
2. Tests that depend on seeded traces (`Trace Detail`, `Analysis`) skip
   when the list is empty rather than failing.
