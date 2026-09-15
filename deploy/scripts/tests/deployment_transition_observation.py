"""Strict observations for the CI fixture's asynchronous Nginx handoff."""
import json
import time


def fixture_pair(references, version):
    return {service: {"kind": service, "name": references[service]["id"][:12], "version": version}
            for service in ("front", "api")}


def matches_pair(observed, previous, expected):
    if observed == expected:
        return True
    if observed == previous:
        return False
    raise AssertionError(f"Unknown or mixed front/API pair: {json.dumps(observed, sort_keys=True)}")


def matches_callback(observed, previous, expected):
    # A callback is a second edge request: its API caller may belong to the
    # previous pair while the callback already reaches the new front.
    if (observed.get("status") != 200
            or observed.get("api") not in (previous["api"], expected["api"])
            or observed.get("callback") not in (previous["front"], expected["front"])):
        raise AssertionError(f"Invalid callback during handoff: {json.dumps(observed, sort_keys=True)}")
    return observed["api"] == expected["api"] and observed["callback"] == expected["front"]


def wait_for_observation(read, is_expected, label, seconds=20, *, clock=time.monotonic, pause=time.sleep):
    deadline = clock() + seconds
    while True:
        # Transport errors and invalid identities propagate immediately. Only
        # a recognized transitional response can lead to another observation.
        observed = read(max(0.001, deadline - clock()))
        if is_expected(observed):
            return observed
        if clock() >= deadline:
            raise AssertionError(f"Timed out observing {label}; last valid response: {json.dumps(observed, sort_keys=True)}")
        pause(0.05)
