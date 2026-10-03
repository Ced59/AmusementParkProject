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
  echo 'The deployment must restore the pre-cutover historical state when cutover is interrupted.' >&2
  exit 1
fi

for mongo_script in \
  'freeze-history-authorities-5.4.81.js' \
  'release-history-write-freeze-5.4.81.js' \
  'complete-history-cutover-5.4.81.js' \
  'rollback-history-cutover-5.4.81.js'; do
  script_line="$(grep -n "${mongo_script}" "${deploy_script}" | head -n 1 | cut -d: -f1)"
  script_context="$(sed -n "$((script_line - 2)),$((script_line + 1))p" "${deploy_script}")"
  if ! grep -Fq -- '--file /dev/stdin' <<< "${script_context}"; then
    echo "MongoDB must parse ${mongo_script} as one complete file." >&2
    exit 1
  fi
done

if ! grep -Fq 'Historical rollback did not complete; the original writer remains isolated.' "${deploy_script}"; then
  echo 'A failed historical rollback must not restart its writer.' >&2
  exit 1
fi

for guarded_rollback_failure in \
  'The unexposed historical candidate could not be isolated; rollback is paused.' \
  'The historical data was restored, but its original writer did not restart.' \
  'The historical rollback completed, but its deployment journal remains armed.'; do
  if ! grep -Fq "${guarded_rollback_failure}" "${deploy_script}"; then
    echo "Historical rollback is missing a fail-closed transition: ${guarded_rollback_failure}" >&2
    exit 1
  fi
done

if ! grep -Fq 'recover_interrupted_unexposed_deployment' "${deploy_script}" \
  || ! grep -Fq 'deployment_transaction.py abandon-unexposed' "${deploy_script}"; then
  echo 'A diagnosed unexposed candidate must be recoverable by the next deployment.' >&2
  exit 1
fi

recovery_call_line="$(grep -n '^recover_interrupted_unexposed_deployment$' "${deploy_script}" | head -n 1 | cut -d: -f1)"
deployment_prepare_line="$(grep -n '^python3 ./scripts/deployment_transaction.py prepare$' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${recovery_call_line}" ] \
  || [ -z "${deployment_prepare_line}" ] \
  || [ "${recovery_call_line}" -ge "${deployment_prepare_line}" ]; then
  echo 'An unexposed interrupted deployment must be recovered before preparing new candidates.' >&2
  exit 1
fi

historical_rollback_line="$(grep -n 'rollback_incomplete_historical_history_cutover || historical_rollback_exit_code=$?' "${deploy_script}" | head -n 1 | cut -d: -f1)"
ranking_rollback_line="$(grep -n 'rollback_incomplete_personal_ranking_cutover || ranking_rollback_exit_code=$?' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${historical_rollback_line}" ] \
  || [ -z "${ranking_rollback_line}" ] \
  || [ "${historical_rollback_line}" -ge "${ranking_rollback_line}" ]; then
  echo 'Both business cutovers must be restored independently, even when historical rollback fails.' >&2
  exit 1
fi

rollback_arm_line="$(grep -n 'historical_history_cutover_started=true' "${deploy_script}" | head -n 1 | cut -d: -f1)"
freeze_command_line="$(grep -n 'freeze-history-authorities-5.4.81.js' "${deploy_script}" | head -n 1 | cut -d: -f1)"
writer_quiesce_line="$(grep -n 'quiesce-original-history-writer' "${deploy_script}" | head -n 1 | cut -d: -f1)"
candidate_prepare_line="$(grep -n 'deployment_transaction.py prepare-candidates' "${deploy_script}" | head -n 1 | cut -d: -f1)"
write_release_line="$(grep -n 'release-history-write-freeze-5.4.81.js' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${rollback_arm_line}" ] \
  || [ -z "${writer_quiesce_line}" ] \
  || [ -z "${freeze_command_line}" ] \
  || [ -z "${candidate_prepare_line}" ] \
  || [ -z "${write_release_line}" ] \
  || [ "${rollback_arm_line}" -ge "${writer_quiesce_line}" ] \
  || [ "${writer_quiesce_line}" -ge "${freeze_command_line}" ] \
  || [ "${freeze_command_line}" -ge "${candidate_prepare_line}" ] \
  || [ "${candidate_prepare_line}" -ge "${write_release_line}" ]; then
  echo 'History must be frozen until the isolated, unexposed candidate is healthy.' >&2
  exit 1
fi

if grep -Fq 'hasSource || total === 0 || pending > 0' "${deploy_script}"; then
  echo 'An empty canonical history without a legacy source must not replay the cutover.' >&2
  exit 1
fi

if ! grep -Fq 'hasSource || pending > 0' "${deploy_script}"; then
  echo 'The historical cutover detector must require a legacy source or pending canonical work.' >&2
  exit 1
fi

rollback_script_line="$(grep -n 'rollback-history-cutover-5.4.81.js' "${deploy_script}" | head -n 1 | cut -d: -f1)"
writer_restore_line="$(grep -n 'restore-original-history-writer' "${deploy_script}" | head -n 1 | cut -d: -f1)"
cutover_restored_line="$(grep -n 'cutover-restored --resource historical-history' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${rollback_script_line}" ] \
  || [ -z "${writer_restore_line}" ] \
  || [ -z "${cutover_restored_line}" ] \
  || [ "${rollback_script_line}" -ge "${writer_restore_line}" ] \
  || [ "${writer_restore_line}" -ge "${cutover_restored_line}" ]; then
  echo 'Rollback must finish before the original historical writer is restored and disarmed.' >&2
  exit 1
fi

if ! grep -Fq 'arm-cutover --resource historical-history' "${deploy_script}"; then
  echo 'The historical cutover must have an independently tracked transaction resource.' >&2
  exit 1
fi

resume_recovery_line="$(grep -n 'cutover-pending --resource historical-history' "${deploy_script}" | head -n 1 | cut -d: -f1)"
cutover_check_line="$(grep -n 'hist-canonical-v2' "${deploy_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${resume_recovery_line}" ] \
  || [ -z "${cutover_check_line}" ] \
  || [ "${resume_recovery_line}" -ge "${cutover_check_line}" ]; then
  echo 'A resumed historical cutover must restore its rollback flag before checking the physical source.' >&2
  exit 1
fi

deploy_transaction_line="$(grep -n 'deployment_transaction.py deploy' "${deploy_script}" | head -n 1 | cut -d: -f1)"
completion_line="$(grep -n '^complete_historical_history_cutover$' "${deploy_script}" | head -n 1 | cut -d: -f1)"
rollback_disarm_line="$(grep -n '^historical_history_cutover_started=false$' "${deploy_script}" | tail -n 1 | cut -d: -f1)"
if [ -z "${deploy_transaction_line}" ] \
  || [ -z "${completion_line}" ] \
  || [ -z "${rollback_disarm_line}" ] \
  || [ "${write_release_line}" -ge "${deploy_transaction_line}" ] \
  || [ "${deploy_transaction_line}" -ge "${completion_line}" ] \
  || [ "${completion_line}" -ge "${rollback_disarm_line}" ]; then
  echo 'Historical collections may be removed only after promotion and before rollback is disarmed.' >&2
  exit 1
fi

for required_cutover_check in \
  'hasSource || pending > 0' \
  'pending > 0' \
  'canonicalizationState:{$nin:'; do
  if ! grep -Fq "${required_cutover_check}" "${deploy_script}"; then
    echo "The deployment cutover check is incomplete: ${required_cutover_check}" >&2
    exit 1
  fi
done

freeze_script="${deploy_root}/scripts/freeze-history-authorities-5.4.81.js"
for required_freeze_step in \
  "renameCollection(frozenCollectionName, false)" \
  "createView(legacyCollectionName, frozenCollectionName, [])" \
  "legacyInfo[0].type === 'view'" \
  "frozenInfo[0].type !== 'collection'" \
  "collMod: narrativeCollectionName" \
  "{ cutoverVersion }" \
  "migrationVersion: canonicalizationVersion" \
  "canonicalizationState: { \$in: ['Canonicalized', 'Blocked'] }"; do
  if ! grep -Fq "${required_freeze_step}" "${freeze_script}"; then
    echo "The historical cutover is missing a write freeze step: ${required_freeze_step}" >&2
    exit 1
  fi
done

rollback_script="${deploy_root}/scripts/rollback-history-cutover-5.4.81.js"
for required_filter in \
  "cutoverVersion: canonicalCutoverVersion" \
  "narrativeContentId: { \$in: stagedNarrativeIds }" \
  "historical-narratives-cutover-backup-hist-canonical-v1" \
  "historical-facts-cutover-backup-hist-canonical-v1" \
  "historical-sources-cutover-backup-hist-canonical-v1" \
  "historical-cutover-state-hist-canonical-v1" \
  "backupStageComplete" \
  "restoreDocuments(narratives, narrativeBackup)" \
  "restoreDocuments(facts, factBackup)" \
  "restoreDocuments(sources, sourceBackup)" \
  "relationCollectionName = 'historical-relations'" \
  "collMod: collectionName" \
  "getCollection(legacyCollectionName).drop()" \
  "renameCollection(legacyCollectionName, false)"; do
  if ! grep -Fq "${required_filter}" "${rollback_script}"; then
    echo "Historical rollback is missing its targeted filter: ${required_filter}" >&2
    exit 1
  fi
done

release_script="${deploy_root}/scripts/release-history-write-freeze-5.4.81.js"
for required_release_step in \
  "editorialCollectionNames" \
  "'historical-narratives'" \
  "'historical-facts'" \
  "'historical-sources'" \
  "'historical-relations'" \
  "collMod: collectionName" \
  "validator: { \$expr: { \$eq: [1, 0] } }" \
  "validationLevel: 'strict'" \
  "validationAction: 'error'"; do
  if ! grep -Fq "${required_release_step}" "${release_script}"; then
    echo "The historical cleanup freeze is missing: ${required_release_step}" >&2
    exit 1
  fi
done

source_restore_line="$(grep -n 'renameCollection(legacyCollectionName, false)' "${rollback_script}" | tail -n 1 | cut -d: -f1)"
backup_cleanup_line="$(grep -n 'const droppedBackups = \[\]' "${rollback_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${source_restore_line}" ] \
  || [ -z "${backup_cleanup_line}" ] \
  || [ "${source_restore_line}" -ge "${backup_cleanup_line}" ]; then
  echo 'Rollback backups must remain available until historical source authority is restored.' >&2
  exit 1
fi

completion_script="${deploy_root}/scripts/complete-history-cutover-5.4.81.js"
for required_completion_step in \
  "history-events-cutover-source-hist-04-v1" \
  "history-events-backup-hist-04-v1" \
  "historical-narratives-cutover-backup-hist-canonical-v1" \
  "historical-facts-cutover-backup-hist-canonical-v1" \
  "historical-sources-cutover-backup-hist-canonical-v1" \
  "historical-cutover-state-hist-canonical-v1" \
  "cutoverPreviousCanonicalFactId: ''" \
  "bypassDocumentValidation: true" \
  "legacyFactsWithoutNarrative" \
  "completedLegacyNarratives !== legacyNarrativeIds.length" \
  "orphanedLegacySourceIds.length > 0" \
  "ordinaryFactsUsingLegacySources > 0" \
  "ordinaryRelationsUsingLegacySources > 0" \
  "relations.countDocuments(legacyRevisionFilter)" \
  "facts.deleteMany(legacyRevisionFilter)" \
  "sources.deleteMany(legacyRevisionFilter)" \
  "for (const collectionName of editorialCollectionNames)" \
  "collMod: collectionName" \
  "validator: {}" \
  "validationLevel: 'off'" \
  "deletedMigrationStates"; do
  if ! grep -Fq "${required_completion_step}" "${completion_script}"; then
    echo "Historical completion is missing its canonical cleanup step: ${required_completion_step}" >&2
    exit 1
  fi
done

legacy_delete_line="$(grep -n 'sources.deleteMany(legacyRevisionFilter)' "${completion_script}" | head -n 1 | cut -d: -f1)"
completion_unfreeze_line="$(grep -n 'for (const collectionName of editorialCollectionNames)' "${completion_script}" | head -n 1 | cut -d: -f1)"
if [ -z "${legacy_delete_line}" ] \
  || [ -z "${completion_unfreeze_line}" ] \
  || [ "${legacy_delete_line}" -ge "${completion_unfreeze_line}" ]; then
  echo 'Canonical history writes must remain frozen until legacy evidence is deleted.' >&2
  exit 1
fi

echo 'Historical history cutover deployment tests passed.'
