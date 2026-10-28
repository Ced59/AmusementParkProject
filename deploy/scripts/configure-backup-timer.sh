#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
deploy_lock_file="${DEPLOY_LOCK_FILE:-/tmp/amusementpark-deploy.lock}"
if [ -e /proc/self/fd/9 ] && [ /proc/self/fd/9 -ef "${deploy_lock_file}" ]; then
  flock -n 9
else
  exec 9>"${deploy_lock_file}"
  flock -w "${DEPLOY_LOCK_TIMEOUT_SECONDS:-900}" 9
fi
[ ! -e .installation-pending.json ] || { echo 'Deployment installation is incomplete.' >&2; exit 1; }
source ./scripts/env-loader.sh
load_env_file .env

timer=amusementpark-mongodb-backup.timer
service=amusementpark-mongodb-backup.service
case "${BACKUP_SCHEDULE_ENABLED:-true}" in
  false)
    systemctl disable --now "${timer}" >/dev/null 2>&1 || true
    exit 0
    ;;
  true) ;;
  *) echo 'BACKUP_SCHEDULE_ENABLED must be true or false.' >&2; exit 1 ;;
esac
[ "$(id -u)" -eq 0 ] || { echo 'Scheduled MongoDB backups require root.' >&2; exit 1; }
directory="$(pwd -P)"
[[ "${directory}" != *'%'* && "${directory}" != *'"'* && "${directory}" != *'\'* ]] \
  || { echo 'Unsupported systemd deployment path.' >&2; exit 1; }
temporary="$(mktemp -d)"
trap 'rm -rf -- "${temporary}"' EXIT
cat > "${temporary}/${service}" <<EOF
[Unit]
Description=AmusementPark verified MongoDB backup and retention
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
WorkingDirectory="${directory}"
ExecStart=/bin/bash "${directory}/scripts/backup-mongo.sh"
TimeoutStartSec=30min
UMask=0077
Nice=10
IOSchedulingClass=best-effort
IOSchedulingPriority=7
EOF
cat > "${temporary}/${timer}" <<EOF
[Unit]
Description=Check whether the daily AmusementPark MongoDB backup is due

[Timer]
OnCalendar=hourly
RandomizedDelaySec=5min
Persistent=true
Unit=${service}

[Install]
WantedBy=timers.target
EOF
install -m 0644 "${temporary}/${service}" "/etc/systemd/system/${service}"
install -m 0644 "${temporary}/${timer}" "/etc/systemd/system/${timer}"
systemctl daemon-reload
systemctl enable --now "${timer}"
echo 'MongoDB backup timer enabled; interval and retention are read from .env.'
