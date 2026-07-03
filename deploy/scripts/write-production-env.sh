#!/usr/bin/env bash
set -euo pipefail

output_file="${1:-deploy/.env.generated}"
mkdir -p "$(dirname "${output_file}")"

value_or_default() {
  local name="$1"
  local default_value="$2"
  local value="${!name:-}"

  if [ -n "${value}" ]; then
    printf '%s' "${value}"
  else
    printf '%s' "${default_value}"
  fi
}

write_line() {
  local name="$1"
  local value="$2"
  printf '%s=%s\n' "${name}" "${value}"
}

url_encode() {
  python3 -c 'import sys, urllib.parse; print(urllib.parse.quote(sys.stdin.read(), safe=""), end="")'
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

append_semicolon_value_if_missing() {
  local list="$1"
  local value="$2"

  if [ -z "${value// }" ]; then
    printf '%s' "${list}"
    return
  fi

  if semicolon_list_contains "${list}" "${value}"; then
    printf '%s' "${list}"
    return
  fi

  if [ -z "${list// }" ]; then
    printf '%s' "${value}"
    return
  fi

  printf '%s;%s' "${list}" "${value}"
}

append_required_allowed_hosts() {
  local list="$1"
  local ssr_api_url="$2"
  local ssr_api_host

  ssr_api_host="$(extract_url_host "${ssr_api_url}")"
  list="$(append_semicolon_value_if_missing "${list}" "${ssr_api_host}")"
  list="$(append_semicolon_value_if_missing "${list}" 'localhost')"
  list="$(append_semicolon_value_if_missing "${list}" '127.0.0.1')"

  printf '%s' "${list}"
}

public_domain="$(value_or_default PUBLIC_DOMAIN 'amusement-parks.fun')"
public_base_url="$(value_or_default PUBLIC_BASE_URL "https://${public_domain}")"
public_www_base_url="$(value_or_default PUBLIC_WWW_BASE_URL "https://www.${public_domain}")"
public_http_port="$(value_or_default PUBLIC_HTTP_PORT '18080')"
npm_docker_network_name="$(value_or_default NPM_DOCKER_NETWORK_NAME 'nginx-proxy-network')"
minio_api_port="$(value_or_default MINIO_API_PORT '19000')"
minio_console_port="$(value_or_default MINIO_CONSOLE_PORT '19001')"
front_ssr_api_internal_url="$(value_or_default FRONT_SSR_API_INTERNAL_URL 'http://api:8080')"
allowed_hosts="$(value_or_default ALLOWED_HOSTS "${public_domain};www.${public_domain};localhost;127.0.0.1;api;amusementpark-api")"
allowed_hosts="$(append_required_allowed_hosts "${allowed_hosts}" "${front_ssr_api_internal_url}")"
forwarded_allowed_hosts="$(value_or_default FORWARDED_HEADERS_ALLOWED_HOSTS "${public_domain};www.${public_domain};localhost;127.0.0.1")"
ssr_allowed_hosts="$(value_or_default SSR_ALLOWED_HOSTS "${public_domain};www.${public_domain};localhost;127.0.0.1")"

{
  write_line COMPOSE_PROJECT_NAME "$(value_or_default COMPOSE_PROJECT_NAME 'amusementpark')"
  write_line API_IMAGE "${API_IMAGE:?API_IMAGE is required}"
  write_line FRONT_IMAGE "${FRONT_IMAGE:?FRONT_IMAGE is required}"
  write_line PUBLIC_HTTP_PORT "${public_http_port}"
  write_line NPM_DOCKER_NETWORK_NAME "${npm_docker_network_name}"
  write_line PUBLIC_DOMAIN "${public_domain}"
  write_line PUBLIC_BASE_URL "${public_base_url}"
  write_line PUBLIC_WWW_BASE_URL "${public_www_base_url}"
  write_line BACKEND_PRIVATE_SUBNET "$(value_or_default BACKEND_PRIVATE_SUBNET '172.30.31.0/24')"
  write_line ALLOWED_HOSTS "${allowed_hosts}"
  write_line FORWARDED_HEADERS_ALLOWED_HOSTS "${forwarded_allowed_hosts}"
  write_line FORWARDED_HEADERS_KNOWN_NETWORKS "$(value_or_default FORWARDED_HEADERS_KNOWN_NETWORKS '172.30.31.0/24')"
  write_line FORWARDED_HEADERS_FORWARD_LIMIT "$(value_or_default FORWARDED_HEADERS_FORWARD_LIMIT '2')"
  write_line FRONT_SSR_API_INTERNAL_URL "${front_ssr_api_internal_url}"
  write_line SSR_ALLOWED_HOSTS "${ssr_allowed_hosts}"
  write_line SSR_FORCE_HTTPS "$(value_or_default SSR_FORCE_HTTPS 'true')"
  write_line SSR_CSP_ALLOW_LOCAL_DEV_SOURCES "$(value_or_default SSR_CSP_ALLOW_LOCAL_DEV_SOURCES 'false')"
  write_line CSP_ENABLED "$(value_or_default CSP_ENABLED 'true')"
  write_line CSP_REPORT_ONLY "$(value_or_default CSP_REPORT_ONLY 'true')"
  write_line CSP_REPORT_URI "$(value_or_default CSP_REPORT_URI '/security/csp-report')"
  write_line FRONT_CSP_REPORT_URI "$(value_or_default FRONT_CSP_REPORT_URI '/api/security/csp-report')"
  write_line SSR_SEO_DOCUMENT_CACHE_SECONDS "$(value_or_default SSR_SEO_DOCUMENT_CACHE_SECONDS '0')"
  write_line SSR_SEO_DOCUMENT_CACHE_MAX_ENTRIES "$(value_or_default SSR_SEO_DOCUMENT_CACHE_MAX_ENTRIES '128')"
  write_line SSR_SEO_DOCUMENT_BROWSER_CACHE_CONTROL "$(value_or_default SSR_SEO_DOCUMENT_BROWSER_CACHE_CONTROL 'no-cache, max-age=0, must-revalidate')"
  write_line SSR_RENDER_ENABLED "$(value_or_default SSR_RENDER_ENABLED 'true')"
  write_line SSR_RENDER_ON_CACHE_MISS "$(value_or_default SSR_RENDER_ON_CACHE_MISS 'false')"
  write_line SSR_RENDER_CRITICAL_ROUTES_ON_CACHE_MISS "$(value_or_default SSR_RENDER_CRITICAL_ROUTES_ON_CACHE_MISS 'true')"
  write_line SSR_ROBOT_NO_JS_HTML_ENABLED "$(value_or_default SSR_ROBOT_NO_JS_HTML_ENABLED 'true')"
  write_line SSR_CACHE_INVALIDATION_TOKEN "${SSR_CACHE_INVALIDATION_TOKEN:?SSR_CACHE_INVALIDATION_TOKEN is required}"
  write_line SSR_INTERNAL_BASE_URL "$(value_or_default SSR_INTERNAL_BASE_URL 'http://front:4000')"
  write_line SSR_PAGE_CACHE_SECONDS "$(value_or_default SSR_PAGE_CACHE_SECONDS '86400')"
  write_line SSR_PAGE_CACHE_MAX_ENTRIES "$(value_or_default SSR_PAGE_CACHE_MAX_ENTRIES '2000')"
  write_line SSR_PAGE_CACHE_BROWSER_CACHE_CONTROL "$(value_or_default SSR_PAGE_CACHE_BROWSER_CACHE_CONTROL 'no-cache, max-age=0, must-revalidate')"
  write_line SSR_PAGE_CACHE_MAX_HTML_BYTES "$(value_or_default SSR_PAGE_CACHE_MAX_HTML_BYTES '2097152')"
  write_line SSR_STALE_PAGE_CACHE_SECONDS "$(value_or_default SSR_STALE_PAGE_CACHE_SECONDS '600')"
  write_line SSR_TARGETED_REFRESH_ENABLED "$(value_or_default SSR_TARGETED_REFRESH_ENABLED 'true')"
  write_line SSR_TARGETED_REFRESH_MAX_URLS "$(value_or_default SSR_TARGETED_REFRESH_MAX_URLS '24')"
  write_line SSR_TARGETED_REFRESH_CONCURRENCY "$(value_or_default SSR_TARGETED_REFRESH_CONCURRENCY '1')"
  write_line SSR_TARGETED_REFRESH_DELAY_MILLISECONDS "$(value_or_default SSR_TARGETED_REFRESH_DELAY_MILLISECONDS '1500')"
  write_line SSR_TARGETED_REFRESH_TIMEOUT_SECONDS "$(value_or_default SSR_TARGETED_REFRESH_TIMEOUT_SECONDS '45')"
  write_line SSR_RENDER_MAX_CONCURRENCY "$(value_or_default SSR_RENDER_MAX_CONCURRENCY '1')"
  write_line SSR_RENDER_QUEUE_MAX_ENTRIES "$(value_or_default SSR_RENDER_QUEUE_MAX_ENTRIES '8')"
  write_line SSR_RENDER_QUEUE_WARNING_THRESHOLD "$(value_or_default SSR_RENDER_QUEUE_WARNING_THRESHOLD '6')"
  write_line SSR_SLOW_RENDER_THRESHOLD_MILLISECONDS "$(value_or_default SSR_SLOW_RENDER_THRESHOLD_MILLISECONDS '3000')"
  write_line SSR_PUBLIC_PAGE_CACHE_ALLOW_AUTH_COOKIES "$(value_or_default SSR_PUBLIC_PAGE_CACHE_ALLOW_AUTH_COOKIES 'true')"
  write_line SSR_DISK_PAGE_CACHE_ENABLED "$(value_or_default SSR_DISK_PAGE_CACHE_ENABLED 'true')"
  write_line SSR_DISK_PAGE_CACHE_DIR "$(value_or_default SSR_DISK_PAGE_CACHE_DIR '/var/cache/amusementpark-ssr')"
  write_line SSR_DISK_PAGE_CACHE_MAX_BYTES "$(value_or_default SSR_DISK_PAGE_CACHE_MAX_BYTES '4294967296')"
  write_line SSR_DISK_PAGE_CACHE_BUDGET_CHECK_EVERY_WRITES "$(value_or_default SSR_DISK_PAGE_CACHE_BUDGET_CHECK_EVERY_WRITES '100')"
  write_line SSR_TECHNICAL_STATS_PERSISTENCE_ENABLED "$(value_or_default SSR_TECHNICAL_STATS_PERSISTENCE_ENABLED 'true')"
  write_line SSR_TECHNICAL_STATS_RETENTION_DAYS "$(value_or_default SSR_TECHNICAL_STATS_RETENTION_DAYS '15')"
  write_line SSR_TECHNICAL_STATS_FLUSH_INTERVAL_SECONDS "$(value_or_default SSR_TECHNICAL_STATS_FLUSH_INTERVAL_SECONDS '60')"
  write_line SSR_ASSET_MISS_LOG_SAMPLE_RATE "$(value_or_default SSR_ASSET_MISS_LOG_SAMPLE_RATE '25')"
  write_line SSR_CSR_FALLBACK_LOG_SAMPLE_RATE "$(value_or_default SSR_CSR_FALLBACK_LOG_SAMPLE_RATE '100')"
  write_line SSR_CSR_FALLBACK_CACHE_CONTROL "$(value_or_default SSR_CSR_FALLBACK_CACHE_CONTROL 'public, max-age=60, stale-while-revalidate=300')"
  write_line SSR_WARMUP_AFTER_DEPLOY "$(value_or_default SSR_WARMUP_AFTER_DEPLOY 'false')"
  write_line SSR_WARMUP_BACKGROUND "$(value_or_default SSR_WARMUP_BACKGROUND 'true')"
  write_line SSR_WARMUP_REQUIRED "$(value_or_default SSR_WARMUP_REQUIRED 'false')"
  write_line SSR_WARMUP_PROFILE "$(value_or_default SSR_WARMUP_PROFILE 'critical')"
  write_line SSR_WARMUP_SEO_DOCUMENTS "$(value_or_default SSR_WARMUP_SEO_DOCUMENTS 'true')"
  write_line SSR_WARMUP_CONCURRENCY "$(value_or_default SSR_WARMUP_CONCURRENCY '1')"
  write_line SSR_WARMUP_TIMEOUT_SECONDS "$(value_or_default SSR_WARMUP_TIMEOUT_SECONDS '90')"
  write_line SSR_WARMUP_SLEEP_SECONDS "$(value_or_default SSR_WARMUP_SLEEP_SECONDS '0')"
  write_line SSR_WARMUP_MAX_URLS "$(value_or_default SSR_WARMUP_MAX_URLS '0')"
  write_line SSR_WARMUP_REFRESH "$(value_or_default SSR_WARMUP_REFRESH 'false')"
  write_line SSR_WARMUP_LANGS "$(value_or_default SSR_WARMUP_LANGS '')"
  write_line SSR_WARMUP_URL_FILE "$(value_or_default SSR_WARMUP_URL_FILE '')"
  write_line SSR_WARMUP_REPORT_FILE "$(value_or_default SSR_WARMUP_REPORT_FILE '')"
  write_line SSR_WARMUP_URL_FILTER_REGEX "$(value_or_default SSR_WARMUP_URL_FILTER_REGEX '')"
  write_line SSR_WARMUP_SITEMAP_FILTER_REGEX "$(value_or_default SSR_WARMUP_SITEMAP_FILTER_REGEX '')"
  write_line SSR_WARMUP_VALIDATE_BOT "$(value_or_default SSR_WARMUP_VALIDATE_BOT 'true')"
  write_line SSR_WARMUP_FAIL_ON_BOT_VALIDATION "$(value_or_default SSR_WARMUP_FAIL_ON_BOT_VALIDATION 'true')"
  write_line SSR_WARMUP_BOT_USER_AGENT "$(value_or_default SSR_WARMUP_BOT_USER_AGENT 'Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)')"
  write_line SEO_DEFAULT_LANGUAGE "$(value_or_default SEO_DEFAULT_LANGUAGE 'en')"
  write_line AUTH_RATE_LIMIT_LOGIN_LIMIT "$(value_or_default AUTH_RATE_LIMIT_LOGIN_LIMIT '5')"
  write_line AUTH_RATE_LIMIT_LOGIN_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_LOGIN_WINDOW_SECONDS '60')"
  write_line AUTH_RATE_LIMIT_EXTERNAL_LOGIN_LIMIT "$(value_or_default AUTH_RATE_LIMIT_EXTERNAL_LOGIN_LIMIT '10')"
  write_line AUTH_RATE_LIMIT_EXTERNAL_LOGIN_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_EXTERNAL_LOGIN_WINDOW_SECONDS '60')"
  write_line AUTH_RATE_LIMIT_REFRESH_TOKEN_LIMIT "$(value_or_default AUTH_RATE_LIMIT_REFRESH_TOKEN_LIMIT '30')"
  write_line AUTH_RATE_LIMIT_REFRESH_TOKEN_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_REFRESH_TOKEN_WINDOW_SECONDS '60')"
  write_line AUTH_RATE_LIMIT_REGISTRATION_LIMIT "$(value_or_default AUTH_RATE_LIMIT_REGISTRATION_LIMIT '5')"
  write_line AUTH_RATE_LIMIT_REGISTRATION_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_REGISTRATION_WINDOW_SECONDS '900')"
  write_line AUTH_RATE_LIMIT_EMAIL_CHALLENGE_LIMIT "$(value_or_default AUTH_RATE_LIMIT_EMAIL_CHALLENGE_LIMIT '3')"
  write_line AUTH_RATE_LIMIT_EMAIL_CHALLENGE_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_EMAIL_CHALLENGE_WINDOW_SECONDS '900')"
  write_line AUTH_RATE_LIMIT_PASSWORD_RESET_LIMIT "$(value_or_default AUTH_RATE_LIMIT_PASSWORD_RESET_LIMIT '5')"
  write_line AUTH_RATE_LIMIT_PASSWORD_RESET_WINDOW_SECONDS "$(value_or_default AUTH_RATE_LIMIT_PASSWORD_RESET_WINDOW_SECONDS '900')"
  write_line ASPNETCORE_ENVIRONMENT "$(value_or_default ASPNETCORE_ENVIRONMENT 'Production')"
  write_line MONGO_DATABASE_NAME "$(value_or_default MONGO_DATABASE_NAME 'AmusementPark')"
  write_line MONGO_INITDB_ROOT_USERNAME "${MONGO_INITDB_ROOT_USERNAME:?MONGO_INITDB_ROOT_USERNAME is required}"
  write_line MONGO_INITDB_ROOT_PASSWORD "${MONGO_INITDB_ROOT_PASSWORD:?MONGO_INITDB_ROOT_PASSWORD is required}"
  mongo_app_username="${MONGO_APP_USERNAME:?MONGO_APP_USERNAME is required}"
  mongo_app_password="${MONGO_APP_PASSWORD:?MONGO_APP_PASSWORD is required}"
  write_line MONGO_APP_USERNAME "${mongo_app_username}"
  write_line MONGO_APP_PASSWORD "${mongo_app_password}"
  write_line MONGO_APP_USERNAME_URL_ENCODED "$(printf '%s' "${mongo_app_username}" | url_encode)"
  write_line MONGO_APP_PASSWORD_URL_ENCODED "$(printf '%s' "${mongo_app_password}" | url_encode)"
  write_line MINIO_IMAGE "$(value_or_default MINIO_IMAGE 'quay.io/minio/minio:RELEASE.2025-07-23T15-54-02Z')"
  write_line MINIO_ROOT_USER "${MINIO_ROOT_USER:?MINIO_ROOT_USER is required}"
  write_line MINIO_ROOT_PASSWORD "${MINIO_ROOT_PASSWORD:?MINIO_ROOT_PASSWORD is required}"
  write_line MINIO_BUCKET "$(value_or_default MINIO_BUCKET 'amusement-park-images')"
  write_line MINIO_API_PORT "${minio_api_port}"
  write_line MINIO_CONSOLE_PORT "${minio_console_port}"
  write_line JWT_KEY "${JWT_KEY:?JWT_KEY is required}"
  write_line JWT_ISSUER "$(value_or_default JWT_ISSUER 'AmusementPark')"
  write_line JWT_AUDIENCE "$(value_or_default JWT_AUDIENCE 'AmusementPark')"
  write_line GOOGLE_CLIENT_ID "$(value_or_default GOOGLE_CLIENT_ID '')"
  write_line GOOGLE_CLIENT_SECRET "$(value_or_default GOOGLE_CLIENT_SECRET '')"
  write_line GOOGLE_REDIRECT_URI "$(value_or_default GOOGLE_REDIRECT_URI "${public_base_url%/}/api/auth/external/google/callback")"
  write_line FACEBOOK_APP_ID "$(value_or_default FACEBOOK_APP_ID '')"
  write_line FACEBOOK_APP_SECRET "$(value_or_default FACEBOOK_APP_SECRET '')"
  write_line EMAIL_MODE "$(value_or_default EMAIL_MODE 'Smtp')"
  write_line EMAIL_HOST "$(value_or_default EMAIL_HOST '')"
  write_line EMAIL_PORT "$(value_or_default EMAIL_PORT '587')"
  write_line EMAIL_USE_SSL "$(value_or_default EMAIL_USE_SSL 'false')"
  write_line EMAIL_USE_STARTTLS "$(value_or_default EMAIL_USE_STARTTLS 'true')"
  write_line EMAIL_USERNAME "$(value_or_default EMAIL_USERNAME '')"
  write_line EMAIL_PASSWORD "$(value_or_default EMAIL_PASSWORD '')"
  write_line EMAIL_FROM_ADDRESS "$(value_or_default EMAIL_FROM_ADDRESS "noreply@${public_domain}")"
  write_line EMAIL_FROM_NAME "$(value_or_default EMAIL_FROM_NAME 'Amusement Park')"
  write_line EMAIL_NOTIFICATION_ADMIN_ADDRESS "$(value_or_default EMAIL_NOTIFICATION_ADMIN_ADDRESS "admin@${public_domain}")"
  write_line EMAIL_CONTACT_ADDRESS "$(value_or_default EMAIL_CONTACT_ADDRESS "contact@${public_domain}")"
  write_line EMAIL_CONTACT_NOTIFICATIONS_ENABLED "$(value_or_default EMAIL_CONTACT_NOTIFICATIONS_ENABLED 'true')"
  write_line EMAIL_WEATHER_NOTIFICATIONS_ENABLED "$(value_or_default EMAIL_WEATHER_NOTIFICATIONS_ENABLED 'true')"
  write_line ADMIN_USER_ENABLED "$(value_or_default ADMIN_USER_ENABLED 'false')"
  write_line ADMIN_USER_EMAIL "$(value_or_default ADMIN_USER_EMAIL '')"
  write_line ADMIN_USER_PASSWORD "$(value_or_default ADMIN_USER_PASSWORD '')"
  write_line BACKUP_BEFORE_DEPLOY "$(value_or_default BACKUP_BEFORE_DEPLOY 'true')"
  write_line RUN_OPENING_HOURS_LOCALIZED_NOTES_MIGRATION "$(value_or_default RUN_OPENING_HOURS_LOCALIZED_NOTES_MIGRATION 'true')"
  write_line OPENING_HOURS_LOCALIZED_NOTES_MIGRATION_DRY_RUN "$(value_or_default OPENING_HOURS_LOCALIZED_NOTES_MIGRATION_DRY_RUN 'false')"
  write_line RUN_LEGACY_ENUM_MIGRATIONS "$(value_or_default RUN_LEGACY_ENUM_MIGRATIONS 'false')"
  write_line LEGACY_ENUM_MIGRATIONS_DRY_RUN "$(value_or_default LEGACY_ENUM_MIGRATIONS_DRY_RUN 'false')"
} > "${output_file}"

chmod 600 "${output_file}"
echo "Production environment file written to ${output_file}."
