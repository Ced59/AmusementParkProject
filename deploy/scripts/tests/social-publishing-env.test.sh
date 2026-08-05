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
export EMAIL_MODE='Console'
export SOCIAL_PUBLISHING_FACEBOOK_ENABLED='true'
export SOCIAL_PUBLISHING_FACEBOOK_API_VERSION='v24.0'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID='1285475681307050'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN='test-page-access-token-value'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL='https://www.facebook.com/profile.php?id=61592732938801'
export SOCIAL_PUBLISHING_FACEBOOK_REQUEST_TIMEOUT_SECONDS='10'
export SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED='false'

valid_env_file="${temp_dir}/valid.env"
"${deploy_scripts_dir}/write-production-env.sh" "${valid_env_file}"

assert_env_line() {
  local expected_line="$1"

  if ! grep -Fqx "${expected_line}" "${valid_env_file}"; then
    echo "Expected generated environment line was not found: ${expected_line}" >&2
    exit 1
  fi
}

assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_ENABLED=true'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_API_VERSION=v24.0'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID=1285475681307050'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN=test-page-access-token-value'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL=https://www.facebook.com/profile.php?id=61592732938801'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_REQUEST_TIMEOUT_SECONDS=10'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED=false'

"${deploy_scripts_dir}/validate-production-env.sh" "${valid_env_file}"

export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN=''
invalid_env_file="${temp_dir}/missing-token.env"
"${deploy_scripts_dir}/write-production-env.sh" "${invalid_env_file}"

if "${deploy_scripts_dir}/validate-production-env.sh" "${invalid_env_file}" >/dev/null 2>&1; then
  echo 'Validation unexpectedly accepted enabled Facebook publishing without a Page Access Token.' >&2
  exit 1
fi

echo 'Social publishing production environment tests passed.'
