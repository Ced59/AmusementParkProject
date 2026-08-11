#!/usr/bin/env bash
set -euo pipefail

url="${1:?Usage: verify-public-response-integrity.sh <public-html-url>}"
user_agent="${2:-AmusementPark-Deployment-Transport-Smoke/1.0}"
max_attempts="${3:-5}"
attempt=1
temporary_directory="$(mktemp -d)"
trap 'rm -rf "${temporary_directory}"' EXIT

headers_file="${temporary_directory}/headers.txt"
body_file="${temporary_directory}/body.bin"

while [ "${attempt}" -le "${max_attempts}" ]; do
  : > "${headers_file}"
  : > "${body_file}"

  if status_code="$(curl --silent --show-error --fail \
    --http1.1 \
    --user-agent "${user_agent}" \
    --header 'Accept: text/html' \
    --header 'Accept-Encoding: identity' \
    --dump-header "${headers_file}" \
    --output "${body_file}" \
    --write-out '%{http_code}' \
    "${url}")"; then
    content_length="$(awk '
      tolower($1) == "content-length:" {
        gsub("\\r", "", $2)
        value = $2
      }
      END { print value }
    ' "${headers_file}")"
    actual_length="$(wc -c < "${body_file}" | tr -d '[:space:]')"

    if [ "${status_code}" = "200" ] \
      && { [ -z "${content_length}" ] || [ "${actual_length}" = "${content_length}" ]; }; then
      echo "Public response integrity verified: HTTP 200, ${actual_length} complete bytes."
      exit 0
    fi

    echo "Public response integrity attempt ${attempt}/${max_attempts} returned HTTP ${status_code}, expected bytes ${content_length:-unknown}, received bytes ${actual_length}." >&2
  else
    echo "Public response integrity attempt ${attempt}/${max_attempts} failed while downloading the response." >&2
  fi

  attempt=$((attempt + 1))
  if [ "${attempt}" -le "${max_attempts}" ]; then
    sleep 3
  fi
done

echo "Public response body is incomplete after ${max_attempts} attempts: ${url}" >&2
exit 1
