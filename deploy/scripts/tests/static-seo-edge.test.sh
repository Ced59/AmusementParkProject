#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_directory="$(cd "${script_dir}/../.." && pwd)"
deploy_script="${deploy_directory}/scripts/deploy.sh"
compose_file="${deploy_directory}/compose.prod.yml"
temp_dir="$(mktemp -d)"
container_name="amusementpark-static-seo-edge-test-${RANDOM}-$$"
front_container_name="${container_name}-front"
network_name="${container_name}-network"
live_nginx_directory="${temp_dir}/nginx"
live_static_policy="${live_nginx_directory}/seo-static-routing.conf"

mkdir -p "${live_nginx_directory}"
cp "${deploy_directory}/nginx/edge.conf" "${live_nginx_directory}/edge.conf"

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
printf '%s\n' \
  'User-agent: *' \
  'Disallow:' \
  > "${temp_dir}/seo/current/robots.txt"

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
  docker rm -f "${container_name}" >/dev/null 2>&1 || true
  docker run -d \
    --name "${container_name}" \
    --network "${network_name}" \
    --read-only \
    --tmpfs /var/cache/nginx \
    --tmpfs /var/run \
    --tmpfs /tmp \
    -v "${live_nginx_directory}:/etc/nginx/amusementpark:ro" \
    -v "${temp_dir}/seo:/srv/seo:ro" \
    nginx:1.29-alpine \
    /bin/sh -ec \
    "ln -sf /etc/nginx/amusementpark/seo-static-routing.conf /tmp/seo-static-routing.conf && exec nginx -c /etc/nginx/amusementpark/edge.conf -g 'daemon off;'" \
    >/dev/null
}

read_response() {
  local request_path="${1:-/sitemap.xml}"
  local response=""

  for _attempt in $(seq 1 20); do
    response="$(docker exec "${container_name}" /bin/sh -ec '
      request_path="$1"
      headers_file=/tmp/static-seo-test-headers
      body_file=/tmp/static-seo-test-body

      if ! wget -S -O "${body_file}" "http://127.0.0.1:4000${request_path}" 2> "${headers_file}"; then
        cat "${headers_file}" >&2
        exit 1
      fi

      cat "${headers_file}"
      printf "\n---BODY---\n"
      cat "${body_file}"
    ' sh "${request_path}" 2>&1 || true)"
    if grep -qi 'HTTP/1.1 200 OK' <<< "${response}"; then
      printf '%s' "${response}"
      return 0
    fi
    sleep 1
  done

  printf '%s\n' "${response}" >&2
  return 1
}

start_edge true

headers="$(read_response)"

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
assert_header "Content-Security-Policy:[[:space:]]*default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'" 'XML viewer-compatible content security policy'

headers="$(read_response /robots.txt)"
assert_header "Content-Security-Policy:[[:space:]]*default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'" 'strict non-XML content security policy'
if grep -qi "style-src 'unsafe-inline'" <<< "${headers}"; then
  echo 'Inline styles must only be permitted on XML responses rendered by the browser XML viewer.' >&2
  printf '%s\n' "${headers}" >&2
  exit 1
fi

cp "${deploy_directory}/nginx/seo-static-true.conf" "${live_static_policy}.next"
mv -f "${live_static_policy}.next" "${live_static_policy}"
docker exec "${container_name}" nginx -t -c /etc/nginx/amusementpark/edge.conf
docker exec "${container_name}" nginx -s reload -c /etc/nginx/amusementpark/edge.conf

for _attempt in $(seq 1 20); do
  headers="$(read_response)"
  if grep -Eqi 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' <<< "${headers}"; then
    break
  fi
  sleep 1
done
assert_header 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' 'atomically refreshed XML UTF-8 content type'

for _attempt in $(seq 1 20); do
  headers="$(read_response /parks-fr.xml)"
  if grep -Eqi 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' <<< "${headers}"; then
    break
  fi
  sleep 1
done
assert_header 'X-AmusementPark-SEO-Source:[[:space:]]*static' 'static child sitemap source header'
assert_header 'Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8' 'child sitemap XML UTF-8 content type'
assert_header "Content-Security-Policy:[[:space:]]*default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'" 'child sitemap XML viewer-compatible content security policy'

cp "${deploy_directory}/nginx/seo-static-false.conf" "${live_static_policy}.next"
mv -f "${live_static_policy}.next" "${live_static_policy}"
docker exec "${container_name}" nginx -t -c /etc/nginx/amusementpark/edge.conf
docker exec "${container_name}" nginx -s reload -c /etc/nginx/amusementpark/edge.conf

for _attempt in $(seq 1 20); do
  headers="$(read_response)"
  body="${headers#*$'\n---BODY---\n'}"
  if grep -qi 'X-AmusementPark-Test-Source:[[:space:]]*front' <<< "${headers}" \
    && [ "${body}" = 'front-fallback' ]; then
    break
  fi
  sleep 1
done

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

if ! grep -Fq './nginx:/etc/nginx/amusementpark:ro' "${compose_file}"; then
  echo 'The complete Nginx configuration directory must be bind-mounted so atomic file replacements remain visible.' >&2
  exit 1
fi

if grep -Fq 'force-recreate edge' "${deploy_script}"; then
  echo 'Routine deployments must not interrupt the sole Nginx edge service.' >&2
  exit 1
fi

if ! grep -Fq 'if ! compose exec -T edge nginx -t -c /etc/nginx/amusementpark/edge.conf; then' "${deploy_script}" \
  || ! grep -Fq 'if ! compose exec -T edge nginx -s reload -c /etc/nginx/amusementpark/edge.conf; then' "${deploy_script}"; then
  echo 'Deployments must validate and gracefully reload the directory-mounted Nginx edge configuration.' >&2
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
  echo 'The Nginx edge must be healthy, reload gracefully, then pass the static sitemap validation.' >&2
  exit 1
fi

if [ "$(grep -Fc "grep -Eqi '^Content-Type:[[:space:]]*application/xml;[[:space:]]*charset=utf-8'" "${deploy_script}")" -lt 2 ]; then
  echo 'Deployment validation must enforce the XML UTF-8 content type on the sitemap index and a child sitemap.' >&2
  exit 1
fi

echo 'Static SEO edge security and disabled fallback tests passed.'
