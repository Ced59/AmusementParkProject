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

mkdir -p "${output_dir}"
log_file="${SSR_WARMUP_LOG_FILE:-${output_dir}/ssr-warmup-$(date -u +%Y%m%dT%H%M%SZ).log}"
url_file="${SSR_WARMUP_URL_FILE:-${output_dir}/urls-${profile}.txt}"

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
export SSR_WARMUP_PROGRESS_EVERY_VALUE="${progress_every}"
export SSR_WARMUP_URL_FILTER_REGEX_VALUE="${url_filter_regex}"
export SSR_WARMUP_SITEMAP_FILTER_REGEX_VALUE="${sitemap_filter_regex}"

python3 - <<'PY'
import os
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
progress_every = max(1, int(os.environ['SSR_WARMUP_PROGRESS_EVERY_VALUE']))
url_filter_regex = os.environ['SSR_WARMUP_URL_FILTER_REGEX_VALUE'].strip()
sitemap_filter_regex = os.environ['SSR_WARMUP_SITEMAP_FILTER_REGEX_VALUE'].strip()
url_filter = re.compile(url_filter_regex) if url_filter_regex else None
sitemap_filter = re.compile(sitemap_filter_regex) if sitemap_filter_regex else None

# Keep the deploy script simple: fail loudly on unsupported modes.
allowed_profiles = {'critical', 'static', 'parks', 'references', 'items', 'full'}
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


def collect_urls() -> list[str]:
    sitemap_index_url = base_url + '/sitemap.xml'
    root = read_xml(sitemap_index_url)
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

    unique_urls = sorted(set(page_urls))
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
            elif cache_status.startswith('CSR-'):
                fallback += 1

            if index <= 20 or index % progress_every == 0 or status < 200 or status >= 300:
                log(f"{index}/{total} status={status} time={elapsed:.3f}s cache={cache_status} url={url}")

    elapsed_total = time.monotonic() - started
    log(f"Warmup finished: total={total}, success={success}, failed={failed}, hit={hit}, warmed={warmed}, fallback={fallback}, duration={elapsed_total:.1f}s")


log(f"SSR warmup configuration: base={base_url}, profile={profile}, concurrency={concurrency}, max_urls={max_urls}, refresh={refresh}")
warmup(collect_urls())
PY
