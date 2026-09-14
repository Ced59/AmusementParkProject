from __future__ import annotations

import os
import re
import time
from concurrent.futures import FIRST_COMPLETED, Future, ThreadPoolExecutor, wait
from typing import Callable, Iterable, Iterator, Protocol, TypeVar


class ReadableResponse(Protocol):
    def read(self, size: int = -1) -> bytes:
        ...


TInput = TypeVar('TInput')
TResult = TypeVar('TResult')


def get_host_load_average() -> tuple[float, float, float]:
    getloadavg = getattr(os, 'getloadavg', None)
    if getloadavg is None:
        raise OSError('load average is unavailable on this host')

    return getloadavg()


def build_html_request_headers(refresh: bool) -> dict[str, str]:
    headers = {
        'Accept': 'text/html,*/*',
        'Accept-Encoding': 'gzip',
        'User-Agent': 'AmusementPark-SSR-Warmup/1.0',
        'X-AmusementPark-SSR-Warmup': '1',
    }
    if refresh:
        headers['X-AmusementPark-SSR-Warmup-Refresh'] = '1'

    return headers


def validate_bot_html_assets(html: str, user_agent: str, robot_html: str) -> list[str]:
    expects_full_html = re.search(
        r'googlebot|adsbot-google|mediapartners-google|googleother|google-inspectiontool'
        r'|google-agent|googleagent-mariner|google-gemininotebook|google-notebooklm',
        user_agent,
        re.I,
    ) is not None
    if not expects_full_html:
        return [] if robot_html.lower() == 'no-js' else [f"robot-html-{robot_html or 'missing'}"]

    failures: list[str] = []
    if robot_html.lower() == 'no-js':
        failures.append('google-html-stripped')
    if not re.search(r'<script\b[^>]*\bsrc=["\'][^"\']+\.m?js(?:[?#][^"\']*)?["\']', html, re.I):
        failures.append('missing-google-scripts')
    if not re.search(r'<script\b[^>]*\bid=["\']ng-state["\']', html, re.I):
        failures.append('missing-google-transfer-state')
    if not re.search(r'<style\b|<link\b[^>]*\brel=["\']stylesheet["\']', html, re.I):
        failures.append('missing-google-styles')
    if not re.search(r'\bclass=["\'][^"\']+', html, re.I):
        failures.append('missing-google-presentation-classes')
    return failures


def drain_response(response: ReadableResponse, chunk_bytes: int = 64 * 1024) -> int:
    if chunk_bytes <= 0:
        raise ValueError('chunk_bytes must be positive')

    total_bytes = 0
    while True:
        chunk = response.read(chunk_bytes)
        if not chunk:
            return total_bytes

        total_bytes += len(chunk)


def map_bounded(
    values: Iterable[TInput],
    worker: Callable[[TInput], TResult],
    max_workers: int,
) -> Iterator[TResult]:
    if max_workers <= 0:
        raise ValueError('max_workers must be positive')

    iterator = iter(values)
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        pending: set[Future[TResult]] = set()
        for _ in range(max_workers):
            try:
                value = next(iterator)
            except StopIteration:
                break

            pending.add(executor.submit(worker, value))

        while pending:
            completed, pending = wait(pending, return_when=FIRST_COMPLETED)
            for future in completed:
                result = future.result()
                try:
                    value = next(iterator)
                except StopIteration:
                    pass
                else:
                    pending.add(executor.submit(worker, value))

                yield result


def wait_for_available_capacity(
    max_load_per_cpu: float,
    pause_seconds: float,
    *,
    load_provider: Callable[[], tuple[float, float, float]] = get_host_load_average,
    cpu_count_provider: Callable[[], int | None] = os.cpu_count,
    sleep: Callable[[float], None] = time.sleep,
    logger: Callable[[str], None] | None = None,
    log_every_pauses: int = 12,
) -> int:
    if max_load_per_cpu <= 0:
        return 0
    if pause_seconds <= 0:
        raise ValueError('pause_seconds must be positive')
    if log_every_pauses <= 0:
        raise ValueError('log_every_pauses must be positive')

    pause_count = 0
    while True:
        try:
            load_average = load_provider()[0]
        except (AttributeError, OSError):
            if logger is not None:
                logger('Warmup load guard unavailable on this host; continuing without load-based pauses.')
            return pause_count

        cpu_count = max(1, cpu_count_provider() or 1)
        load_per_cpu = load_average / cpu_count
        if load_per_cpu <= max_load_per_cpu:
            if pause_count > 0 and logger is not None:
                logger(
                    'Warmup load guard resumed requests: '
                    f'load_per_cpu={load_per_cpu:.2f}, threshold={max_load_per_cpu:.2f}'
                )
            return pause_count

        if logger is not None and (pause_count == 0 or pause_count % log_every_pauses == 0):
            logger(
                'Warmup load guard paused requests: '
                f'load_per_cpu={load_per_cpu:.2f}, threshold={max_load_per_cpu:.2f}, '
                f'pause={pause_seconds:.1f}s'
            )

        sleep(pause_seconds)
        pause_count += 1
