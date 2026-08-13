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
export FACEBOOK_APP_ID='123456789012345'
export SOCIAL_PUBLISHING_FACEBOOK_ENABLED='true'
export SOCIAL_PUBLISHING_FACEBOOK_API_VERSION='v24.0'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID='1285475681307050'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN='test-page-access-token-value'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL='https://www.facebook.com/profile.php?id=61592732938801'
export SOCIAL_PUBLISHING_FACEBOOK_REQUEST_TIMEOUT_SECONDS='30'
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
assert_env_line 'FACEBOOK_APP_ID=123456789012345'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_API_VERSION=v24.0'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID=1285475681307050'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN=test-page-access-token-value'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL=https://www.facebook.com/profile.php?id=61592732938801'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_REQUEST_TIMEOUT_SECONDS=30'
assert_env_line 'SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED=false'

if ! grep -Fq 'FACEBOOK_APP_ID: ${FACEBOOK_APP_ID:-}' "${deploy_scripts_dir}/../compose.prod.yml"; then
  echo 'The frontend SSR service does not receive FACEBOOK_APP_ID.' >&2
  exit 1
fi

if ! grep -Fq 'SOCIAL_PUBLISHING_FACEBOOK_ENABLED: ${SOCIAL_PUBLISHING_FACEBOOK_ENABLED:-false}' "${deploy_scripts_dir}/../compose.prod.yml"; then
  echo 'The frontend SSR service does not receive SOCIAL_PUBLISHING_FACEBOOK_ENABLED.' >&2
  exit 1
fi

"${deploy_scripts_dir}/validate-production-env.sh" "${valid_env_file}"

export SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED='true'
export SOCIAL_PUBLISHING_FACEBOOK_APP_SECRET='test-facebook-app-secret-value'
export SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_VERIFY_TOKEN='test-facebook-webhook-verify-token-value'
valid_webhook_env_file="${temp_dir}/valid-webhook.env"
"${deploy_scripts_dir}/write-production-env.sh" "${valid_webhook_env_file}"
"${deploy_scripts_dir}/validate-production-env.sh" "${valid_webhook_env_file}"

export SOCIAL_PUBLISHING_FACEBOOK_ENABLED='false'
webhook_without_publishing_env_file="${temp_dir}/webhook-without-publishing.env"
"${deploy_scripts_dir}/write-production-env.sh" "${webhook_without_publishing_env_file}"

if "${deploy_scripts_dir}/validate-production-env.sh" "${webhook_without_publishing_env_file}" >/dev/null 2>&1; then
  echo 'Validation unexpectedly accepted an enabled Facebook webhook without enabled publishing.' >&2
  exit 1
fi

export SOCIAL_PUBLISHING_FACEBOOK_ENABLED='true'
export SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED='false'
export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN=''
invalid_env_file="${temp_dir}/missing-token.env"
"${deploy_scripts_dir}/write-production-env.sh" "${invalid_env_file}"

if "${deploy_scripts_dir}/validate-production-env.sh" "${invalid_env_file}" >/dev/null 2>&1; then
  echo 'Validation unexpectedly accepted enabled Facebook publishing without a Page Access Token.' >&2
  exit 1
fi

export SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN='test-page-access-token-value'
export FACEBOOK_APP_ID=''
missing_app_id_env_file="${temp_dir}/missing-app-id.env"
"${deploy_scripts_dir}/write-production-env.sh" "${missing_app_id_env_file}"

if "${deploy_scripts_dir}/validate-production-env.sh" "${missing_app_id_env_file}" >/dev/null 2>&1; then
  echo 'Validation unexpectedly accepted enabled Facebook publishing without FACEBOOK_APP_ID.' >&2
  exit 1
fi

export SOCIAL_PUBLISHING_FACEBOOK_ENABLED='false'
publishing_disabled_without_app_id_env_file="${temp_dir}/publishing-disabled-without-app-id.env"
"${deploy_scripts_dir}/write-production-env.sh" "${publishing_disabled_without_app_id_env_file}"
"${deploy_scripts_dir}/validate-production-env.sh" "${publishing_disabled_without_app_id_env_file}"

echo 'Social publishing production environment tests passed.'
