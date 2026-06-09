#!/usr/bin/env bash
set -euo pipefail

container_name="${SSR_CACHE_CONTAINER:-amusementpark-front}"

docker exec "${container_name}" sh -lc '
CACHE_DIR="${SSR_DISK_PAGE_CACHE_DIR:-/var/cache/amusementpark-ssr}"
MAX_BYTES="${SSR_DISK_PAGE_CACHE_MAX_BYTES:-0}"

echo "Cache dir: ${CACHE_DIR}"
echo "Max bytes: ${MAX_BYTES}"

if [ ! -d "${CACHE_DIR}" ]; then
  echo "Cache directory does not exist yet."
  exit 0
fi

echo
echo "=== Files ==="
find "${CACHE_DIR}" -type f -name "*.json" | wc -l

echo
echo "=== Size ==="
du -sh "${CACHE_DIR}"

echo
echo "=== Latest files ==="
ls -lth "${CACHE_DIR}" | head -20
'
