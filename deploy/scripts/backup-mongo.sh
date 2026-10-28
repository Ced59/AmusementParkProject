#!/usr/bin/env bash
set -euo pipefail
umask 077

force=false
case "${1:-}" in
  '') ;;
  --force) force=true ;;
  *) echo 'Usage: backup-mongo.sh [--force]' >&2; exit 1 ;;
esac
[ "$#" -le 1 ] || { echo 'Too many arguments.' >&2; exit 1; }

cd "$(dirname "${BASH_SOURCE[0]}")/.."

# Share the installer/deployment lock, including the descriptor inherited by
# pre-deployment backups. The scheduled job must not read a half-installed .env.
deploy_lock_file="${DEPLOY_LOCK_FILE:-/tmp/amusementpark-deploy.lock}"
if [ -e /proc/self/fd/9 ] && [ /proc/self/fd/9 -ef "${deploy_lock_file}" ]; then
  flock -n 9 || { echo 'Invalid inherited deployment lock.' >&2; exit 1; }
else
  exec 9>"${deploy_lock_file}"
  flock -w "${DEPLOY_LOCK_TIMEOUT_SECONDS:-900}" 9 || { echo 'Backup lock timeout.' >&2; exit 1; }
fi
[ ! -e .installation-pending.json ] || { echo 'Deployment installation is incomplete.' >&2; exit 1; }

if [ ! -f .env ]; then
  echo "Missing .env file in $(pwd)." >&2
  exit 1
fi

# shellcheck disable=SC1091
source ./scripts/env-loader.sh
load_env_file .env

compose_project_name="${COMPOSE_PROJECT_NAME:-amusementpark}"
backup_dir="${BACKUP_DIR:-./backups/mongodb}"
interval_hours="${BACKUP_INTERVAL_HOURS:-24}"
retention_count="${BACKUP_RETENTION_COUNT:-14}"
max_total_mib="${BACKUP_MAX_TOTAL_MIB:-8192}"
min_retained="${BACKUP_MIN_RETAINED:-3}"
for setting in interval_hours retention_count max_total_mib min_retained; do
  [[ "${!setting}" =~ ^(0|[1-9][0-9]{0,5})$ ]] || { echo "Invalid backup setting: ${setting}." >&2; exit 1; }
done
if (( min_retained < 1 || retention_count < min_retained || max_total_mib < 1 )); then
  echo 'Backup limits require count >= minimum >= 1 and a positive size budget.' >&2
  exit 1
fi
if [ "${SHARED_INFRASTRUCTURE_MAINTENANCE:-none}" = mongodb ]; then
  force=true
fi

mkdir -p -- "${backup_dir}"
backup_dir="$(realpath -e "${backup_dir}")"
marker="${backup_dir}/.last-success"
partial=''
marker_temporary=''
trap 'if [ -n "${partial}" ]; then rm -f -- "${partial}"; fi; if [ -n "${marker_temporary}" ]; then rm -f -- "${marker_temporary}"; fi' EXIT

snapshot() {
  [ -f "$1" ] && [ ! -L "$1" ] || return 1
  stat -c '%n|%s|%y|%z' -- "$1"
}

record_success() {
  marker_temporary="$(mktemp "${backup_dir}/.last-success.XXXXXX")"
  snapshot "$1" > "${marker_temporary}"
  mv -fT -- "${marker_temporary}" "${marker}"
  marker_temporary=''
}

list_archives() {
  # Exact managed names only: never include partial dumps, symlinks or other backups.
  find "${backup_dir}" -maxdepth 1 -regextype posix-extended -type f \
    -regex '.*/amusementpark-mongodb-[0-9]{8}T[0-9]{6}Z\.archive\.gz' -printf '%f\n' | LC_ALL=C sort
}

prune_archives() {
  local total=0 remaining name size before index
  local -a names sizes snapshots
  mapfile -t names < <(list_archives)
  remaining="${#names[@]}"
  for name in "${names[@]}"; do
    snapshots+=("$(snapshot "${backup_dir}/${name}")")
    size="$(stat -c %s -- "${backup_dir}/${name}")"
    sizes+=("${size}")
    total=$((total + size))
  done
  for ((index=0; index<${#names[@]}; index++)); do
    if (( remaining <= min_retained || (remaining <= retention_count && total <= max_total_mib * 1048576) )); then break; fi
    name="${names[index]}"
    before="$(snapshot "${backup_dir}/${name}")"
    [ "${before}" = "${snapshots[index]}" ] || { echo 'Backup changed during retention.' >&2; return 1; }
    rm -- "${backup_dir}/${name}"
    total=$((total - sizes[index]))
    remaining=$((remaining - 1))
    echo "Removed old MongoDB backup: ${name}"
  done
  echo "MongoDB retention: ${remaining} archives, ${total} bytes."
  if (( total > max_total_mib * 1048576 )); then
    echo "WARNING: size budget exceeded; the last ${min_retained} backups remain protected." >&2
  fi
}

mapfile -t archives < <(list_archives)
if [ "${force}" != true ]; then
  now="$(date +%s)"
  for ((index=${#archives[@]}-1; index>=0; index--)); do
    archive="${backup_dir}/${archives[index]}"
    age=$((now - $(stat -c %Y -- "${archive}")))
    if (( age < 0 || age >= interval_hours * 3600 )); then continue; fi
    before="$(snapshot "${archive}")"
    if { [ -f "${marker}" ] && [ ! -L "${marker}" ] && [ "$(cat -- "${marker}")" = "${before}" ]; } \
      || gzip -t -- "${archive}"; then
      [ "${before}" = "$(snapshot "${archive}")" ] || { echo 'Backup changed during verification.' >&2; exit 1; }
      record_success "${archive}"
      echo "Recent valid MongoDB backup reused: ${archives[index]} (minimum interval: ${interval_hours} hours)."
      prune_archives
      exit 0
    fi
    echo "Ignoring incomplete MongoDB backup: ${archives[index]}" >&2
  done
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
destination="${backup_dir}/amusementpark-mongodb-${timestamp}.archive.gz"
[ ! -e "${destination}" ] && [ ! -L "${destination}" ] || { echo 'Backup filename already exists.' >&2; exit 1; }
partial="$(mktemp "${backup_dir}/.amusementpark-mongodb-${timestamp}.XXXXXX.partial")"

docker compose --project-name "${compose_project_name}" -f compose.prod.yml exec -T mongodb \
  mongodump \
  --username "${MONGO_INITDB_ROOT_USERNAME}" \
  --password "${MONGO_INITDB_ROOT_PASSWORD}" \
  --authenticationDatabase admin \
  --db "${MONGO_DATABASE_NAME:-AmusementPark}" \
  --archive \
  --gzip > "${partial}"

# Failed or truncated dumps never become restorable archives and never trigger a purge.
gzip -t -- "${partial}"
mv -T -- "${partial}" "${destination}"
partial=''
record_success "${destination}"

echo "MongoDB backup written to ${destination}"
prune_archives
