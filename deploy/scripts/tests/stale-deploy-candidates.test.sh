#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cleanup_script="$(cd "${script_dir}/.." && pwd)/cleanup-stale-deploy-candidates.sh"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT

mock_bin="${temp_dir}/bin"
docker_log="${temp_dir}/docker.log"
mkdir -p "${mock_bin}"

cat > "${mock_bin}/docker" <<'MOCK_DOCKER'
#!/usr/bin/env bash
set -euo pipefail

case "${1:-}" in
  ps)
    if [ "${MOCK_DOCKER_EMPTY:-false}" != "true" ]; then
      printf '%s\n' \
        'amusementpark-api-candidate-20260820T120000Z-100' \
        'amusementpark-front-candidate-20260820T120000Z-100' \
        'amusementpark-api' \
        'another-api-candidate-20260820T120000Z-100'
    fi
    ;;
  rm)
    printf '%s\n' "$*" >> "${MOCK_DOCKER_LOG:?}"
    ;;
  *)
    echo "Unexpected docker command: $*" >&2
    exit 1
    ;;
esac
MOCK_DOCKER
chmod +x "${mock_bin}/docker"

export PATH="${mock_bin}:${PATH}"
export MOCK_DOCKER_LOG="${docker_log}"

"${cleanup_script}" amusementpark

expected_log="${temp_dir}/expected.log"
printf '%s\n' \
  'rm -f amusementpark-api-candidate-20260820T120000Z-100' \
  'rm -f amusementpark-front-candidate-20260820T120000Z-100' > "${expected_log}"
diff -u "${expected_log}" "${docker_log}"

: > "${docker_log}"
export MOCK_DOCKER_EMPTY=true
"${cleanup_script}" amusementpark
if [ -s "${docker_log}" ]; then
  echo 'No candidate should be removed when Docker returns no matching container.' >&2
  exit 1
fi

if "${cleanup_script}" 'amusementpark;unsafe' >/dev/null 2>&1; then
  echo 'An unsafe Docker Compose project name was unexpectedly accepted.' >&2
  exit 1
fi

echo 'Stale deployment candidate cleanup tests passed.'
