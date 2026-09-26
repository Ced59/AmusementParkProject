#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_script="$(cd "${script_dir}/.." && pwd)/deploy.sh"
deploy_root="$(cd "${script_dir}/../.." && pwd)"

if ! grep -Fq 'prepare_historical_history_cutover' "${deploy_script}"; then
  echo 'The deployment must prepare the historical cutover before starting candidates.' >&2
  exit 1
fi

if ! grep -Fq 'rollback_incomplete_historical_history_cutover' "${deploy_script}"; then
  echo 'The deployment must restore legacy historical authority when cutover is interrupted.' >&2
  exit 1
fi

rollback_arm_line="$(grep -n 'historical_history_cutover_started=true' "${deploy_script}" | head -n 1 | cut -d: -f1)"
freeze_command_line="$(grep -n 'freeze-legacy-history-5.3.82.js' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${rollback_arm_line}" ] \
  || [ -z "${freeze_command_line}" ] \
  || [ "${rollback_arm_line}" -ge "${freeze_command_line}" ]; then
  echo 'Historical rollback must be armed before attempting the legacy collection freeze.' >&2
  exit 1
fi

if ! grep -Fq 'arm-cutover --resource historical-history' "${deploy_script}"; then
  echo 'The historical cutover must have an independently tracked transaction resource.' >&2
  exit 1
fi

resume_recovery_line="$(grep -n 'cutover-pending --resource historical-history' "${deploy_script}" | head -n 1 | cut -d: -f1)"
migration_check_line="$(grep -n 'getCollection("historical-migrations")' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${resume_recovery_line}" ] \
  || [ -z "${migration_check_line}" ] \
  || [ "${resume_recovery_line}" -ge "${migration_check_line}" ]; then
  echo 'A resumed historical cutover must restore its rollback flag before accepting a completed migration.' >&2
  exit 1
fi

freeze_script="${deploy_root}/scripts/freeze-legacy-history-5.3.82.js"
for required_freeze_step in \
  "renameCollection(frozenCollectionName, false)" \
  "createView(legacyCollectionName, frozenCollectionName, [])" \
  "legacyInfo[0].type === 'view'"; do
  if ! grep -Fq "${required_freeze_step}" "${freeze_script}"; then
    echo "The historical cutover is missing its read-only view step: ${required_freeze_step}" >&2
    exit 1
  fi
done

rollback_script="${deploy_root}/scripts/rollback-history-5.3.82.js"
for required_filter in \
  "migrationVersion: migrationId" \
  "publicationMethodologyVersion: methodologyVersion" \
  "'transitionReviewEvent.actorUserId': migrationActor" \
  "getCollection(legacyCollectionName).drop()" \
  "renameCollection(legacyCollectionName, false)"; do
  if ! grep -Fq "${required_filter}" "${rollback_script}"; then
    echo "Historical rollback is missing its targeted filter: ${required_filter}" >&2
    exit 1
  fi
done

echo 'Historical history cutover deployment tests passed.'
