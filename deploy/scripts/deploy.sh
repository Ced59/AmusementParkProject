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
deploy_lock_file="${DEPLOY_LOCK_FILE:-/tmp/amusementpark-deploy.lock}"
deploy_lock_timeout_seconds="${DEPLOY_LOCK_TIMEOUT_SECONDS:-900}"
deploy_lock_wait_log_interval_seconds="${DEPLOY_LOCK_WAIT_LOG_INTERVAL_SECONDS:-30}"
deploy_compose_log_timeout_seconds="${DEPLOY_COMPOSE_LOG_TIMEOUT_SECONDS:-30}"
deploy_compose_up_timeout_seconds="${DEPLOY_COMPOSE_UP_TIMEOUT_SECONDS:-300}"
deploy_docker_prune_timeout_seconds="${DEPLOY_DOCKER_PRUNE_TIMEOUT_SECONDS:-120}"
deploy_zero_downtime_enabled="${DEPLOY_ZERO_DOWNTIME_ENABLED:-true}"
continuous_warmup_service_name="amusementpark-ssr-warmup.service"

compose() {
  docker compose --project-name "${compose_project_name}" -f compose.prod.yml "$@"
}

run_with_timeout() {
  local timeout_seconds="$1"
  shift

  if command -v timeout >/dev/null 2>&1; then
    timeout --preserve-status "${timeout_seconds}" "$@"
  else
    "$@"
  fi
}

compose_with_timeout() {
  local timeout_seconds="$1"
  shift

  run_with_timeout "${timeout_seconds}" docker compose --project-name "${compose_project_name}" -f compose.prod.yml "$@"
}

reconcile_continuous_warmup_service() {
  local deploy_directory
  local service_file
  local temporary_service_file

  service_file="/etc/systemd/system/${continuous_warmup_service_name}"

  if [ "${SSR_WARMUP_CONTINUOUS_ENABLED:-false}" != "true" ]; then
    if command -v systemctl >/dev/null 2>&1; then
      systemctl disable --now "${continuous_warmup_service_name}" >/dev/null 2>&1 || true
      if [ -f "${service_file}" ]; then
        rm -f "${service_file}"
        systemctl daemon-reload
      fi
    fi
    return 0
  fi

  if [ "${SSR_WARMUP_AFTER_DEPLOY:-false}" != "true" ]; then
    echo "SSR_WARMUP_CONTINUOUS_ENABLED requires SSR_WARMUP_AFTER_DEPLOY=true." >&2
    exit 1
  fi

  if [ "$(id -u)" -ne 0 ] || ! command -v systemctl >/dev/null 2>&1; then
    echo "Continuous SSR warmup requires root and systemd supervision." >&2
    exit 1
  fi

  deploy_directory="$(pwd -P)"
  temporary_service_file="$(mktemp)"
  {
    printf '%s\n' \
      '[Unit]' \
      'Description=AmusementPark continuous SSR cache warmup' \
      'After=network-online.target docker.service' \
      'Wants=network-online.target' \
      '' \
      '[Service]' \
      'Type=simple' \
      "WorkingDirectory=${deploy_directory}" \
      "ExecStart=${deploy_directory}/scripts/warmup-ssr-cache-continuous.sh" \
      'Restart=on-failure' \
      'RestartSec=30' \
      'KillMode=control-group' \
      '' \
      '[Install]' \
      'WantedBy=multi-user.target'
  } > "${temporary_service_file}"

  install -m 0644 "${temporary_service_file}" "${service_file}"
  rm -f "${temporary_service_file}"
  systemctl daemon-reload
  systemctl enable "${continuous_warmup_service_name}" >/dev/null
  systemctl restart "${continuous_warmup_service_name}"
  echo "Continuous SSR warmup service is enabled and running."
}

compose_logs() {
  local tail_lines="$1"
  local service_name="$2"

  compose_with_timeout "${deploy_compose_log_timeout_seconds}" logs --tail="${tail_lines}" "${service_name}"
}

if ! command -v flock >/dev/null 2>&1; then
  echo "Missing required flock command; deployment locking cannot be enforced." >&2
  exit 1
fi

exec 9>"${deploy_lock_file}"

acquire_deployment_lock() {
  local started_seconds="${SECONDS}"
  local elapsed_seconds=0
  local sleep_seconds=0

  echo "Acquiring deployment lock ${deploy_lock_file}..."

  while ! flock -n 9; do
    elapsed_seconds=$((SECONDS - started_seconds))

    if [ "${elapsed_seconds}" -ge "${deploy_lock_timeout_seconds}" ]; then
      echo "Timed out while waiting for deployment lock after ${deploy_lock_timeout_seconds}s." >&2
      return 1
    fi

    echo "Deployment lock is still held after ${elapsed_seconds}s; waiting up to ${deploy_lock_timeout_seconds}s..."
    sleep_seconds="${deploy_lock_wait_log_interval_seconds}"
    if [ "${sleep_seconds}" -lt 1 ]; then
      sleep_seconds=1
    fi
    if [ $((elapsed_seconds + sleep_seconds)) -gt "${deploy_lock_timeout_seconds}" ]; then
      sleep_seconds=$((deploy_lock_timeout_seconds - elapsed_seconds))
    fi

    sleep "${sleep_seconds}"
  done

  echo "Deployment lock acquired."
}

acquire_deployment_lock

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
        compose_logs 160 "${service_name}" >&2 || true
        return 1
      fi
    fi

    sleep 2
    elapsed_seconds=$((elapsed_seconds + 2))
  done

  echo "Timed out while waiting for ${service_name} service to become healthy after ${timeout_seconds}s." >&2
  compose ps >&2 || true
  compose_logs 160 "${service_name}" >&2 || true
  return 1
}

service_is_healthy() {
  local service_name="$1"
  local container_id=""
  local health_status=""

  container_id="$(compose ps -q "${service_name}" 2>/dev/null || true)"
  if [ -z "${container_id}" ]; then
    return 1
  fi

  health_status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "${container_id}" 2>/dev/null || true)"
  [ "${health_status}" = "healthy" ]
}

wait_for_container_healthy() {
  local container_name="$1"
  local timeout_seconds="${2:-180}"
  local elapsed_seconds=0
  local health_status=""

  while [ "${elapsed_seconds}" -lt "${timeout_seconds}" ]; do
    health_status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "${container_name}" 2>/dev/null || true)"
    if [ "${health_status}" = "healthy" ]; then
      echo "${container_name} is healthy."
      return 0
    fi
    if [ "${health_status}" = "unhealthy" ] || [ "${health_status}" = "exited" ] || [ "${health_status}" = "dead" ]; then
      echo "${container_name} reached status '${health_status}'." >&2
      docker logs --tail=160 "${container_name}" >&2 || true
      return 1
    fi
    sleep 2
    elapsed_seconds=$((elapsed_seconds + 2))
  done

  echo "Timed out while waiting for ${container_name} after ${timeout_seconds}s." >&2
  docker logs --tail=160 "${container_name}" >&2 || true
  return 1
}

start_deploy_candidate() {
  local service_name="$1"
  local container_name="$2"

  echo "Starting healthy ${service_name} deployment candidate ${container_name}..."
  compose run -d --no-deps --name "${container_name}" "${service_name}" >/dev/null
  wait_for_container_healthy "${container_name}" 180
}

attach_candidate_aliases() {
  local service_name="$1"
  local container_name="$2"
  local backend_network="${compose_project_name}_backend_private"

  docker network disconnect "${backend_network}" "${container_name}"
  if [ "${service_name}" = "api" ]; then
    docker network connect --alias api --alias amusementpark-api "${backend_network}" "${container_name}"
  else
    docker network connect --alias front --alias amusementpark-front "${backend_network}" "${container_name}"
    wait_for_container_healthy "${container_name}" 30
    docker network disconnect "${npm_docker_network_name}" "${container_name}"
    docker network connect \
      --alias amusementpark-front \
      --alias amusementpark-front-ssr \
      "${npm_docker_network_name}" \
      "${container_name}"
  fi

  wait_for_container_healthy "${container_name}" 30
  echo "Production aliases attached to healthy ${container_name}."
}

api_candidate_name=""
front_candidate_name=""

cleanup_deploy_candidates() {
  local candidate_name=""
  for candidate_name in "${front_candidate_name}" "${api_candidate_name}"; do
    if [ -n "${candidate_name}" ] && docker inspect "${candidate_name}" >/dev/null 2>&1; then
      echo "Removing deployment candidate ${candidate_name}..."
      docker rm -f "${candidate_name}" >/dev/null || true
    fi
  done
}

trap cleanup_deploy_candidates EXIT

run_legacy_enum_migrations() {
  local migration_script="./scripts/migrate-legacy-enums-1.10.0.js"
  local mongo_database_name="${MONGO_DATABASE_NAME:-AmusementPark}"
  local dry_run="${LEGACY_ENUM_MIGRATIONS_DRY_RUN:-false}"

  if [ ! -f "${migration_script}" ]; then
    echo "Missing legacy enum migration script: ${migration_script}" >&2
    return 1
  fi

  echo "Running legacy enum MongoDB migrations..."
  compose exec -T \
    -e MONGO_APP_DATABASE="${mongo_database_name}" \
    -e DRY_RUN="${dry_run}" \
    mongodb mongosh --quiet \
      --username "${MONGO_APP_USERNAME:?MONGO_APP_USERNAME is required}" \
      --password "${MONGO_APP_PASSWORD:?MONGO_APP_PASSWORD is required}" \
      --authenticationDatabase "${mongo_database_name}" \
      "${mongo_database_name}" < "${migration_script}"
}

run_opening_hours_localized_notes_migration() {
  local migration_script="./scripts/migrate-opening-hours-localized-notes-3.0.5.js"
  local mongo_database_name="${MONGO_DATABASE_NAME:-AmusementPark}"
  local dry_run="${OPENING_HOURS_LOCALIZED_NOTES_MIGRATION_DRY_RUN:-false}"

  if [ ! -f "${migration_script}" ]; then
    echo "Missing opening-hours localized notes migration script: ${migration_script}" >&2
    return 1
  fi

  echo "Running opening-hours localized notes MongoDB migration..."
  compose exec -T \
    -e MONGO_APP_DATABASE="${mongo_database_name}" \
    -e DRY_RUN="${dry_run}" \
    mongodb mongosh --quiet \
      --username "${MONGO_APP_USERNAME:?MONGO_APP_USERNAME is required}" \
      --password "${MONGO_APP_PASSWORD:?MONGO_APP_PASSWORD is required}" \
      --authenticationDatabase "${mongo_database_name}" \
      "${mongo_database_name}" < "${migration_script}"
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
  compose_logs 160 front >&2 || true
  compose_logs 120 api >&2 || true
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

rolling_deploy=false
if [ "${deploy_zero_downtime_enabled}" = "true" ] && service_is_healthy api && service_is_healthy front; then
  rolling_deploy=true
  candidate_suffix="$(date -u +%Y%m%dT%H%M%SZ)-$$"
  api_candidate_name="${compose_project_name}-api-candidate-${candidate_suffix}"
  front_candidate_name="${compose_project_name}-front-candidate-${candidate_suffix}"
  start_deploy_candidate api "${api_candidate_name}"
  attach_candidate_aliases api "${api_candidate_name}"
  start_deploy_candidate front "${front_candidate_name}"
  attach_candidate_aliases front "${front_candidate_name}"
else
  echo "Healthy API/front stack not available or zero-downtime disabled; using the standard startup path."
fi

echo "Starting production stack..."
if ! compose_with_timeout "${deploy_compose_up_timeout_seconds}" up -d; then
  echo "Docker Compose failed while starting the production stack. Recent container status:" >&2
  compose ps >&2 || true
  echo "Recent API logs:" >&2
  compose_logs 200 api >&2 || true
  echo "Recent MongoDB logs:" >&2
  compose_logs 120 mongodb >&2 || true
  echo "Recent MinIO logs:" >&2
  compose_logs 80 minio >&2 || true
  echo "Recent Front SSR logs:" >&2
  compose_logs 120 front >&2 || true
  exit 1
fi

compose ps

wait_for_service_healthy mongodb 180

if [ "${RUN_OPENING_HOURS_LOCALIZED_NOTES_MIGRATION:-true}" = "true" ]; then
  run_opening_hours_localized_notes_migration
else
  echo "Opening-hours localized notes MongoDB migration is disabled. Set RUN_OPENING_HOURS_LOCALIZED_NOTES_MIGRATION=true to enable it."
fi

if [ "${RUN_LEGACY_ENUM_MIGRATIONS:-false}" = "true" ]; then
  run_legacy_enum_migrations
else
  echo "Legacy enum MongoDB migrations are disabled. Set RUN_LEGACY_ENUM_MIGRATIONS=true to enable them."
fi

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

if [ "${rolling_deploy}" = "true" ]; then
  echo "Canonical services are healthy; removing deployment candidates."
  cleanup_deploy_candidates
  front_candidate_name=""
  api_candidate_name=""
  compose_with_timeout "${deploy_compose_up_timeout_seconds}" up -d --remove-orphans
fi

reconcile_continuous_warmup_service

if [ "${SSR_WARMUP_AFTER_DEPLOY:-false}" = "true" ]; then
  mkdir -p ./warmup

  if [ "${SSR_WARMUP_CONTINUOUS_ENABLED:-false}" = "true" ]; then
    echo "Continuous SSR cache warmup is managed by ${continuous_warmup_service_name}."
  elif [ "${SSR_WARMUP_BACKGROUND:-true}" = "true" ]; then
    warmup_log="./warmup/ssr-warmup-deploy-$(date -u +%Y%m%dT%H%M%SZ).log"
    echo "Starting optional SSR cache warmup in background. Log: ${warmup_log}"
    # Do not let the optional background warmup keep the deployment lock open.
    nohup ./scripts/warmup-ssr-cache.sh > "${warmup_log}" 2>&1 9>&- &
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
run_with_timeout "${deploy_docker_prune_timeout_seconds}" docker image prune -af --filter "until=168h" >/dev/null || true

echo "Deployment completed. Configure Nginx Proxy Manager to forward ${public_domain} to amusementpark-front:4000 on Docker network ${npm_docker_network_name} with SSL + Force SSL."
