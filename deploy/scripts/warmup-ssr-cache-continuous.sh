#!/usr/bin/env bash
set -u

cd "$(dirname "${BASH_SOURCE[0]}")/.."

mkdir -p ./warmup
continuous_lock_file="./warmup/ssr-warmup-continuous.lock"
retry_seconds="${SSR_WARMUP_CONTINUOUS_RETRY_SECONDS:-300}"
interval_seconds="${SSR_WARMUP_CONTINUOUS_INTERVAL_SECONDS:-21600}"

exec 8>"${continuous_lock_file}"
if ! flock -n 8; then
  echo "A continuous SSR warmup worker is already running."
  exit 0
fi

echo "Continuous SSR warmup worker started."
while true; do
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
