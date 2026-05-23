#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ ! -f .env ]; then
  echo "Missing .env file in $(pwd)." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1091
source .env
set +a

docker compose -f compose.prod.yml pull
docker compose -f compose.prod.yml up -d --remove-orphans

docker compose -f compose.prod.yml ps

echo "Checking public frontend on 127.0.0.1:${PUBLIC_HTTP_PORT:-8080}..."
curl -fsS "http://127.0.0.1:${PUBLIC_HTTP_PORT:-8080}/" >/dev/null

echo "Checking API health through the frontend reverse proxy..."
curl -fsS "http://127.0.0.1:${PUBLIC_HTTP_PORT:-8080}/api/health" >/dev/null

echo "Pruning old Docker images older than 7 days..."
docker image prune -af --filter "until=168h" >/dev/null || true

echo "Deployment completed."
