#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ -f .env ]; then
  # shellcheck disable=SC1091
  source ./scripts/env-loader.sh
  load_env_file .env
fi

public_http_port="${PUBLIC_HTTP_PORT:-18080}"
npm_docker_network_name="${NPM_DOCKER_NETWORK_NAME:-nginx-proxy-network}"
minio_api_port="${MINIO_API_PORT:-19000}"
minio_console_port="${MINIO_CONSOLE_PORT:-19001}"
backend_private_subnet="${BACKEND_PRIVATE_SUBNET:-172.30.31.0/24}"
compose_project_name="${COMPOSE_PROJECT_NAME:-amusementpark}"

section() {
  printf '\n== %s ==\n' "$1"
}

check_command() {
  local command_name="$1"
  if command -v "${command_name}" >/dev/null 2>&1; then
    printf 'OK: %s -> %s\n' "${command_name}" "$(command -v "${command_name}")"
  else
    printf 'MISSING: %s\n' "${command_name}"
  fi
}

check_port() {
  local port="$1"
  local label="$2"

  if ss -ltn "sport = :${port}" | tail -n +2 | grep -q .; then
    printf 'IN USE: 127.0.0.1:%s / *:%s (%s)\n' "${port}" "${port}" "${label}"
    ss -ltnp "sport = :${port}" || true
  else
    printf 'FREE: %s (%s)\n' "${port}" "${label}"
  fi
}

section "System"
uname -a || true
lsb_release -a 2>/dev/null || cat /etc/os-release || true
date -u || true

section "Disk and memory"
df -h / || true
free -h || true

section "Required commands"
check_command docker
check_command curl
check_command tar
check_command ssh

section "Docker"
docker --version || true
docker compose version || true
docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}' || true

section "Ports expected by AmusementPark production stack"
check_port "${public_http_port}" "front SSR loopback direct diagnostic port"
check_port "${minio_api_port}" "MinIO API loopback / SSH tunnel"
check_port "${minio_console_port}" "MinIO console loopback / SSH tunnel"
check_port 80 "public HTTP - usually owned by existing Nginx Proxy Manager"
check_port 443 "public HTTPS - usually owned by existing Nginx Proxy Manager"
check_port 81 "Nginx Proxy Manager admin UI if default port is used"

section "Docker networks"
docker network ls || true
if docker network inspect "${npm_docker_network_name}" >/dev/null 2>&1; then
  printf 'OK: external NPM Docker network exists: %s\n' "${npm_docker_network_name}"
else
  printf 'MISSING: external NPM Docker network does not exist: %s\n' "${npm_docker_network_name}"
fi
printf '\nRequired external NPM network=%s\n' "${npm_docker_network_name}"
printf 'Requested BACKEND_PRIVATE_SUBNET=%s\n' "${backend_private_subnet}"
docker network inspect $(docker network ls -q) --format '{{.Name}} {{range .IPAM.Config}}{{.Subnet}} {{end}}' 2>/dev/null || true

section "Current compose project status"
docker compose --project-name "${compose_project_name}" -f compose.prod.yml ps 2>/dev/null || true

section "Nginx Proxy Manager hint"
echo "If NPM is already installed on this VPS, create/update a Proxy Host later:"
echo "  Domain Names: amusement-parks.fun, www.amusement-parks.fun"
echo "  Scheme: http"
echo "  Forward Hostname / IP: 127.0.0.1"
echo "  Forward Port: ${public_http_port}"
echo "  SSL: enabled, Force SSL enabled, HTTP/2 enabled"

section "Done"
echo "This script is read-only. It did not change the VPS."
