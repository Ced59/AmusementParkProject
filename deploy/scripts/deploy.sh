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
npm_docker_network_name="${NPM_DOCKER_NETWORK_NAME:-nginx-proxy-network}"
public_domain="${PUBLIC_DOMAIN:-amusement-parks.fun}"

compose() {
  docker compose --project-name "${compose_project_name}" -f compose.prod.yml "$@"
}

./scripts/validate-production-env.sh .env

if ! docker network inspect "${npm_docker_network_name}" >/dev/null 2>&1; then
  echo "Missing external Docker network '${npm_docker_network_name}'." >&2
  echo "This VPS appears to use an existing Nginx Proxy Manager network. Create it or set NPM_DOCKER_NETWORK_NAME to the actual network name." >&2
  exit 1
fi

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
if ! compose up -d --remove-orphans; then
  echo "Docker Compose failed while starting the production stack. Recent container status:" >&2
  compose ps >&2 || true
  echo "Recent API logs:" >&2
  compose logs --tail=200 api >&2 || true
  echo "Recent MongoDB logs:" >&2
  compose logs --tail=120 mongodb >&2 || true
  echo "Recent MinIO logs:" >&2
  compose logs --tail=80 minio >&2 || true
  echo "Recent Front SSR logs:" >&2
  compose logs --tail=120 front >&2 || true
  exit 1
fi

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

echo "Deployment completed. Configure Nginx Proxy Manager to forward ${public_domain} to amusementpark-front:4000 on Docker network ${npm_docker_network_name} with SSL + Force SSL."
