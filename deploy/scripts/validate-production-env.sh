#!/usr/bin/env bash
set -euo pipefail

env_file="${1:-.env}"

if [ ! -f "${env_file}" ]; then
  echo "Missing production environment file: ${env_file}" >&2
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${script_dir}/env-loader.sh"
load_env_file "${env_file}"

errors=0
warnings=0

require_value() {
  local name="$1"
  local value="${!name:-}"

  if [ -z "${value// }" ]; then
    echo "ERROR: ${name} is required." >&2
    errors=$((errors + 1))
  fi
}

reject_placeholder() {
  local name="$1"
  local value="${!name:-}"
  local upper_value

  upper_value="$(printf '%s' "${value}" | tr '[:lower:]' '[:upper:]')"

  case "${upper_value}" in
    *VIA-USER-SECRETS*|*CHANGE_ME*|*CHANGEME*|*TODO*|SECRET|PASSWORD|MINIOADMIN)
      echo "ERROR: ${name} still uses a placeholder/default value." >&2
      errors=$((errors + 1))
      ;;
  esac
}

require_https_public_url() {
  local name="$1"
  local value="${!name:-}"

  if [ -z "${value// }" ]; then
    echo "ERROR: ${name} is required." >&2
    errors=$((errors + 1))
    return
  fi

  if [[ ! "${value}" =~ ^https://[^/]+/?$ ]]; then
    echo "ERROR: ${name} must be a root https origin, for example https://amusement-parks.fun." >&2
    errors=$((errors + 1))
  fi

  if [[ "${value}" =~ localhost|127\.0\.0\.1|::1 ]]; then
    echo "ERROR: ${name} must not point to localhost in production." >&2
    errors=$((errors + 1))
  fi
}

reject_wildcard() {
  local name="$1"
  local value="${!name:-}"

  if [[ "${value}" == *"*"* ]]; then
    echo "ERROR: ${name} must not contain a wildcard in production." >&2
    errors=$((errors + 1))
  fi
}

extract_url_host() {
  local url="$1"
  local without_scheme
  local authority

  without_scheme="${url#*://}"

  if [ "${without_scheme}" = "${url}" ]; then
    printf ''
    return
  fi

  authority="${without_scheme%%/*}"
  printf '%s' "${authority%%:*}"
}

semicolon_list_contains() {
  local list="$1"
  local expected="$2"

  [[ ";${list};" == *";${expected};"* ]]
}

validate_front_ssr_api_host() {
  local url="${FRONT_SSR_API_INTERNAL_URL:-http://api:8080}"
  local host

  host="$(extract_url_host "${url}")"

  if [ -z "${host}" ]; then
    echo "ERROR: FRONT_SSR_API_INTERNAL_URL must be an absolute URL, for example http://api:8080." >&2
    errors=$((errors + 1))
    return
  fi

  if ! semicolon_list_contains "${ALLOWED_HOSTS:-}" "${host}"; then
    echo "ERROR: ALLOWED_HOSTS must contain FRONT_SSR_API_INTERNAL_URL host '${host}' to avoid API 400 Invalid Hostname responses during SSR." >&2
    errors=$((errors + 1))
  fi
}


validate_port() {
  local name="$1"
  local value="${!name:-}"

  if [ -z "${value// }" ]; then
    return
  fi

  if [[ ! "${value}" =~ ^[0-9]+$ ]] || [ "${value}" -lt 1 ] || [ "${value}" -gt 65535 ]; then
    echo "ERROR: ${name} must be a valid TCP port between 1 and 65535." >&2
    errors=$((errors + 1))
  fi
}

warn_missing() {
  local name="$1"
  local value="${!name:-}"

  if [ -z "${value// }" ]; then
    echo "WARNING: ${name} is empty. This is allowed only if the corresponding feature is intentionally disabled." >&2
    warnings=$((warnings + 1))
  fi
}

required_names=(
  API_IMAGE
  FRONT_IMAGE
  PUBLIC_BASE_URL
  PUBLIC_DOMAIN
  ALLOWED_HOSTS
  FORWARDED_HEADERS_ALLOWED_HOSTS
  FORWARDED_HEADERS_KNOWN_NETWORKS
  SSR_ALLOWED_HOSTS
  NPM_DOCKER_NETWORK_NAME
  MONGO_INITDB_ROOT_USERNAME
  MONGO_INITDB_ROOT_PASSWORD
  MONGO_APP_USERNAME
  MONGO_APP_PASSWORD
  MONGO_APP_USERNAME_URL_ENCODED
  MONGO_APP_PASSWORD_URL_ENCODED
  MINIO_ROOT_USER
  MINIO_ROOT_PASSWORD
  JWT_KEY
  JWT_ISSUER
  JWT_AUDIENCE
  GOOGLE_CLIENT_ID
  GOOGLE_CLIENT_SECRET
  GOOGLE_REDIRECT_URI
)

for required_name in "${required_names[@]}"; do
  require_value "${required_name}"
  reject_placeholder "${required_name}"
done

require_https_public_url PUBLIC_BASE_URL

if [ -n "${PUBLIC_WWW_BASE_URL:-}" ]; then
  require_https_public_url PUBLIC_WWW_BASE_URL
fi

reject_wildcard ALLOWED_HOSTS
reject_wildcard FORWARDED_HEADERS_ALLOWED_HOSTS
reject_wildcard SSR_ALLOWED_HOSTS
validate_front_ssr_api_host

if [[ "${ALLOWED_HOSTS:-}" == *"localhost"* ]] || [[ "${ALLOWED_HOSTS:-}" == *"127.0.0.1"* ]]; then
  echo "NOTICE: ALLOWED_HOSTS contains localhost/127.0.0.1 for internal healthchecks. Keep API ports private." >&2
fi

if [[ "${SSR_ALLOWED_HOSTS:-}" == *"amusement.localhost"* ]] || [[ "${SSR_ALLOWED_HOSTS:-}" == *"matomo.amusement.localhost"* ]]; then
  echo "ERROR: SSR_ALLOWED_HOSTS must not contain local-prod hostnames in production." >&2
  errors=$((errors + 1))
fi

if [ "${SSR_CSP_ALLOW_LOCAL_DEV_SOURCES:-false}" != "false" ]; then
  echo "ERROR: SSR_CSP_ALLOW_LOCAL_DEV_SOURCES must stay false in production." >&2
  errors=$((errors + 1))
fi

validate_port PUBLIC_HTTP_PORT

if [[ "${NPM_DOCKER_NETWORK_NAME:-}" =~ [[:space:]] ]]; then
  echo "ERROR: NPM_DOCKER_NETWORK_NAME must not contain spaces." >&2
  errors=$((errors + 1))
fi

validate_port MINIO_API_PORT
validate_port MINIO_CONSOLE_PORT


jwt_value="${JWT_KEY:-}"
jwt_length="${#jwt_value}"
if [ "${jwt_length}" -lt 32 ]; then
  echo "ERROR: JWT_KEY must contain at least 32 characters." >&2
  errors=$((errors + 1))
fi

case "${EMAIL_MODE:-Console}" in
  Console)
    echo "WARNING: EMAIL_MODE=Console. This is acceptable for a discreet MVP smoke test, but SMTP should be configured before real users." >&2
    warnings=$((warnings + 1))
    ;;
  Smtp)
    for smtp_name in EMAIL_HOST EMAIL_USERNAME EMAIL_PASSWORD EMAIL_FROM_ADDRESS; do
      require_value "${smtp_name}"
      reject_placeholder "${smtp_name}"
    done
    ;;
  *)
    echo "ERROR: EMAIL_MODE must be Console or Smtp." >&2
    errors=$((errors + 1))
    ;;
esac

if [ "${errors}" -gt 0 ]; then
  echo "Production environment validation failed with ${errors} error(s) and ${warnings} warning(s)." >&2
  exit 1
fi

echo "Production environment validation completed with ${warnings} warning(s)."
