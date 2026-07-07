#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ -f .env ]; then
  # shellcheck disable=SC1091
  source ./scripts/env-loader.sh
  load_env_file .env
fi

public_base_url="${SSR_WARMUP_BASE_URL:-${PUBLIC_BASE_URL:-https://${PUBLIC_DOMAIN:-amusement-parks.fun}}}"
profile="${SSR_WARMUP_PROFILE:-critical}"
languages="${SSR_WARMUP_LANGS:-}"
concurrency="${SSR_WARMUP_CONCURRENCY:-1}"
timeout_seconds="${SSR_WARMUP_TIMEOUT_SECONDS:-90}"
sleep_seconds="${SSR_WARMUP_SLEEP_SECONDS:-0}"
max_urls="${SSR_WARMUP_MAX_URLS:-0}"
refresh="${SSR_WARMUP_REFRESH:-false}"
output_dir="${SSR_WARMUP_OUTPUT_DIR:-$(pwd)/warmup}"
progress_every="${SSR_WARMUP_PROGRESS_EVERY:-25}"
url_filter_regex="${SSR_WARMUP_URL_FILTER_REGEX:-}"
sitemap_filter_regex="${SSR_WARMUP_SITEMAP_FILTER_REGEX:-}"
url_file_configured="false"
if [ -n "${SSR_WARMUP_URL_FILE:-}" ]; then
  url_file_configured="true"
fi

mkdir -p "${output_dir}"
log_file="${SSR_WARMUP_LOG_FILE:-${output_dir}/ssr-warmup-$(date -u +%Y%m%dT%H%M%SZ).log}"
url_file="${SSR_WARMUP_URL_FILE:-${output_dir}/urls-${profile}.txt}"
report_file="${SSR_WARMUP_REPORT_FILE:-${output_dir}/ssr-warmup-report-$(date -u +%Y%m%dT%H%M%SZ).csv}"
lock_dir="${SSR_WARMUP_LOCK_DIR:-${output_dir}/.ssr-warmup.lock}"

mkdir -p "$(dirname "${log_file}")"

is_true() {
  local value
  value="$(printf '%s' "${1:-}" | tr '[:upper:]' '[:lower:]')"
  case "${value}" in
    1|true|yes|y)
      return 0
      ;;
    *)
      return 1
      ;;
  esac
}

write_lock_log() {
  printf '%s | %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$1" | tee -a "${log_file}" >&2
}

cleanup_warmup_lock() {
  rm -f "${lock_dir}/pid"
  rmdir "${lock_dir}" 2>/dev/null || true
}

acquire_warmup_lock() {
  local existing_pid
  local should_fail_if_locked

  if mkdir "${lock_dir}" 2>/dev/null; then
    printf '%s\n' "$$" > "${lock_dir}/pid"
    trap cleanup_warmup_lock EXIT
    trap 'cleanup_warmup_lock; exit 130' INT
    trap 'cleanup_warmup_lock; exit 143' TERM
    return 0
  fi

  existing_pid=""
  if [ -f "${lock_dir}/pid" ]; then
    existing_pid="$(cat "${lock_dir}/pid" 2>/dev/null || true)"
  fi

  case "${existing_pid}" in
    ''|*[!0-9]*)
      existing_pid=""
      ;;
  esac

  if [ -n "${existing_pid}" ] && kill -0 "${existing_pid}" 2>/dev/null; then
    write_lock_log "SSR warmup already running with pid=${existing_pid}; skipping this run."
    should_fail_if_locked="${SSR_WARMUP_FAIL_IF_LOCKED:-${SSR_WARMUP_REQUIRED:-false}}"
    if is_true "${should_fail_if_locked}"; then
      exit 1
    fi

    exit 0
  fi

  write_lock_log "Removing stale SSR warmup lock at ${lock_dir}."
  rm -f "${lock_dir}/pid"
  rmdir "${lock_dir}" 2>/dev/null || true

  if mkdir "${lock_dir}" 2>/dev/null; then
    printf '%s\n' "$$" > "${lock_dir}/pid"
    trap cleanup_warmup_lock EXIT
    trap 'cleanup_warmup_lock; exit 130' INT
    trap 'cleanup_warmup_lock; exit 143' TERM
    return 0
  fi

  write_lock_log "Unable to acquire SSR warmup lock at ${lock_dir}; skipping this run."
  should_fail_if_locked="${SSR_WARMUP_FAIL_IF_LOCKED:-${SSR_WARMUP_REQUIRED:-false}}"
  if is_true "${should_fail_if_locked}"; then
    exit 1
  fi

  exit 0
}

acquire_warmup_lock

export SSR_WARMUP_PUBLIC_BASE_URL="${public_base_url%/}"
export SSR_WARMUP_PROFILE_VALUE="${profile}"
export SSR_WARMUP_LANGS_VALUE="${languages}"
export SSR_WARMUP_CONCURRENCY_VALUE="${concurrency}"
export SSR_WARMUP_TIMEOUT_SECONDS_VALUE="${timeout_seconds}"
export SSR_WARMUP_SLEEP_SECONDS_VALUE="${sleep_seconds}"
export SSR_WARMUP_MAX_URLS_VALUE="${max_urls}"
export SSR_WARMUP_REFRESH_VALUE="${refresh}"
export SSR_WARMUP_LOG_FILE_VALUE="${log_file}"
export SSR_WARMUP_URL_FILE_VALUE="${url_file}"
export SSR_WARMUP_URL_FILE_CONFIGURED_VALUE="${url_file_configured:-}"
export SSR_WARMUP_REPORT_FILE_VALUE="${report_file}"
export SSR_WARMUP_PROGRESS_EVERY_VALUE="${progress_every}"
export SSR_WARMUP_URL_FILTER_REGEX_VALUE="${url_filter_regex}"
export SSR_WARMUP_SITEMAP_FILTER_REGEX_VALUE="${sitemap_filter_regex}"
export SSR_WARMUP_SEO_DOCUMENTS_VALUE="${SSR_WARMUP_SEO_DOCUMENTS:-true}"
export SSR_WARMUP_VALIDATE_BOT_VALUE="${SSR_WARMUP_VALIDATE_BOT:-true}"
export SSR_WARMUP_FAIL_ON_BOT_VALIDATION_VALUE="${SSR_WARMUP_FAIL_ON_BOT_VALIDATION:-true}"
export SSR_WARMUP_BOT_USER_AGENT_VALUE="${SSR_WARMUP_BOT_USER_AGENT:-Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)}"

python3 - <<'PY'
import os
import csv
import re
import ssl
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable
from xml.etree import ElementTree as ET

base_url = os.environ['SSR_WARMUP_PUBLIC_BASE_URL'].rstrip('/')
profile = os.environ['SSR_WARMUP_PROFILE_VALUE'].strip().lower() or 'critical'
languages_raw = os.environ['SSR_WARMUP_LANGS_VALUE'].strip()
languages = {item.strip().lower() for item in re.split(r'[;,]', languages_raw) if item.strip()} if languages_raw else set()
concurrency = max(1, int(os.environ['SSR_WARMUP_CONCURRENCY_VALUE']))
timeout_seconds = max(1, int(os.environ['SSR_WARMUP_TIMEOUT_SECONDS_VALUE']))
sleep_seconds = max(0.0, float(os.environ['SSR_WARMUP_SLEEP_SECONDS_VALUE']))
max_urls = max(0, int(os.environ['SSR_WARMUP_MAX_URLS_VALUE']))
refresh = os.environ['SSR_WARMUP_REFRESH_VALUE'].lower() in {'1', 'true', 'yes', 'y'}
log_file = Path(os.environ['SSR_WARMUP_LOG_FILE_VALUE'])
url_file = Path(os.environ['SSR_WARMUP_URL_FILE_VALUE'])
url_file_configured = os.environ['SSR_WARMUP_URL_FILE_CONFIGURED_VALUE'].strip().lower() in {'1', 'true', 'yes', 'y'}
report_file = Path(os.environ['SSR_WARMUP_REPORT_FILE_VALUE'])
progress_every = max(1, int(os.environ['SSR_WARMUP_PROGRESS_EVERY_VALUE']))
url_filter_regex = os.environ['SSR_WARMUP_URL_FILTER_REGEX_VALUE'].strip()
sitemap_filter_regex = os.environ['SSR_WARMUP_SITEMAP_FILTER_REGEX_VALUE'].strip()
url_filter = re.compile(url_filter_regex) if url_filter_regex else None
sitemap_filter = re.compile(sitemap_filter_regex) if sitemap_filter_regex else None
warmup_seo_documents_enabled = os.environ['SSR_WARMUP_SEO_DOCUMENTS_VALUE'].strip().lower() in {'1', 'true', 'yes', 'y'}
bot_validation_enabled = os.environ['SSR_WARMUP_VALIDATE_BOT_VALUE'].strip().lower() in {'1', 'true', 'yes', 'y'}
fail_on_bot_validation = os.environ['SSR_WARMUP_FAIL_ON_BOT_VALIDATION_VALUE'].strip().lower() in {'1', 'true', 'yes', 'y'}
bot_user_agent = os.environ['SSR_WARMUP_BOT_USER_AGENT_VALUE'].strip()

# Keep the deploy script simple: fail loudly on unsupported modes.
allowed_profiles = {'critical', 'static', 'parks', 'references', 'items', 'seo-important', 'full'}
if profile not in allowed_profiles:
    raise SystemExit(f"Unsupported SSR_WARMUP_PROFILE={profile!r}. Allowed: {', '.join(sorted(allowed_profiles))}")

log_file.parent.mkdir(parents=True, exist_ok=True)
url_file.parent.mkdir(parents=True, exist_ok=True)


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def log(message: str) -> None:
    line = f"{now()} | {message}"
    print(line, flush=True)
    with log_file.open('a', encoding='utf-8') as stream:
        stream.write(line + '\n')


def read_xml(url: str) -> ET.Element:
    request = urllib.request.Request(
        url,
        headers={
            'Accept': 'application/xml,text/xml,text/plain,*/*',
            'User-Agent': 'AmusementPark-SSR-Warmup/1.0',
        },
        method='GET',
    )
    with urllib.request.urlopen(request, timeout=timeout_seconds, context=ssl.create_default_context()) as response:
        return ET.fromstring(response.read())


def locs(root: ET.Element) -> list[str]:
    values: list[str] = []
    for element in root.iter():
        if element.tag.endswith('loc') and element.text:
            values.append(element.text.strip())
    return values


def get_path(url: str) -> str:
    parsed = urllib.parse.urlparse(url)
    return parsed.path or '/'


def normalize_configured_url(value: str) -> str:
    if value.startswith('http://') or value.startswith('https://'):
        return value
    if value.startswith('/'):
        return base_url + value
    return base_url + '/' + value


def unique_preserving_order(values: Iterable[str]) -> list[str]:
    ordered: list[str] = []
    seen: set[str] = set()
    for value in values:
        if value in seen:
            continue

        seen.add(value)
        ordered.append(value)

    return ordered


def language_matches(path: str) -> bool:
    if not languages:
        return True
    match = re.match(r'^/([a-z]{2})(?:/|$)', path, re.I)
    return match is not None and match.group(1).lower() in languages


def sitemap_matches(url: str) -> bool:
    if sitemap_filter is not None and not sitemap_filter.search(url):
        return False

    filename = get_path(url).rsplit('/', 1)[-1].lower()

    if profile == 'full':
        return True
    if profile == 'critical':
        return filename.startswith('static-')
    if profile == 'seo-important':
        return (
            filename.startswith('static-')
            or filename.startswith('parks-')
            or filename.startswith('park-opening-hours-')
            or filename.startswith('park-items-')
            or filename.startswith('history-')
            or filename.startswith('history-articles-')
            or filename.startswith('references-')
        )
    if profile == 'static':
        return filename.startswith('static-')
    if profile == 'parks':
        return filename.startswith('static-') or filename.startswith('parks-')
    if profile == 'references':
        return filename.startswith('references-')
    if profile == 'items':
        return filename.startswith('park-items-')

    return False


def url_matches(url: str) -> bool:
    if not url.startswith(base_url + '/') and url != base_url:
        return False

    if url_filter is not None and not url_filter.search(url):
        return False

    path = get_path(url)
    if not language_matches(path):
        return False

    if profile == 'critical':
        return bool(
            re.match(r'^/$', path)
            or re.match(r'^/[a-z]{2}/?$', path, re.I)
            or re.match(r'^/[a-z]{2}/(?:home|parks|about)/?$', path, re.I)
        )

    return True


def warmup_seo_document(path: str) -> tuple[str, int, float, str]:
    url = base_url + path
    headers = {
        'Accept': 'application/xml,text/xml,text/plain,*/*',
        'User-Agent': 'AmusementPark-SSR-Warmup/1.0',
    }
    started = time.monotonic()
    request = urllib.request.Request(url, headers=headers, method='GET')
    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds, context=ssl.create_default_context()) as response:
            # Read the whole body so the API fully generates the document and
            # populates its OutputCache (cold-start protection for Googlebot).
            response.read()
            status = int(response.status)
            cache_status = response.headers.get('X-AmusementPark-SEO-Cache') or '-'
            return url, status, time.monotonic() - started, cache_status
    except urllib.error.HTTPError as exc:
        return url, int(exc.code), time.monotonic() - started, 'HTTP-ERROR'
    except Exception as exc:  # noqa: BLE001 - deployment script needs resilient logging.
        return url, 0, time.monotonic() - started, f"ERROR:{exc}"


def collect_seo_document_paths(root: ET.Element) -> list[str]:
    # Always warm robots.txt and the sitemap index, plus EVERY section sitemap
    # referenced by the index (independent of the page warmup profile): Googlebot
    # fetches them all, so none of them must ever be served cold.
    paths: list[str] = ['/robots.txt', '/sitemap.xml']
    for url in locs(root):
        path = get_path(url)
        if path.endswith('.xml') and path != '/sitemap.xml':
            paths.append(path)

    ordered: list[str] = []
    seen: set[str] = set()
    for path in paths:
        if path not in seen:
            seen.add(path)
            ordered.append(path)

    return ordered


def warmup_seo_documents(root: ET.Element) -> None:
    paths = collect_seo_document_paths(root)
    log(f"SEO documents warmup: {len(paths)} target(s)")

    success = 0
    failed = 0
    for path in paths:
        url, status, elapsed, cache_status = warmup_seo_document(path)
        if 200 <= status < 300:
            success += 1
        else:
            failed += 1
        log(f"SEO status={status} time={elapsed:.3f}s cache={cache_status} url={url}")

    log(f"SEO documents warmup finished: total={len(paths)}, success={success}, failed={failed}")


def collect_urls(root: ET.Element) -> list[str]:
    if url_file_configured and not url_file.exists():
        raise SystemExit(f"Configured SSR_WARMUP_URL_FILE does not exist: {url_file}")

    if url_file_configured:
        configured_urls = [
            normalize_configured_url(line.strip())
            for line in url_file.read_text(encoding='utf-8').splitlines()
            if line.strip() and not line.strip().startswith('#')
        ]
        unique_configured_urls = unique_preserving_order(configured_urls)
        if max_urls > 0:
            unique_configured_urls = unique_configured_urls[:max_urls]
        log(f"Using configured warmup URL file: {url_file} -> {len(unique_configured_urls)} URL(s)")
        return unique_configured_urls

    sitemap_urls = [url for url in locs(root) if sitemap_matches(url)]
    page_urls: list[str] = []

    log(f"Collecting URLs from {len(sitemap_urls)} sitemap(s), profile={profile}, languages={','.join(sorted(languages)) or 'all'}")

    for sitemap_url in sitemap_urls:
        try:
            sitemap_root = read_xml(sitemap_url)
            sitemap_page_urls = [url for url in locs(sitemap_root) if url_matches(url)]
            page_urls.extend(sitemap_page_urls)
            log(f"OK sitemap {sitemap_url} -> {len(sitemap_page_urls)} URL(s)")
        except Exception as exc:  # noqa: BLE001 - deployment script needs resilient logging.
            log(f"ERROR sitemap {sitemap_url} -> {exc}")

    unique_urls = unique_preserving_order(page_urls)
    if max_urls > 0:
        unique_urls = unique_urls[:max_urls]

    url_file.write_text('\n'.join(unique_urls) + ('\n' if unique_urls else ''), encoding='utf-8')
    return unique_urls


def warmup_url(url: str) -> tuple[str, int, float, str]:
    headers = {
        'Accept': 'text/html,*/*',
        'User-Agent': 'AmusementPark-SSR-Warmup/1.0',
        'X-AmusementPark-SSR-Warmup': '1',
    }
    if refresh:
        headers['X-AmusementPark-SSR-Warmup-Refresh'] = '1'

    started = time.monotonic()
    request = urllib.request.Request(url, headers=headers, method='GET')
    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds, context=ssl.create_default_context()) as response:
            response.read(1024)
            status = int(response.status)
            cache_status = response.headers.get('X-AmusementPark-SSR-Cache') or response.headers.get('X-AmusementPark-SSR-Mode') or '-'
            return url, status, time.monotonic() - started, cache_status
    except urllib.error.HTTPError as exc:
        return url, int(exc.code), time.monotonic() - started, 'HTTP-ERROR'
    except Exception as exc:  # noqa: BLE001
        return url, 0, time.monotonic() - started, f"ERROR:{exc}"


def warmup(urls: list[str]) -> None:
    total = len(urls)
    if total == 0:
        log('No URL selected for warmup.')
        return

    log(f"Warmup started: total={total}, concurrency={concurrency}, timeout={timeout_seconds}s, refresh={refresh}, log={log_file}")
    success = 0
    failed = 0
    hit = 0
    warmed = 0
    fallback = 0
    started = time.monotonic()

    with ThreadPoolExecutor(max_workers=concurrency) as executor:
        futures = []
        for url in urls:
            futures.append(executor.submit(warmup_url, url))
            if sleep_seconds > 0:
                time.sleep(sleep_seconds)

        for index, future in enumerate(as_completed(futures), start=1):
            url, status, elapsed, cache_status = future.result()
            if 200 <= status < 300:
                success += 1
            else:
                failed += 1

            if cache_status == 'HIT' or cache_status == 'WARMUP-HIT':
                hit += 1
            elif cache_status == 'WARMED':
                warmed += 1
            elif cache_status.startswith('CSR'):
                fallback += 1

            if index <= 20 or index % progress_every == 0 or status < 200 or status >= 300:
                log(f"{index}/{total} status={status} time={elapsed:.3f}s cache={cache_status} url={url}")

    elapsed_total = time.monotonic() - started
    log(f"Warmup finished: total={total}, success={success}, failed={failed}, hit={hit}, warmed={warmed}, fallback={fallback}, duration={elapsed_total:.1f}s")


def has_title(html: str) -> bool:
    match = re.search(r'<title\b[^>]*>([\s\S]*?)</title>', html, re.I)
    return bool(match and re.sub(r'\s+', ' ', re.sub(r'<[^>]+>', ' ', match.group(1))).strip())


def has_meta_description(html: str) -> bool:
    for tag in re.findall(r'<meta\b[^>]*>', html, re.I):
        name = get_attribute(tag, 'name').lower()
        content = get_attribute(tag, 'content').strip()
        if name == 'description' and content:
            return True
    return False


def has_canonical(html: str) -> bool:
    for tag in re.findall(r'<link\b[^>]*>', html, re.I):
        rel_values = {value for value in get_attribute(tag, 'rel').lower().split() if value}
        href = get_attribute(tag, 'href').strip()
        if 'canonical' in rel_values and href:
            return True
    return False


def has_json_ld(html: str) -> bool:
    return bool(re.search(r'<script\b[^>]*type=["\']application/ld\+json["\'][^>]*>', html, re.I))


def has_bare_app_root(html: str) -> bool:
    return bool(
        re.search(r'<app-root\b[^>]*>\s*</app-root>', html, re.I)
        or re.search(r'<app-root\b[^>]*>\s*</body>', html, re.I)
    )


def get_attribute(tag: str, name: str) -> str:
    escaped_name = re.escape(name)
    match = re.search(rf'\s{escaped_name}\s*=\s*([\'"])(.*?)\1', tag, re.I)
    return match.group(2) if match else ''


def validate_bot_url(url: str) -> dict[str, str]:
    headers = {
        'Accept': 'text/html,*/*',
        'User-Agent': bot_user_agent,
    }
    started = time.monotonic()
    request = urllib.request.Request(url, headers=headers, method='GET')
    body = b''
    response_headers = {}
    status = 0
    error = ''

    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds, context=ssl.create_default_context()) as response:
            body = response.read()
            status = int(response.status)
            response_headers = response.headers
    except urllib.error.HTTPError as exc:
        status = int(exc.code)
        response_headers = exc.headers
        body = exc.read()
    except Exception as exc:  # noqa: BLE001
        error = str(exc)

    html = body.decode('utf-8', errors='replace')
    ssr_mode = response_headers.get('X-AmusementPark-SSR-Mode', '') if response_headers else ''
    ssr_cache = response_headers.get('X-AmusementPark-SSR-Cache', '') if response_headers else ''
    seo_ready = response_headers.get('X-AmusementPark-Seo-Ready', '') if response_headers else ''
    seo_ready_reason = response_headers.get('X-AmusementPark-Seo-Ready-Reason', '') if response_headers else ''
    robot_html = response_headers.get('X-AmusementPark-Robot-Html', '') if response_headers else ''
    retry_after = response_headers.get('Retry-After', '') if response_headers else ''

    title = has_title(html)
    meta_description = has_meta_description(html)
    canonical = has_canonical(html)
    json_ld = has_json_ld(html)
    bare_app_root = has_bare_app_root(html)
    failure_reasons: list[str] = []

    if error:
        failure_reasons.append('request-error')
    elif status != 200:
        failure_reasons.append(f'http-{status}')
    else:
        if seo_ready.lower() != 'true':
            failure_reasons.append(f"seo-ready-{seo_ready_reason or 'false'}")
        if ssr_mode.upper().startswith('CSR') or ssr_cache.upper().startswith('CSR'):
            failure_reasons.append('csr-fallback')
        if robot_html.lower() != 'no-js':
            failure_reasons.append(f"robot-html-{robot_html or 'missing'}")
        if not title:
            failure_reasons.append('missing-title')
        if not meta_description:
            failure_reasons.append('missing-meta-description')
        if not canonical:
            failure_reasons.append('missing-canonical')
        if bare_app_root:
            failure_reasons.append('bare-angular-shell')

    return {
        'url': url,
        'status': str(status),
        'ssr_mode': ssr_mode,
        'ssr_cache': ssr_cache,
        'seo_ready': seo_ready,
        'seo_ready_reason': seo_ready_reason,
        'robot_html': robot_html,
        'retry_after': retry_after,
        'body_bytes': str(len(body)),
        'has_title': str(title).lower(),
        'has_meta_description': str(meta_description).lower(),
        'has_canonical': str(canonical).lower(),
        'has_json_ld': str(json_ld).lower(),
        'bare_app_root': str(bare_app_root).lower(),
        'elapsed_seconds': f"{time.monotonic() - started:.3f}",
        'ok': str(len(failure_reasons) == 0).lower(),
        'failure_reasons': ';'.join(failure_reasons),
        'error': error,
    }


def validate_bot_urls(urls: list[str]) -> None:
    if not bot_validation_enabled:
        log('Bot validation skipped because SSR_WARMUP_VALIDATE_BOT is false.')
        return

    if not urls:
        log('Bot validation skipped because no URL was selected.')
        return

    report_file.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        'url',
        'status',
        'ssr_mode',
        'ssr_cache',
        'seo_ready',
        'seo_ready_reason',
        'robot_html',
        'retry_after',
        'body_bytes',
        'has_title',
        'has_meta_description',
        'has_canonical',
        'has_json_ld',
        'bare_app_root',
        'elapsed_seconds',
        'ok',
        'failure_reasons',
        'error',
    ]

    log(f"Bot validation started: total={len(urls)}, report={report_file}")
    rows: list[dict[str, str]] = []
    failed_rows: list[dict[str, str]] = []
    with ThreadPoolExecutor(max_workers=concurrency) as executor:
        futures = [executor.submit(validate_bot_url, url) for url in urls]
        for index, future in enumerate(as_completed(futures), start=1):
            row = future.result()
            rows.append(row)
            if row['ok'] != 'true':
                failed_rows.append(row)

            if index <= 20 or index % progress_every == 0 or row['ok'] != 'true':
                log(
                    "BOT "
                    f"{index}/{len(urls)} status={row['status']} seo={row['seo_ready'] or '-'} "
                    f"robot={row['robot_html'] or '-'} mode={row['ssr_mode'] or '-'} "
                    f"ok={row['ok']} url={row['url']}"
                )

    rows.sort(key=lambda item: item['url'])
    with report_file.open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    log(f"Bot validation finished: total={len(rows)}, failed={len(failed_rows)}, report={report_file}")
    if failed_rows and fail_on_bot_validation:
        examples = ', '.join(row['url'] for row in failed_rows[:5])
        raise SystemExit(f"Bot validation failed for {len(failed_rows)} URL(s). Examples: {examples}")


log(f"SSR warmup configuration: base={base_url}, profile={profile}, concurrency={concurrency}, max_urls={max_urls}, refresh={refresh}")

sitemap_index_root = read_xml(base_url + '/sitemap.xml')

if warmup_seo_documents_enabled:
    warmup_seo_documents(sitemap_index_root)

selected_urls = collect_urls(sitemap_index_root)
warmup(selected_urls)
validate_bot_urls(selected_urls)
PY
