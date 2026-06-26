# GeneFlow — Continuous Deployment (push → Hetzner)

Every push to `master` in any of the 5 repos builds a Docker image, pushes it to
**GHCR**, then SSHes into the Hetzner host and restarts that one service:

```
push master ─▶ GitHub Actions (build + push GHCR) ─▶ ssh host ─▶ docker compose pull <svc> && up -d <svc>
```

| Repo | Image | Compose service |
|------|-------|-----------------|
| GeneFlow-Backend  | `ghcr.io/geneflow-app/geneflow-backend`  | `api` |
| GeneFlow-Frontend | `ghcr.io/geneflow-app/geneflow-frontend` | `frontend` |
| GeneFlow-AI       | `ghcr.io/geneflow-app/geneflow-ai`       | `ai` |
| GeneFlow-Analysis | `ghcr.io/geneflow-app/geneflow-analysis` | `worker` |
| GeneFlow-Datalake | `ghcr.io/geneflow-app/geneflow-datalake` | `datalake` |

The full stack is defined in [`docker-compose.prod.yml`](./docker-compose.prod.yml)
and lives on the host at `/opt/geneflow/docker-compose.yml`.

---

## 1. Provision the Hetzner host (one time)

1. Create a Hetzner Cloud server (Ubuntu 24.04, CX/CPX = amd64).
2. Create an SSH key pair for CI **on your machine** (no passphrase):
   ```bash
   ssh-keygen -t ed25519 -f geneflow_deploy -C "github-actions-deploy"
   ```
   Add the **public** key (`geneflow_deploy.pub`) to the host's
   `~/.ssh/authorized_keys` (a dedicated `deploy` user is recommended).
3. Copy this `deploy/` folder to the host and run the bootstrap (installs Docker,
   firewall, GHCR login, lays down the stack):
   ```bash
   scp -r deploy deploy-user@HOST:/tmp/geneflow-deploy
   ssh deploy-user@HOST
   GHCR_USER=<github-username> GHCR_TOKEN=<PAT read:packages> \
     bash /tmp/geneflow-deploy/bootstrap.sh
   ```
4. Fill secrets and start:
   ```bash
   sudo nano /opt/geneflow/.env          # use deploy/.env.prod.example as the guide
   cd /opt/geneflow
   docker compose pull && docker compose up -d
   docker compose ps
   ```
5. Point DNS at the host: `A` records for `API_DOMAIN` (api.geneflow.app) and
   `FRONTEND_DOMAIN` (app.geneflow.app). Caddy fetches TLS certs automatically.

> The backend applies EF Core migrations automatically on startup
> (`DatabaseExtensions.MigrateAsync`), so no manual DB migration step is needed.

---

## 2. GitHub configuration (one time)

Set these at the **organization** level (`geneflow-app` → Settings) so all 5 repos
share them. Restrict the secrets to these repositories.

### Secrets (Settings → Secrets and variables → Actions → Secrets)
| Name | Value |
|------|-------|
| `DEPLOY_HOST` | Hetzner public IP / hostname |
| `DEPLOY_USER` | SSH user (e.g. `deploy`) |
| `DEPLOY_SSH_KEY` | **private** key from step 1.2 (full contents of `geneflow_deploy`) |
| `DEPLOY_PORT` | *(optional)* SSH port, defaults to `22` |

### Variables (Settings → Secrets and variables → Actions → Variables)
| Name | Value |
|------|-------|
| `DEPLOY_PATH` | *(optional)* defaults to `/opt/geneflow` |

### Frontend repo only — build-time public config (Variables)
These are inlined into the JS bundle at build time, so they are **build args**,
not runtime env. All are public values (safe as variables, not secrets):

| Name | Example |
|------|---------|
| `NEXT_PUBLIC_API_URL` | `https://api.geneflow.app` |
| `NEXT_PUBLIC_AI_API_URL` | `https://api.geneflow.app/ai` |
| `NEXT_PUBLIC_GITHUB_CLIENT_ID` | `Ov23li...` |
| `NEXT_PUBLIC_GOOGLE_CLIENT_ID` | `1005...apps.googleusercontent.com` |
| `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` | `pk_live_...` |

> The `GITHUB_TOKEN` used for pushing images is provided automatically by Actions;
> no PAT is needed in CI. The PAT is only used **on the host** to *pull* private images.

---

## 3. How a deploy flows after setup

1. `git push` to `master` on any repo.
2. CI builds the image and pushes `…:latest` (+ `…:sha-<commit>`) to GHCR.
3. The `deploy` job SSHes to the host and runs, for that service only:
   ```bash
   cd /opt/geneflow
   docker compose pull <service>
   docker compose up -d <service>
   docker image prune -f
   ```
4. Only the changed service restarts; the rest keep running.

Manual redeploy / rollback on the host:
```bash
cd /opt/geneflow
# redeploy everything
docker compose pull && docker compose up -d
# pin a service to a specific commit (rollback)
echo "BACKEND_TAG=sha-<commit>" >> .env && docker compose up -d api
```

---

## Notes & caveats
- **Architecture:** Python images build multi-arch (amd64+arm64). The backend and
  frontend build amd64 only — fine for Hetzner CX/CPX. For an ARM (CAX) box, add
  `platforms: linux/amd64,linux/arm64` to those two `cd.yml` build steps.
- **Object storage / vector DB:** the stack runs all Python services with *local*
  storage volumes (their defaults). If AI needs Qdrant or datalake needs MinIO in
  production, add those services to `docker-compose.prod.yml` and wire the env.
- **Secrets in `appsettings.json`:** the repo's committed `appsettings.json` holds
  dev secrets (Stripe/Resend/OAuth). Production overrides them via `.env`, but those
  committed dev secrets should still be rotated.
- **First boot:** `docker compose logs -f caddy` to watch certificate issuance; if you
  hit Let's Encrypt rate limits while testing, uncomment the staging CA in `config/Caddyfile`.
