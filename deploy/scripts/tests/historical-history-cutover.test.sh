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

if ! grep -Fq "validator: { \$expr: { \$eq: [1, 0] } }" \
  "${deploy_root}/scripts/freeze-legacy-history-5.3.82.js"; then
  echo 'The legacy historical collection freeze must reject writes.' >&2
  exit 1
fi

rollback_script="${deploy_root}/scripts/rollback-history-5.3.82.js"
for required_filter in \
  "migrationVersion: migrationId" \
  "publicationMethodologyVersion: methodologyVersion" \
  "'transitionReviewEvent.actorUserId': migrationActor" \
  "validator: {}"; do
  if ! grep -Fq "${required_filter}" "${rollback_script}"; then
    echo "Historical rollback is missing its targeted filter: ${required_filter}" >&2
    exit 1
  fi
done

echo 'Historical history cutover deployment tests passed.'
