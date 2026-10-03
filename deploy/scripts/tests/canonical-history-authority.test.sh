#!/usr/bin/env bash
set -euo pipefail

deploy_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
deploy_script="${deploy_root}/scripts/deploy.sh"

for required_proof in \
  'assert_canonical_history_authority' \
  'validationLevel === "off"' \
  'revisionOrigin:"LegacyMigration"' \
  'publicationState:{$in:["LegacyPublishedPendingReview","Suppressed"]}' \
  'migrationVersion:{$ne:"hist-canonical-v2"}' \
  'timelineSortOrdinal:{$exists:false}' \
  'publicationState:{$in:["Published","LegacyPublishedPendingReview"]}' \
  'subject.publicationPolicy":"HistoricalOnly"' \
  'subject.contextParkId":{$exists:false}' \
  'refusing deployment and preserving recovery artifacts'; do
  if ! grep -Fq "${required_proof}" "${deploy_script}"; then
    echo "Missing canonical history handoff proof: ${required_proof}" >&2
    exit 1
  fi
done

assertion_line="$(grep -n '^assert_canonical_history_authority$' "${deploy_script}" | cut -d: -f1)"
deployment_line="$(grep -n '^python3 ./scripts/deployment_transaction.py deploy$' "${deploy_script}" | cut -d: -f1)"
cleanup_line="$(grep -n '^remove_superseded_history_cutover_artifacts$' "${deploy_script}" | cut -d: -f1)"
if [ -z "${assertion_line}" ] || [ -z "${deployment_line}" ] || [ -z "${cleanup_line}" ] \
  || [ "${assertion_line}" -ge "${deployment_line}" ] \
  || [ "${deployment_line}" -ge "${cleanup_line}" ]; then
  echo 'Canonical history must be proven before exposure and artifacts removed only after commit.' >&2
  exit 1
fi

echo 'Canonical history authority handoff tests passed.'
