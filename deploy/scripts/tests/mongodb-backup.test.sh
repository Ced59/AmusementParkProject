#!/usr/bin/env bash
set -euo pipefail

scripts="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
temporary="$(mktemp -d)"
trap 'rm -rf -- "${temporary}"' EXIT
mkdir "${temporary}/bin"
export PATH="${temporary}/bin:${PATH}"
cat > "${temporary}/bin/docker" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
echo dump >> "${TEST_DUMP_LOG}"
case "${TEST_DUMP_BEHAVIOR:-valid}" in
  fail) printf 'partial'; exit 1 ;;
  corrupt) printf 'not gzip' ;;
  valid) printf 'test mongo archive' | gzip -c ;;
esac
EOF
cat > "${temporary}/bin/systemctl" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$*" >> "${TEST_SYSTEMCTL_LOG}"
EOF
cat > "${temporary}/bin/install" <<'EOF'
#!/usr/bin/env bash
cp -- "$3" "${TEST_UNIT_DIR}/$(basename "$4")"
EOF
cat > "${temporary}/bin/id" <<'EOF'
#!/usr/bin/env bash
echo 0
EOF
chmod +x "${temporary}/bin/"*

fail() { echo "FAILED: $*" >&2; exit 1; }
new_case() {
  directory="${temporary}/$1 with spaces"
  mkdir -p "${directory}/scripts" "${directory}/backups/mongodb" "${directory}/units"
  cp "${scripts}/backup-mongo.sh" "${scripts}/configure-backup-timer.sh" "${scripts}/env-loader.sh" "${directory}/scripts/"
  cat > "${directory}/.env" <<'EOF'
MONGO_INITDB_ROOT_USERNAME=test-root
MONGO_INITDB_ROOT_PASSWORD=test-password
EOF
  export TEST_DUMP_LOG="${directory}/dumps.log" TEST_DUMP_BEHAVIOR=valid
  export DEPLOY_LOCK_FILE="${directory}/deploy.lock" DEPLOY_LOCK_TIMEOUT_SECONDS=0
  export TEST_SYSTEMCTL_LOG="${directory}/systemctl.log" TEST_UNIT_DIR="${directory}/units"
  backups="${directory}/backups/mongodb"
  : > "${TEST_DUMP_LOG}"
}
archive() {
  printf 'previous mongo archive' | gzip -c > "${backups}/amusementpark-mongodb-2026010${1}T000000Z.archive.gz"
}
count() { find "${backups}" -maxdepth 1 -type f -name '*.archive.gz' | wc -l; }
run_backup() { bash "${directory}/scripts/backup-mongo.sh" "$@" > "${directory}/output" 2>&1; }

new_case initial
run_backup
[ "$(count)" -eq 1 ] || fail 'first backup not created'
created="$(find "${backups}" -name '*.archive.gz')"
gzip -t "${created}"
[ "$(stat -c %a "${created}")" = 600 ] || fail 'backup permissions'
run_backup
[ "$(wc -l < "${TEST_DUMP_LOG}")" -eq 1 ] || fail 'recent backup not reused'
grep -q 'Recent valid' "${directory}/output" || fail 'reuse missing'

for mode in --force maintenance; do
  new_case "forced-${mode}"
  archive 1
  if [ "${mode}" = maintenance ]; then
    echo SHARED_INFRASTRUCTURE_MAINTENANCE=mongodb >> "${directory}/.env"
    run_backup
  else
    run_backup --force
  fi
  [ "$(wc -l < "${TEST_DUMP_LOG}")" -eq 1 ] || fail 'fresh maintenance/forced backup skipped'
done

new_case count
for day in 1 2 3 4 5; do archive "${day}"; done
printf 'BACKUP_RETENTION_COUNT=3\nBACKUP_MIN_RETAINED=2\n' >> "${directory}/.env"
printf 'unrelated' > "${backups}/personal-backup.tar.gz"
ln -s "${backups}/personal-backup.tar.gz" "${backups}/amusementpark-mongodb-20250101T000000Z.archive.gz"
run_backup
[ "$(count)" -eq 3 ] || fail 'count retention'
[ -f "${backups}/amusementpark-mongodb-20260103T000000Z.archive.gz" ] || fail 'wrong archives retained'
[ -L "${backups}/amusementpark-mongodb-20250101T000000Z.archive.gz" ] || fail 'symlink touched'
[ -f "${backups}/personal-backup.tar.gz" ] || fail 'unrelated file touched'

for minimum in 2 3; do
  new_case "size-${minimum}"
  for day in 1 2 3; do archive "${day}"; truncate -s 716800 "${backups}/amusementpark-mongodb-2026010${day}T000000Z.archive.gz"; done
  archive 4
  printf 'BACKUP_MAX_TOTAL_MIB=1\nBACKUP_MIN_RETAINED=%s\n' "${minimum}" >> "${directory}/.env"
  run_backup
  [ "$(count)" -eq "${minimum}" ] || fail 'size retention/minimum protection'
  if [ "${minimum}" -eq 3 ]; then grep -q 'WARNING: size budget exceeded' "${directory}/output" || fail 'missing budget warning'; fi
done

for behavior in fail corrupt; do
  new_case "failed-${behavior}"
  for day in 1 2 3 4 5; do archive "${day}"; done
  printf 'BACKUP_RETENTION_COUNT=3\n' >> "${directory}/.env"
  export TEST_DUMP_BEHAVIOR="${behavior}"
  if run_backup --force; then fail 'bad dump accepted'; fi
  [ "$(count)" -eq 5 ] || fail 'old backups pruned after failure'
  [ -z "$(find "${backups}" -name '*.partial')" ] || fail 'partial backup left behind'
done

new_case legacy_corrupt
archive 1
printf 'truncated' > "${backups}/amusementpark-mongodb-20260102T000000Z.archive.gz"
run_backup
[ ! -s "${TEST_DUMP_LOG}" ] || fail 'valid previous backup not reused'
grep -q 'Ignoring incomplete' "${directory}/output" || fail 'corrupt legacy archive reused'

new_case corrupt_newer_retention
archive 1
for day in 2 3 4; do printf 'truncated' > "${backups}/amusementpark-mongodb-2026010${day}T000000Z.archive.gz"; done
echo BACKUP_RETENTION_COUNT=3 >> "${directory}/.env"
run_backup
[ -f "${backups}/amusementpark-mongodb-20260101T000000Z.archive.gz" ] || fail 'only verified restore point was deleted'
[ "$(count)" -eq 1 ] || fail 'known-invalid archives counted as protected backups'
gzip -t "${backups}/amusementpark-mongodb-20260101T000000Z.archive.gz"

new_case no_valid_backup_failed_dump
for day in 1 2 3 4; do printf 'truncated' > "${backups}/amusementpark-mongodb-2026010${day}T000000Z.archive.gz"; done
echo BACKUP_RETENTION_COUNT=3 >> "${directory}/.env"
export TEST_DUMP_BEHAVIOR=fail
if run_backup; then fail 'failed dump without a valid backup accepted'; fi
[ "$(count)" -eq 4 ] || fail 'known-invalid legacy files pruned before a successful backup'

new_case changed_cached_archive
archive 1
run_backup
printf 'truncated' > "${backups}/amusementpark-mongodb-20260101T000000Z.archive.gz"
run_backup
[ "$(wc -l < "${TEST_DUMP_LOG}")" -eq 1 ] || fail 'changed cached archive trusted'

new_case expired
archive 1
touch -d '25 hours ago' "${backups}/amusementpark-mongodb-20260101T000000Z.archive.gz"
run_backup
[ "$(wc -l < "${TEST_DUMP_LOG}")" -eq 1 ] || fail 'expired backup reused'

new_case locked
if flock "${DEPLOY_LOCK_FILE}" bash -c 'exec 9>&-; bash "$1"' _ "${directory}/scripts/backup-mongo.sh" > "${directory}/output" 2>&1; then fail 'concurrent deployment lock ignored'; fi
[ ! -s "${TEST_DUMP_LOG}" ] || fail 'dump started under another lock'
# An actual pre-deployment child inherits descriptor 9 and must not deadlock.
(
  exec 9>"${DEPLOY_LOCK_FILE}"
  flock 9
  run_backup
)
[ "$(wc -l < "${TEST_DUMP_LOG}")" -eq 1 ] || fail 'inherited deployment lock rejected'

new_case invalid
printf 'BACKUP_MIN_RETAINED=0\n' >> "${directory}/.env"
if run_backup; then fail 'invalid retention accepted'; fi
[ ! -s "${TEST_DUMP_LOG}" ] || fail 'dump started with invalid configuration'

new_case pending
touch "${directory}/.installation-pending.json"
if run_backup; then fail 'incomplete installation ignored'; fi
[ ! -s "${TEST_DUMP_LOG}" ] || fail 'dump started during incomplete installation'

new_case timer
bash "${directory}/scripts/configure-backup-timer.sh" > "${directory}/output"
grep -qx 'OnCalendar=hourly' "${TEST_UNIT_DIR}/amusementpark-mongodb-backup.timer" || fail 'timer cadence'
grep -qx 'Persistent=true' "${TEST_UNIT_DIR}/amusementpark-mongodb-backup.timer" || fail 'missed timers not recovered'
grep -Fqx "ExecStart=/bin/bash \"${directory}/scripts/backup-mongo.sh\"" "${TEST_UNIT_DIR}/amusementpark-mongodb-backup.service" || fail 'scheduled entrypoint'
grep -qx 'enable --now amusementpark-mongodb-backup.timer' "${TEST_SYSTEMCTL_LOG}" || fail 'timer not activated'
echo BACKUP_SCHEDULE_ENABLED=false >> "${directory}/.env"
bash "${directory}/scripts/configure-backup-timer.sh"
grep -qx 'disable --now amusementpark-mongodb-backup.timer' "${TEST_SYSTEMCTL_LOG}" || fail 'timer not disabled'

echo 'MongoDB backup interval, integrity, failure safety, retention, locking and timer tests passed.'
