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

backup_dir="${BACKUP_DIR:-./backups/mongodb}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "${backup_dir}"

docker compose -f compose.prod.yml exec -T mongodb \
  mongodump \
  --username "${MONGO_INITDB_ROOT_USERNAME}" \
  --password "${MONGO_INITDB_ROOT_PASSWORD}" \
  --authenticationDatabase admin \
  --db "${MONGO_DATABASE_NAME:-AmusementPark}" \
  --archive \
  --gzip > "${backup_dir}/amusementpark-mongodb-${timestamp}.archive.gz"

echo "MongoDB backup written to ${backup_dir}/amusementpark-mongodb-${timestamp}.archive.gz"
