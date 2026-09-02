#!/usr/bin/env bash
set -euo pipefail

compose_project_name="${1:-${COMPOSE_PROJECT_NAME:-amusementpark}}"
desired_state="${RATINGS_ELIGIBILITY_ENABLED:?RATINGS_ELIGIBILITY_ENABLED is required}"
state_directory="${RATING_RANKING_DEPLOYMENT_STATE_DIR:-.deployment-state}"
state_file="${state_directory}/ratings-eligibility-enabled"

if [[ ! "${compose_project_name}" =~ ^[a-zA-Z0-9][a-zA-Z0-9_.-]*$ ]]; then
  echo 'A valid Docker Compose project name is required to reconcile rating ranking caches.' >&2
  exit 1
fi

case "${desired_state}" in
  true|false)
    ;;
  *)
    echo 'RATINGS_ELIGIBILITY_ENABLED must be true or false.' >&2
    exit 1
    ;;
esac

previous_state=""
if [ -f "${state_file}" ]; then
  previous_state="$(tr -d '\r\n' < "${state_file}")"
fi

if [ "${previous_state}" = "${desired_state}" ]; then
  echo "Rating ranking eligibility state is unchanged (${desired_state}); cache purge skipped."
  exit 0
fi

echo "Rating ranking eligibility transition '${previous_state:-unset}' -> '${desired_state}'; invalidating ranking-dependent SSR pages."
docker compose --project-name "${compose_project_name}" -f compose.prod.yml exec -T front \
  node --input-type=module -e '
const response = await fetch("http://127.0.0.1:4000/internal/cache/invalidate", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-AmusementPark-Cache-Token": process.env.SSR_CACHE_INVALIDATION_TOKEN ?? "",
  },
  body: JSON.stringify({
    all: false,
    paths: [],
    prefixes: [],
    pageGroups: ["rating-rankings"],
    includeSeoDocuments: false,
    allowStale: false,
    refresh: false,
  }),
});
if (!response.ok) {
  const body = await response.text();
  throw new Error(`SSR cache invalidation failed with HTTP ${response.status}: ${body.slice(0, 500)}`);
}
'

mkdir -p "${state_directory}"
temporary_state_file="${state_file}.tmp.$$"
printf '%s\n' "${desired_state}" > "${temporary_state_file}"
mv "${temporary_state_file}" "${state_file}"
echo "Rating ranking eligibility cache transition recorded (${desired_state})."
