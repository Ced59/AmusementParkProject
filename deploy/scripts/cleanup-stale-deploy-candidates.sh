#!/usr/bin/env bash
set -euo pipefail

compose_project_name="${1:-}"
if [[ ! "${compose_project_name}" =~ ^[a-zA-Z0-9][a-zA-Z0-9_.-]*$ ]]; then
  echo "A valid Docker Compose project name is required to clean deployment candidates." >&2
  exit 1
fi

candidate_names="$(
  docker ps -a \
    --filter "label=com.docker.compose.project=${compose_project_name}" \
    --filter 'label=com.docker.compose.oneoff=True' \
    --format '{{.Names}}'
)"

while IFS= read -r candidate_name; do
  case "${candidate_name}" in
    "${compose_project_name}-api-candidate-"*|"${compose_project_name}-front-candidate-"*)
      echo "Removing stale deployment candidate ${candidate_name}..."
      docker rm -f "${candidate_name}" >/dev/null
      ;;
  esac
done <<< "${candidate_names}"
