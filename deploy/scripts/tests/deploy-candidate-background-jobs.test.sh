#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_script="$(cd "${script_dir}/.." && pwd)/deploy.sh"
candidate_function="$(sed -n '/^start_deploy_candidate() {$/,/^}$/p' "${deploy_script}")"

if ! grep -Fq 'if [ "${service_name}" = "api" ]; then' <<< "${candidate_function}"; then
  echo 'The deployment candidate must distinguish the API service.' >&2
  exit 1
fi

if ! grep -Fq -- '-e DurableBackgroundJobs__Worker__Enabled=false' <<< "${candidate_function}"; then
  echo 'The API deployment candidate must disable durable background job execution.' >&2
  exit 1
fi

echo 'Deployment candidate background job isolation tests passed.'
