#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ ! -f .env ]; then
  echo "Missing .env file in $(pwd)." >&2
  exit 1
fi

# shellcheck disable=SC1091
source ./scripts/env-loader.sh
load_env_file .env

compose_project_name="${COMPOSE_PROJECT_NAME:-amusementpark}"
public_http_port="${PUBLIC_HTTP_PORT:-18080}"
public_domain="${PUBLIC_DOMAIN:-amusement-parks.fun}"

compose() {
  docker compose --project-name "${compose_project_name}" -f compose.prod.yml "$@"
}

./scripts/validate-production-env.sh .env

if [ "${BACKUP_BEFORE_DEPLOY:-true}" = "true" ] && compose ps --services --filter status=running | grep -qx 'mongodb'; then
  echo "Running MongoDB backup before deployment..."
  ./scripts/backup-mongo.sh || {
    echo "MongoDB backup failed. Deployment aborted." >&2
    exit 1
  }
fi

echo "Pulling production images..."
compose pull

echo "Starting production stack..."
compose up -d --remove-orphans

compose ps

echo "Checking SSR frontend health on 127.0.0.1:${public_http_port}..."
curl -fsS \
  -H "Host: ${public_domain}" \
  -H "X-Forwarded-Proto: https" \
  "http://127.0.0.1:${public_http_port}/healthz" >/dev/null

echo "Checking API health through SSR public proxy..."
curl -fsS \
  -H "Host: ${public_domain}" \
  -H "X-Forwarded-Proto: https" \
  "http://127.0.0.1:${public_http_port}/api/health" >/dev/null

echo "Checking robots.txt through SSR public proxy..."
curl -fsS \
  -H "Host: ${public_domain}" \
  -H "X-Forwarded-Proto: https" \
  "http://127.0.0.1:${public_http_port}/robots.txt" >/dev/null

echo "Pruning old Docker images older than 7 days..."
docker image prune -af --filter "until=168h" >/dev/null || true

echo "Deployment completed. Configure Nginx Proxy Manager to forward ${public_domain} to 127.0.0.1:${public_http_port} with SSL + Force SSL."
