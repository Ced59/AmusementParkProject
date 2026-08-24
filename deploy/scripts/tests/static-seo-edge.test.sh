#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_directory="$(cd "${script_dir}/../.." && pwd)"
temp_dir="$(mktemp -d)"
container_name="amusementpark-static-seo-edge-test-${RANDOM}-$$"

cleanup() {
  docker rm -f "${container_name}" >/dev/null 2>&1 || true
  rm -rf "${temp_dir}"
}
trap cleanup EXIT

mkdir -p "${temp_dir}/seo/current"
printf '%s\n' \
  '<?xml version="1.0" encoding="utf-8"?>' \
  '<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></sitemapindex>' \
  > "${temp_dir}/seo/current/sitemap.xml"

docker run -d \
  --name "${container_name}" \
  --read-only \
  --tmpfs /var/cache/nginx \
  --tmpfs /var/run \
  --tmpfs /tmp \
  -v "${deploy_directory}/nginx/edge.conf:/etc/nginx/nginx.conf:ro" \
  -v "${temp_dir}/seo:/srv/seo:ro" \
  nginx:1.29-alpine >/dev/null

headers=""
for _attempt in $(seq 1 20); do
  headers="$(docker exec "${container_name}" wget -S --spider http://127.0.0.1:4000/sitemap.xml 2>&1 || true)"
  if grep -qi 'HTTP/1.1 200 OK' <<< "${headers}"; then
    break
  fi
  sleep 1
done

assert_header() {
  local expected_pattern="$1"
  local description="$2"

  if ! grep -Eqi "${expected_pattern}" <<< "${headers}"; then
    echo "Missing ${description} on the static sitemap response." >&2
    printf '%s\n' "${headers}" >&2
    exit 1
  fi
}

assert_header 'X-AmusementPark-SEO-Source:[[:space:]]*static' 'static source header'
assert_header 'Cache-Control:[[:space:]]*public, max-age=600' 'public cache policy'
assert_header 'X-Content-Type-Options:[[:space:]]*nosniff' 'content type protection'
assert_header 'X-Frame-Options:[[:space:]]*DENY' 'frame protection'
assert_header 'Referrer-Policy:[[:space:]]*strict-origin-when-cross-origin' 'referrer policy'
assert_header 'Permissions-Policy:[[:space:]]*camera=\(\), microphone=\(\), geolocation=\(self\)' 'permissions policy'
assert_header "Content-Security-Policy:[[:space:]]*default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'" 'content security policy'

echo 'Static SEO edge security header tests passed.'
