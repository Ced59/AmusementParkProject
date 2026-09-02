#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deploy_scripts_dir="$(cd "${script_dir}/.." && pwd)"
activation_script="${deploy_scripts_dir}/reconcile-rating-ranking-eligibility-cache.sh"
deploy_script="${deploy_scripts_dir}/deploy.sh"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT

mock_bin="${temp_dir}/bin"
docker_log="${temp_dir}/docker.log"
state_dir="${temp_dir}/state"
mkdir -p "${mock_bin}"

cat > "${mock_bin}/docker" <<'MOCK_DOCKER'
#!/usr/bin/env bash
set -euo pipefail

printf '%s' "$*" | tr '\n' ' ' >> "${MOCK_DOCKER_LOG:?}"
printf '\n' >> "${MOCK_DOCKER_LOG:?}"
if [ "${MOCK_DOCKER_FAIL:-false}" = "true" ]; then
  exit 31
fi
MOCK_DOCKER
chmod +x "${mock_bin}/docker"

export PATH="${mock_bin}:${PATH}"
export MOCK_DOCKER_LOG="${docker_log}"
export RATING_RANKING_DEPLOYMENT_STATE_DIR="${state_dir}"
export RATINGS_ELIGIBILITY_ENABLED=true

if "${activation_script}" 'amusementpark;unsafe' >/dev/null 2>&1; then
  echo 'An unsafe Docker Compose project name was unexpectedly accepted.' >&2
  exit 1
fi

"${activation_script}" amusementpark

state_file="${state_dir}/ratings-eligibility-enabled"
if [ "$(cat "${state_file}")" != "true" ]; then
  echo 'The enabled transition was not recorded.' >&2
  exit 1
fi
if [ "$(wc -l < "${docker_log}")" -ne 1 ]; then
  echo 'The first activation must purge SSR caches exactly once.' >&2
  exit 1
fi
if ! grep -Fq 'internal/cache/invalidate' "${docker_log}"; then
  echo 'The transition did not call the internal SSR cache invalidation endpoint.' >&2
  exit 1
fi
if ! grep -Fq 'pageGroups: ["rating-rankings"]' "${docker_log}"; then
  echo 'The transition did not limit SSR invalidation to ranking-dependent pages.' >&2
  exit 1
fi
if grep -Fq 'all: true' "${docker_log}" || grep -Fq 'includeSeoDocuments: true' "${docker_log}"; then
  echo 'The transition unexpectedly requested a full SSR or static SEO purge.' >&2
  exit 1
fi

"${activation_script}" amusementpark
if [ "$(wc -l < "${docker_log}")" -ne 1 ]; then
  echo 'An unchanged eligibility state must not repeat the purge.' >&2
  exit 1
fi

export RATINGS_ELIGIBILITY_ENABLED=false
"${activation_script}" amusementpark
if [ "$(cat "${state_file}")" != "false" ] || [ "$(wc -l < "${docker_log}")" -ne 2 ]; then
  echo 'The rollback transition must purge caches and record the disabled state.' >&2
  exit 1
fi

export RATINGS_ELIGIBILITY_ENABLED=true
export MOCK_DOCKER_FAIL=true
if "${activation_script}" amusementpark >/dev/null 2>&1; then
  echo 'A rejected SSR purge unexpectedly succeeded.' >&2
  exit 1
fi
if [ "$(cat "${state_file}")" != "false" ]; then
  echo 'A failed purge must not advance the recorded eligibility state.' >&2
  exit 1
fi

cleanup_line="$(grep -n '^./scripts/cleanup-stale-deploy-candidates.sh ' "${deploy_script}" | cut -d: -f1)"
activation_line="$(grep -n '^./scripts/reconcile-rating-ranking-eligibility-cache.sh ' "${deploy_script}" | cut -d: -f1)"
integrity_line="$(grep -n '^./scripts/verify-public-response-integrity.sh ' "${deploy_script}" | cut -d: -f1)"
if [ -z "${cleanup_line}" ] || [ -z "${activation_line}" ] || [ -z "${integrity_line}" ] \
  || [ "${activation_line}" -le "${cleanup_line}" ] \
  || [ "${activation_line}" -ge "${integrity_line}" ]; then
  echo 'The eligibility transition purge must run after canonical cleanup and before public verification.' >&2
  exit 1
fi

echo 'Rating ranking activation tests passed.'
