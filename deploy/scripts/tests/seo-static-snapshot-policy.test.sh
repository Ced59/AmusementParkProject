#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${script_dir}/../seo-static-snapshot-policy.sh"

unset SSR_SEO_STATIC_SNAPSHOT_ENABLED
if ! is_static_seo_snapshot_enabled; then
  echo 'Static SEO snapshot publishing must be enabled by default.' >&2
  exit 1
fi

SSR_SEO_STATIC_SNAPSHOT_ENABLED='true'
if ! is_static_seo_snapshot_enabled; then
  echo 'Static SEO snapshot publishing must be enabled for an explicit true value.' >&2
  exit 1
fi

SSR_SEO_STATIC_SNAPSHOT_ENABLED='false'
if is_static_seo_snapshot_enabled; then
  echo 'Static SEO snapshot deployment checks must be skipped for an explicit false value.' >&2
  exit 1
fi

export ALLOWED_HOSTS='test'
export API_IMAGE='test'
export FORWARDED_HEADERS_ALLOWED_HOSTS='test'
export FORWARDED_HEADERS_KNOWN_NETWORKS='test'
export FRONT_IMAGE='test'
export JWT_AUDIENCE='test'
export JWT_ISSUER='test'
export JWT_KEY='test'
export MINIO_ROOT_PASSWORD='test'
export MINIO_ROOT_USER='test'
export MONGO_APP_PASSWORD='test'
export MONGO_APP_PASSWORD_URL_ENCODED='test'
export MONGO_APP_USERNAME='test'
export MONGO_APP_USERNAME_URL_ENCODED='test'
export MONGO_INITDB_ROOT_PASSWORD='test'
export MONGO_INITDB_ROOT_USERNAME='test'
export PUBLIC_BASE_URL='https://amusement-parks.fun'
export SSR_ALLOWED_HOSTS='test'
export SSR_CACHE_INVALIDATION_TOKEN='test'

compose_file="${script_dir}/../../compose.prod.yml"
for expected_policy in true false; do
  export SSR_SEO_STATIC_SNAPSHOT_ENABLED="${expected_policy}"
  compose_config="$(docker compose -f "${compose_file}" config)"
  if ! grep -q "seo-static-${expected_policy}\.conf" <<< "${compose_config}"; then
    echo "Docker Compose must select the ${expected_policy} static SEO routing policy." >&2
    exit 1
  fi
done

echo 'Static SEO snapshot deployment policy tests passed.'
