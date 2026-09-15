import copy
import unittest
from unittest.mock import Mock
from urllib.error import HTTPError

from deployment_transition_observation import fixture_pair, matches_callback, matches_pair, wait_for_observation


class DeploymentTransitionObservationTests(unittest.TestCase):
    def setUp(self):
        self.previous = fixture_pair({"front": {"id": "a" * 64}, "api": {"id": "b" * 64}}, "old")
        self.expected = fixture_pair({"front": {"id": "c" * 64}, "api": {"id": "d" * 64}}, "new")

    def observe_pair(self, read, **options):
        return wait_for_observation(read, lambda value: matches_pair(value, self.previous, self.expected),
                                    "new pair", pause=lambda _: None, **options)

    def test_old_complete_pair_is_allowed_until_new_complete_pair_is_observed(self):
        read = Mock(side_effect=[self.previous, self.previous, self.expected])
        self.assertEqual(self.observe_pair(read), self.expected)
        self.assertEqual(read.call_count, 3)

    def test_mixed_pair_fails_immediately_even_if_next_response_would_be_correct(self):
        mixed = {"front": self.expected["front"], "api": self.previous["api"]}
        read = Mock(side_effect=[mixed, self.expected])
        with self.assertRaisesRegex(AssertionError, "mixed front/API"):
            self.observe_pair(read)
        self.assertEqual(read.call_count, 1)

    def test_unknown_identity_or_wrong_version_is_not_retried(self):
        for field, value in (("name", "unknown"), ("version", "unexpected")):
            with self.subTest(field=field):
                invalid = copy.deepcopy(self.expected)
                invalid["front"][field] = value
                read = Mock(side_effect=[invalid, self.expected])
                with self.assertRaises(AssertionError):
                    self.observe_pair(read)
                self.assertEqual(read.call_count, 1)

    def test_http_failure_is_not_retried(self):
        read = Mock(side_effect=[HTTPError("http://fixture.test/pair", 502, "bad gateway", {}, None), self.expected])
        with self.assertRaises(HTTPError):
            self.observe_pair(read)
        self.assertEqual(read.call_count, 1)

    def test_previous_pair_cannot_satisfy_the_deadline(self):
        read = Mock(return_value=self.previous)
        with self.assertRaisesRegex(AssertionError, "Timed out observing new pair"):
            self.observe_pair(read, seconds=1, clock=Mock(side_effect=[0, 0, 1]))
        self.assertEqual(read.call_count, 1)
        read.assert_called_once_with(1)

    def test_callback_waits_for_new_api_and_new_front_after_valid_transitional_calls(self):
        calls = [{"status": 200, "api": self.previous["api"], "callback": self.expected["front"]},
                 {"status": 200, "api": self.expected["api"], "callback": self.previous["front"]},
                 {"status": 200, "api": self.expected["api"], "callback": self.expected["front"]}]
        read = Mock(side_effect=calls)
        observed = wait_for_observation(read, lambda value: matches_callback(value, self.previous, self.expected),
                                        "new callback", pause=lambda _: None)
        self.assertEqual(observed, calls[-1])
        self.assertEqual(read.call_count, 3)

    def test_invalid_callback_status_or_identity_fails_without_retry(self):
        for status, front in ((403, self.expected["front"]), (200, {"name": "unknown"})):
            with self.subTest(status=status):
                read = Mock(return_value={"status": status, "api": self.expected["api"], "callback": front})
                with self.assertRaisesRegex(AssertionError, "Invalid callback"):
                    wait_for_observation(read, lambda value: matches_callback(value, self.previous, self.expected),
                                         "callback", pause=lambda _: None)
                self.assertEqual(read.call_count, 1)


if __name__ == "__main__":
    unittest.main()
