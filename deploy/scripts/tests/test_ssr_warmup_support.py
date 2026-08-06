import threading
import time
import unittest
from pathlib import Path

from deploy.scripts.ssr_warmup_support import (
    build_html_request_headers,
    drain_response,
    map_bounded,
    wait_for_available_capacity,
)


class FakeResponse:
    def __init__(self, chunks: list[bytes]) -> None:
        self._chunks = iter(chunks)
        self.read_sizes: list[int] = []

    def read(self, size: int = -1) -> bytes:
        self.read_sizes.append(size)
        return next(self._chunks, b'')


class SsrWarmupSupportTests(unittest.TestCase):
    def test_html_headers_request_compressed_body_and_optional_refresh(self) -> None:
        regular_headers = build_html_request_headers(refresh=False)
        refresh_headers = build_html_request_headers(refresh=True)

        self.assertEqual('gzip', regular_headers['Accept-Encoding'])
        self.assertNotIn('X-AmusementPark-SSR-Warmup-Refresh', regular_headers)
        self.assertEqual('1', refresh_headers['X-AmusementPark-SSR-Warmup-Refresh'])

    def test_drain_response_consumes_every_chunk_until_eof(self) -> None:
        response = FakeResponse([b'a' * 7, b'b' * 5, b''])

        total_bytes = drain_response(response, chunk_bytes=8)

        self.assertEqual(12, total_bytes)
        self.assertEqual([8, 8, 8], response.read_sizes)

    def test_map_bounded_processes_all_values_with_configured_concurrency(self) -> None:
        active_workers = 0
        maximum_active_workers = 0
        lock = threading.Lock()

        def worker(value: int) -> int:
            nonlocal active_workers, maximum_active_workers
            with lock:
                active_workers += 1
                maximum_active_workers = max(maximum_active_workers, active_workers)
            time.sleep(0.01)
            with lock:
                active_workers -= 1
            return value * 2

        results = list(map_bounded(range(8), worker, max_workers=2))

        self.assertEqual([value * 2 for value in range(8)], sorted(results))
        self.assertLessEqual(maximum_active_workers, 2)

    def test_map_bounded_does_not_eagerly_submit_the_complete_selection(self) -> None:
        consumed_values: list[int] = []

        def values():
            for value in range(100):
                consumed_values.append(value)
                yield value

        results = map_bounded(values(), lambda value: value, max_workers=2)

        next(results)

        self.assertLessEqual(len(consumed_values), 3)
        results.close()

    def test_load_guard_pauses_until_capacity_is_available(self) -> None:
        loads = iter([(4.0, 4.0, 4.0), (2.0, 2.0, 2.0), (1.0, 1.0, 1.0)])
        sleeps: list[float] = []
        logs: list[str] = []

        pause_count = wait_for_available_capacity(
            max_load_per_cpu=0.75,
            pause_seconds=5.0,
            load_provider=lambda: next(loads),
            cpu_count_provider=lambda: 2,
            sleep=sleeps.append,
            logger=logs.append,
        )

        self.assertEqual(2, pause_count)
        self.assertEqual([5.0, 5.0], sleeps)
        self.assertIn('paused requests', logs[0])
        self.assertIn('resumed requests', logs[-1])

    def test_load_guard_can_be_disabled(self) -> None:
        pause_count = wait_for_available_capacity(
            max_load_per_cpu=0,
            pause_seconds=5.0,
            load_provider=lambda: self.fail('load provider should not be called'),
        )

        self.assertEqual(0, pause_count)

    def test_embedded_warmup_python_compiles(self) -> None:
        script_path = Path(__file__).resolve().parents[1] / 'warmup-ssr-cache.sh'
        script = script_path.read_text(encoding='utf-8')
        marker = "python3 - <<'PY'\n"
        embedded_python = script.split(marker, maxsplit=1)[1].rsplit('\nPY', maxsplit=1)[0]

        compile(embedded_python, str(script_path), 'exec')


if __name__ == '__main__':
    unittest.main()
