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

wait_for_service_healthy() {
  local service_name="$1"
  local timeout_seconds="${2:-120}"
  local elapsed_seconds=0
  local container_id=""
  local health_status=""

  echo "Waiting for ${service_name} service to become healthy..."

  while [ "${elapsed_seconds}" -lt "${timeout_seconds}" ]; do
    container_id="$(compose ps -q "${service_name}" 2>/dev/null || true)"

    if [ -n "${container_id}" ]; then
      health_status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "${container_id}" 2>/dev/null || true)"

      if [ "${health_status}" = "healthy" ]; then
        echo "${service_name} service is healthy."
        return 0
      fi

      if [ "${health_status}" = "unhealthy" ] || [ "${health_status}" = "exited" ] || [ "${health_status}" = "dead" ]; then
        echo "${service_name} service reached status '${health_status}'." >&2
        compose ps >&2 || true
        compose logs --tail=160 "${service_name}" >&2 || true
        return 1
      fi
    fi

    sleep 2
    elapsed_seconds=$((elapsed_seconds + 2))
  done

  echo "Timed out while waiting for ${service_name} service to become healthy after ${timeout_seconds}s." >&2
  compose ps >&2 || true
  compose logs --tail=160 "${service_name}" >&2 || true
  return 1
}

curl_with_retry() {
  local label="$1"
  local url="$2"
  local max_attempts="${3:-20}"
  local attempt=1

  echo "${label}"

  while [ "${attempt}" -le "${max_attempts}" ]; do
    if curl -fsS \
      -H "Host: ${public_domain}" \
      -H "X-Forwarded-Proto: https" \
      "${url}" >/dev/null; then
      return 0
    fi

    echo "Attempt ${attempt}/${max_attempts} failed for ${url}. Retrying..." >&2
    sleep 3
    attempt=$((attempt + 1))
  done

  echo "Health check failed after ${max_attempts} attempts: ${url}" >&2
  compose ps >&2 || true
  compose logs --tail=160 front >&2 || true
  compose logs --tail=120 api >&2 || true
  return 1
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

wait_for_service_healthy api 180
wait_for_service_healthy front 180

curl_with_retry \
  "Checking SSR frontend health on 127.0.0.1:${public_http_port}..." \
  "http://127.0.0.1:${public_http_port}/healthz"

curl_with_retry \
  "Checking API health through SSR public proxy..." \
  "http://127.0.0.1:${public_http_port}/api/health"

curl_with_retry \
  "Checking robots.txt through SSR public proxy..." \
  "http://127.0.0.1:${public_http_port}/robots.txt"


if [ "${SSR_WARMUP_AFTER_DEPLOY:-false}" = "true" ]; then
  mkdir -p ./warmup

  if [ "${SSR_WARMUP_BACKGROUND:-true}" = "true" ]; then
    warmup_log="./warmup/ssr-warmup-deploy-$(date -u +%Y%m%dT%H%M%SZ).log"
    echo "Starting optional SSR cache warmup in background. Log: ${warmup_log}"
    nohup ./scripts/warmup-ssr-cache.sh > "${warmup_log}" 2>&1 &
  else
    echo "Running optional SSR cache warmup in foreground..."
    if ! ./scripts/warmup-ssr-cache.sh; then
      if [ "${SSR_WARMUP_REQUIRED:-false}" = "true" ]; then
        echo "SSR cache warmup failed and SSR_WARMUP_REQUIRED=true." >&2
        exit 1
      fi

      echo "SSR cache warmup failed, but deployment continues because SSR_WARMUP_REQUIRED is not true." >&2
    fi
  fi
else
  echo "Optional SSR cache warmup after deploy is disabled. Set SSR_WARMUP_AFTER_DEPLOY=true to enable it."
fi

echo "Pruning old Docker images older than 7 days..."
docker image prune -af --filter "until=168h" >/dev/null || true

echo "Deployment completed. Configure Nginx Proxy Manager to forward ${public_domain} to amusementpark-front:4000 on Docker network ${npm_docker_network_name} with SSL + Force SSL."
