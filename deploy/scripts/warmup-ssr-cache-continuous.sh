#!/usr/bin/env bash
set -u

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ -f .env ]; then
  # shellcheck disable=SC1091
  source ./scripts/env-loader.sh
  load_env_file .env
fi

mkdir -p ./warmup
continuous_lock_file="./warmup/ssr-warmup-continuous.lock"
retry_seconds="${SSR_WARMUP_CONTINUOUS_RETRY_SECONDS:-300}"
interval_seconds="${SSR_WARMUP_CONTINUOUS_INTERVAL_SECONDS:-21600}"
artifact_retention_days="${SSR_WARMUP_ARTIFACT_RETENTION_DAYS:-7}"

require_positive_integer() {
  local name="$1"
  local value="$2"

  case "${value}" in
    ''|*[!0-9]*|0)
      echo "${name} must be a positive integer; received '${value}'." >&2
      exit 1
      ;;
  esac
}

require_positive_integer SSR_WARMUP_CONTINUOUS_RETRY_SECONDS "${retry_seconds}"
require_positive_integer SSR_WARMUP_CONTINUOUS_INTERVAL_SECONDS "${interval_seconds}"
require_positive_integer SSR_WARMUP_ARTIFACT_RETENTION_DAYS "${artifact_retention_days}"

exec 8>"${continuous_lock_file}"
if ! flock -n 8; then
  echo "A continuous SSR warmup worker is already running; supervision will retry." >&2
  exit 1
fi

echo "Continuous SSR warmup worker started."
while true; do
  find ./warmup -maxdepth 1 -type f \
    \( -name 'ssr-warmup-*.log' -o -name 'ssr-warmup-report-*.csv' \) \
    -mtime "+${artifact_retention_days}" -delete

  cycle_started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "${cycle_started_at} | Starting SSR warmup cycle."

  if SSR_WARMUP_FAIL_IF_LOCKED=true ./scripts/warmup-ssr-cache.sh; then
    echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) | SSR warmup cycle completed; next cycle in ${interval_seconds}s."
    sleep "${interval_seconds}"
  else
    echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) | SSR warmup cycle failed; retrying in ${retry_seconds}s." >&2
    sleep "${retry_seconds}"
  fi
done
