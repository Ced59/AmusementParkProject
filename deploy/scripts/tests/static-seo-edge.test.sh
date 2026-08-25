#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_directory="$(cd "${script_dir}/../.." && pwd)"
deploy_script="${deploy_directory}/scripts/deploy.sh"
temp_dir="$(mktemp -d)"
container_name="amusementpark-static-seo-edge-test-${RANDOM}-$$"
front_container_name="${container_name}-front"
network_name="${container_name}-network"
live_static_policy="${temp_dir}/seo-static-true.conf"

sed \
  -e '/^[[:space:]]*types {$/,/^[[:space:]]*}$/d' \
  -e '/^[[:space:]]*charset utf-8;$/d' \
  -e '/^[[:space:]]*charset_types application\/xml;$/d' \
  "${deploy_directory}/nginx/seo-static-true.conf" \
  > "${live_static_policy}"

cleanup() {
  docker rm -f "${container_name}" >/dev/null 2>&1 || true
  docker rm -f "${front_container_name}" >/dev/null 2>&1 || true
  docker network rm "${network_name}" >/dev/null 2>&1 || true
  rm -rf "${temp_dir}"
}
trap cleanup EXIT

mkdir -p "${temp_dir}/seo/current"
printf '%s\n' \
  '<?xml version="1.0" encoding="utf-8"?>' \
  '<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></sitemapindex>' \
  > "${temp_dir}/seo/current/sitemap.xml"
printf '%s\n' \
  '<?xml version="1.0" encoding="utf-8"?>' \
  '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></urlset>' \
  > "${temp_dir}/seo/current/parks-fr.xml"

cat > "${temp_dir}/front.conf" <<'EOF'
worker_processes 1;
pid /var/run/nginx.pid;
events { worker_connections 128; }
http {
  server {
    listen 4000;
    location / {
      default_type text/plain;
      add_header X-AmusementPark-Test-Source "front" always;
      return 200 "front-fallback";
    }
  }
}
EOF

docker network create "${network_name}" >/dev/null
docker run -d \
  --name "${front_container_name}" \
  --network "${network_name}" \
  --network-alias front \
  --read-only \
  --tmpfs /var/cache/nginx \
  --tmpfs /var/run \
  --tmpfs /tmp \
  -v "${temp_dir}/front.conf:/etc/nginx/nginx.conf:ro" \
  nginx:1.29-alpine >/dev/null

start_edge() {
  local snapshot_policy="$1"
  local routing_configuration="${deploy_directory}/nginx/seo-static-${snapshot_policy}.conf"

  if [ "${snapshot_policy}" = 'true' ]; then
    routing_configuration="${live_static_policy}"
  fi

  docker rm -f "${container_name}" >/dev/null 2>&1 || true
  docker run -d \
    --name "${container_name}" \
    --network "${network_name}" \
    --read-only \
    --tmpfs /var/cache/nginx \
    --tmpfs /var/run \
    --tmpfs /tmp \
    -v "${deploy_directory}/nginx/edge.conf:/etc/nginx/nginx.conf:ro" \
    -v "${routing_configuration}:/etc/nginx/seo-static-routing.conf:ro" \
    -v "${temp_dir}/seo:/srv/seo:ro" \
    nginx:1.29-alpine >/dev/null
}

read_response_headers() {
  local request_path="${1:-/sitemap.xml}"
  local response_headers=""

  for _attempt in $(seq 1 20); do
    response_headers="$(docker exec "${container_name}" wget -S --spider "http://127.0.0.1:4000${request_path}" 2>&1 || true)"
    if grep -qi 'HTTP/1.1 200 OK' <<< "${response_headers}"; then
      printf '%s' "${response_headers}"
      return 0
    fi
    sleep 1
  done

  printf '%s\n' "${response_headers}" >&2
  return 1
}

start_edge true

headers="$(read_response_headers)"

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
assert_header 'Content-Type:[[:space:]]*text/xml' 'initial legacy XML content type'
assert_header 'Cache-Control:[[:space:]]*public, max-age=600' 'public cache policy'
assert_header 'X-Content-Type-Options:[[:space:]]*nosniff' 'content type protection'
assert_header 'X-Frame-Options:[[:space:]]*DENY' 'frame protection'
assert_header 'Referrer-Policy:[[:space:]]*strict-origin-when-cross-origin' 'referrer policy'
assert_header 'Permissions-Policy:[[:space:]]*camera=\(\), microphone=\(\), geolocation=\(self\)' 'permissions policy'
assert_header "Content-Security-Policy:[[:space:]]*default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'" 'content security policy'

cp "${deploy_directory}/nginx/seo-static-true.conf" "${live_static_policy}"
docker exec "${container_name}" nginx -t
docker exec "${container_name}" nginx -s reload

for _attempt in $(seq 1 20); do
  headers="$(read_response_headers)"
  if grep -Eqi 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' <<< "${headers}"; then
    break
  fi
  sleep 1
done
assert_header 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' 'reloaded XML UTF-8 content type'

headers="$(read_response_headers /parks-fr.xml)"
assert_header 'X-AmusementPark-SEO-Source:[[:space:]]*static' 'static child sitemap source header'
assert_header 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' 'child sitemap XML UTF-8 content type'

start_edge false
headers="$(read_response_headers)"
body="$(docker exec "${container_name}" wget -qO- http://127.0.0.1:4000/sitemap.xml)"

assert_header 'X-AmusementPark-Test-Source:[[:space:]]*front' 'SSR/API fallback source'
if grep -qi 'X-AmusementPark-SEO-Source:[[:space:]]*static' <<< "${headers}"; then
  echo 'A retained static sitemap must be bypassed when snapshot publishing is disabled.' >&2
  printf '%s\n' "${headers}" >&2
  exit 1
fi
if [ "${body}" != 'front-fallback' ]; then
  echo 'Snapshot-disabled routing did not return the frontend fallback response.' >&2
  printf 'Response body: %s\n' "${body}" >&2
  exit 1
fi

if ! grep -Fq 'if ! compose exec -T edge nginx -t; then' "${deploy_script}" \
  || ! grep -Fq 'if ! compose exec -T edge nginx -s reload; then' "${deploy_script}"; then
  echo 'Deployments must validate and reload the bind-mounted Nginx edge configuration.' >&2
  exit 1
fi

edge_healthy_line="$(grep -n '^wait_for_service_healthy edge 180$' "${deploy_script}" | cut -d: -f1)"
edge_reload_line="$(grep -n '^reload_edge_configuration$' "${deploy_script}" | cut -d: -f1)"
snapshot_check_line="$(grep -n '^[[:space:]]*wait_for_static_seo_snapshot 60$' "${deploy_script}" | cut -d: -f1)"
if [ -z "${edge_healthy_line}" ] \
  || [ -z "${edge_reload_line}" ] \
  || [ -z "${snapshot_check_line}" ] \
  || [ "${edge_reload_line}" -le "${edge_healthy_line}" ] \
  || [ "${edge_reload_line}" -ge "${snapshot_check_line}" ]; then
  echo 'The Nginx edge must be healthy, then reloaded before the static sitemap validation.' >&2
  exit 1
fi

if [ "$(grep -Fc "grep -Eqi '^Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8'" "${deploy_script}")" -lt 2 ]; then
  echo 'Deployment validation must enforce the XML UTF-8 content type on the sitemap index and a child sitemap.' >&2
  exit 1
fi

echo 'Static SEO edge security and disabled fallback tests passed.'
