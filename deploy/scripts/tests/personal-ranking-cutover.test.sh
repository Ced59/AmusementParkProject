#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_script="$(cd "${script_dir}/.." && pwd)/deploy.sh"
deploy_root="$(cd "${script_dir}/../.." && pwd)"

if ! grep -Fq 'prepare_personal_ranking_cutover' "${deploy_script}"; then
  echo 'The deployment must prepare the personal ranking cutover before starting candidates.' >&2
  exit 1
fi

if ! grep -Fq 'rollback_incomplete_personal_ranking_cutover' "${deploy_script}"; then
  echo 'The deployment must restore the legacy authority when the cutover is interrupted.' >&2
  exit 1
fi

rollback_arm_line="$(grep -n 'personal_ranking_cutover_started=true' "${deploy_script}" | head -n 1 | cut -d: -f1)"
freeze_command_line="$(grep -n 'freeze-legacy-ranking-shares-5.2.6.js' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${rollback_arm_line}" ] \
  || [ -z "${freeze_command_line}" ] \
  || [ "${rollback_arm_line}" -ge "${freeze_command_line}" ]; then
  echo 'Rollback must be armed before attempting the legacy collection freeze.' >&2
  exit 1
fi

if ! grep -Fq "validator: { \$expr: { \$eq: [1, 0] } }" \
  "${deploy_root}/scripts/freeze-legacy-ranking-shares-5.2.6.js"; then
  echo 'The legacy collection freeze must reject writes.' >&2
  exit 1
fi

if ! grep -Fq "bulkWrite(operations, { ordered: true, bypassDocumentValidation: true })" \
  "${deploy_root}/scripts/rollback-ranking-shares-5.2.6.js"; then
  echo 'Rollback must restore central mutations while the legacy collection remains frozen.' >&2
  exit 1
fi

echo 'Personal ranking cutover deployment tests passed.'
