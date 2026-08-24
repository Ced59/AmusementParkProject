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

echo 'Static SEO snapshot deployment policy tests passed.'
