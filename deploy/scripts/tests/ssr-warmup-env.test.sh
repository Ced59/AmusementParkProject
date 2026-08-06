#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_scripts_dir="$(cd "${script_dir}/.." && pwd)"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT

export API_IMAGE='ghcr.io/example/api:test'
export FRONT_IMAGE='ghcr.io/example/front:test'
export SSR_CACHE_INVALIDATION_TOKEN='test-cache-invalidation-token'
export MONGO_INITDB_ROOT_USERNAME='test-root-user'
export MONGO_INITDB_ROOT_PASSWORD='test-root-password-value'
export MONGO_APP_USERNAME='test-app-user'
export MONGO_APP_PASSWORD='test-app-password-value'
export MINIO_ROOT_USER='test-minio-user'
export MINIO_ROOT_PASSWORD='test-minio-password-value'
export JWT_KEY='test-jwt-key-with-at-least-32-characters'

assert_env_line() {
  local env_file="$1"
  local expected_line="$2"

  if ! grep -Fqx "${expected_line}" "${env_file}"; then
    echo "Expected generated environment line was not found: ${expected_line}" >&2
    exit 1
  fi
}

default_env_file="${temp_dir}/default.env"
"${deploy_scripts_dir}/write-production-env.sh" "${default_env_file}"
assert_env_line "${default_env_file}" 'SSR_WARMUP_MAX_LOAD_PER_CPU=0.75'
assert_env_line "${default_env_file}" 'SSR_WARMUP_PRESSURE_PAUSE_SECONDS=5'

export SSR_WARMUP_MAX_LOAD_PER_CPU='0.50'
export SSR_WARMUP_PRESSURE_PAUSE_SECONDS='12.5'
custom_env_file="${temp_dir}/custom.env"
"${deploy_scripts_dir}/write-production-env.sh" "${custom_env_file}"
assert_env_line "${custom_env_file}" 'SSR_WARMUP_MAX_LOAD_PER_CPU=0.50'
assert_env_line "${custom_env_file}" 'SSR_WARMUP_PRESSURE_PAUSE_SECONDS=12.5'

echo 'SSR warmup production environment tests passed.'
