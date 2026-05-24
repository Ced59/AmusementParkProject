#!/usr/bin/env bash

load_env_file() {
  local env_file="$1"

  if [ ! -f "${env_file}" ]; then
    echo "Missing environment file: ${env_file}" >&2
    return 1
  fi

  while IFS= read -r raw_line || [ -n "${raw_line}" ]; do
    local line="${raw_line}"

    line="$(printf '%s' "${line}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"

    if [ -z "${line}" ] || [[ "${line}" == \#* ]]; then
      continue
    fi

    if [[ "${line}" != *=* ]]; then
      echo "Invalid environment line without '=': ${line}" >&2
      return 1
    fi

    local name="${line%%=*}"
    local value="${line#*=}"

    name="$(printf '%s' "${name}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
    value="$(printf '%s' "${value}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"

    if [[ ! "${name}" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
      echo "Invalid environment variable name: ${name}" >&2
      return 1
    fi

    if [ "${#value}" -ge 2 ]; then
      local first_char="${value:0:1}"
      local last_char="${value: -1}"

      if { [ "${first_char}" = "'" ] && [ "${last_char}" = "'" ]; } \
        || { [ "${first_char}" = '"' ] && [ "${last_char}" = '"' ]; }; then
        value="${value:1:${#value}-2}"
      fi
    fi

    export "${name}=${value}"
  done < "${env_file}"
}
