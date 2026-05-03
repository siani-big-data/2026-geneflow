# CI/CD Pipeline

## GitHub Actions Workflows

### `build.yml` - CI
Ejecuta en cada push/PR a `main`, `develop`, `feature/**`:

1. **Build** - Restore + Build Release
2. **Test** - Unit tests + Coverage report
3. **Lint** - `dotnet format --verify-no-changes`
4. **Docker** - Build imagen (solo main/develop)

### `deploy.yml` - CD
Ejecuta en push a `main`:

1. Build imagen Docker → GitHub Container Registry
2. SSH a Hetzner
3. `docker compose pull` + `up -d`
4. Health checks (API, Redis, PostgreSQL)
5. Notificación Discord (opcional)

## Secrets Requeridos

| Secret | Descripción |
|--------|-------------|
| `HETZNER_SSH_KEY` | Clave SSH privada |
| `HETZNER_HOST` | IP del servidor |
| `HETZNER_USER` | Usuario SSH (default: deploy) |
| `DISCORD_WEBHOOK_URL` | Opcional |

## Docker Compose

```
postgres  ← Datamart (reconstruible)
redis     ← Event Bus (Streams)
api       ← .NET 8 API
worker    ← Python trace processor
datalake  ← Event Store + Mounter
caddy     ← Reverse proxy + SSL
```

**Nota:** No hay `init.sql`. El PostgreSQL Mounter crea schemas/tablas desde eventos.

## Comandos

```bash
# Local
docker compose up -d
docker compose logs -f api

# Deploy manual
ssh deploy@server "cd ~/geneflow && docker compose pull && docker compose up -d"
```
