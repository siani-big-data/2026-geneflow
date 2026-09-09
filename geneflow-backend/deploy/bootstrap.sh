#!/usr/bin/env bash
# =============================================================================
# GeneFlow - Hetzner host bootstrap (run once on a fresh Ubuntu/Debian box)
# =============================================================================
# Installs Docker, logs in to GHCR, opens the firewall, lays down the compose
# stack under /opt/geneflow and starts it. Re-running is safe (idempotent-ish).
#
# Usage (as root or a sudo user):
#   GHCR_USER=<github-username> GHCR_TOKEN=<PAT-with-read:packages> \
#     bash bootstrap.sh
#
# After it finishes: edit /opt/geneflow/.env with real secrets, then:
#   cd /opt/geneflow && docker compose pull && docker compose up -d
# =============================================================================
set -euo pipefail

APP_DIR=/opt/geneflow
REPO_RAW="https://raw.githubusercontent.com/geneflow-app/GeneFlow-Backend/master/deploy"

log() { echo -e "\033[1;32m[bootstrap]\033[0m $*"; }
die() { echo -e "\033[1;31m[bootstrap] ERROR:\033[0m $*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || SUDO=sudo
SUDO=${SUDO:-}

# --- 1. Docker engine + compose plugin --------------------------------------
if ! command -v docker >/dev/null 2>&1; then
  log "Installing Docker Engine..."
  curl -fsSL https://get.docker.com | $SUDO sh
else
  log "Docker already installed: $(docker --version)"
fi
docker compose version >/dev/null 2>&1 || die "docker compose plugin missing"

# --- 2. Firewall (ufw) ------------------------------------------------------
if command -v ufw >/dev/null 2>&1; then
  log "Configuring firewall (allow SSH, HTTP, HTTPS)..."
  $SUDO ufw allow OpenSSH    || true
  $SUDO ufw allow 80/tcp     || true
  $SUDO ufw allow 443/tcp    || true
  $SUDO ufw --force enable   || true
else
  log "ufw not present; skipping firewall (configure Hetzner Cloud Firewall instead)."
fi

# --- 3. GHCR login (needed to pull private images) --------------------------
if [ -n "${GHCR_TOKEN:-}" ] && [ -n "${GHCR_USER:-}" ]; then
  log "Logging in to ghcr.io as $GHCR_USER..."
  echo "$GHCR_TOKEN" | $SUDO docker login ghcr.io -u "$GHCR_USER" --password-stdin
else
  log "GHCR_USER/GHCR_TOKEN not set — skipping registry login."
  log "If images are private, run: echo <PAT> | docker login ghcr.io -u <user> --password-stdin"
fi

# --- 4. Stack files under /opt/geneflow -------------------------------------
log "Preparing $APP_DIR ..."
$SUDO mkdir -p "$APP_DIR/config"

fetch() { # fetch <remote-path> <local-path> (only if missing)
  local dest="$APP_DIR/$2"
  if [ -f "$dest" ]; then log "keep existing $2"; return; fi
  log "fetch $2"
  $SUDO curl -fsSL "$REPO_RAW/$1" -o "$dest" || die "could not fetch $1"
}

# Prefer files copied next to this script; otherwise pull from the repo.
SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SRC_DIR/docker-compose.prod.yml" ]; then
  log "Copying stack files from $SRC_DIR ..."
  $SUDO cp "$SRC_DIR/docker-compose.prod.yml" "$APP_DIR/docker-compose.yml"
  $SUDO cp "$SRC_DIR/config/Caddyfile"        "$APP_DIR/config/Caddyfile"
  [ -f "$APP_DIR/.env" ] || $SUDO cp "$SRC_DIR/.env.prod.example" "$APP_DIR/.env"
else
  fetch "docker-compose.prod.yml" "docker-compose.yml"
  fetch "config/Caddyfile"        "config/Caddyfile"
  [ -f "$APP_DIR/.env" ] || fetch ".env.prod.example" ".env"
fi

log "Done. Next steps:"
echo "  1. Edit $APP_DIR/.env with real secrets and domains."
echo "  2. Point DNS (API_DOMAIN, FRONTEND_DOMAIN) at this host."
echo "  3. cd $APP_DIR && docker compose pull && docker compose up -d"
echo "  4. Check: docker compose ps && docker compose logs -f caddy"
