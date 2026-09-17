#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_scripts_dir="$(cd "${script_dir}/.." && pwd)"
repo_root="$(cd "${deploy_scripts_dir}/../.." && pwd)"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT

grep -Fq "github.event_name == 'workflow_dispatch' && github.run_attempt == 1 && inputs.shared_infrastructure_maintenance" \
  "${repo_root}/.github/workflows/production.yml"
grep -Fq 'mongodb_backup_completed=true' "${deploy_scripts_dir}/deploy.sh"
grep -Fq 'MongoDB maintenance requires a successful backup in this deployment run.' \
  "${deploy_scripts_dir}/deploy.sh"
rolling_guard_line="$(grep -nF 'if [ "${deploy_zero_downtime_enabled}" != "true" ]; then' "${deploy_scripts_dir}/deploy.sh" | cut -d: -f1)"
maintenance_line="$(grep -nF 'python3 ./scripts/deployment_transaction.py maintain-mongodb' "${deploy_scripts_dir}/deploy.sh" | cut -d: -f1)"
if [ -z "${rolling_guard_line}" ] || [ -z "${maintenance_line}" ] || [ "${rolling_guard_line}" -ge "${maintenance_line}" ]; then
  echo 'Transactional rolling mode must be validated before MongoDB maintenance.' >&2
  exit 1
fi

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
export TRIP_FINGERPRINT_CURRENT_VERSION='v1'
export TRIP_FINGERPRINT_CURRENT_KEY='dGVzdC10cmlwLWZpbmdlcnByaW50LWtleS0zMi1ieXRlcy1taW5pbXVt'
export TRIP_FINGERPRINT_PREVIOUS_KEYS=''
export EMAIL_MODE='Console'

default_env_file="${temp_dir}/default.env"
"${deploy_scripts_dir}/write-production-env.sh" "${default_env_file}"
grep -Fqx 'SHARED_INFRASTRUCTURE_MAINTENANCE=none' "${default_env_file}"
"${deploy_scripts_dir}/validate-production-env.sh" "${default_env_file}"

export SHARED_INFRASTRUCTURE_MAINTENANCE='mongodb'
export BACKUP_BEFORE_DEPLOY='true'
maintenance_env_file="${temp_dir}/maintenance.env"
"${deploy_scripts_dir}/write-production-env.sh" "${maintenance_env_file}"
grep -Fqx 'SHARED_INFRASTRUCTURE_MAINTENANCE=mongodb' "${maintenance_env_file}"
"${deploy_scripts_dir}/validate-production-env.sh" "${maintenance_env_file}"

export BACKUP_BEFORE_DEPLOY='false'
unsafe_env_file="${temp_dir}/unsafe.env"
"${deploy_scripts_dir}/write-production-env.sh" "${unsafe_env_file}"
if "${deploy_scripts_dir}/validate-production-env.sh" "${unsafe_env_file}" >/dev/null 2>&1; then
  echo 'MongoDB maintenance without a backup was unexpectedly accepted.' >&2
  exit 1
fi

export BACKUP_BEFORE_DEPLOY='true'
export SHARED_INFRASTRUCTURE_MAINTENANCE='all'
unsupported_env_file="${temp_dir}/unsupported.env"
"${deploy_scripts_dir}/write-production-env.sh" "${unsupported_env_file}"
if "${deploy_scripts_dir}/validate-production-env.sh" "${unsupported_env_file}" >/dev/null 2>&1; then
  echo 'An unsupported shared infrastructure maintenance target was unexpectedly accepted.' >&2
  exit 1
fi

echo 'Shared infrastructure maintenance environment tests passed.'
