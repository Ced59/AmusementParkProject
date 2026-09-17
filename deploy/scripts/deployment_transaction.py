#!/usr/bin/env python3
"""Two-pair deployment with persisted routing intent and conservative recovery."""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile
import time
import uuid

from deployment_runtime import DeploymentError, DockerRuntime, NAME


PHASES = {"prepared", "abandoning", "switch-candidate", "replace-canonical", "switch-canonical", "cleanup", "complete", "abandoned"}
SHARED_INFRASTRUCTURE_MAINTENANCE_SETTING = "SHARED_INFRASTRUCTURE_MAINTENANCE"


def atomic_write(path: Path, content: str, mode=0o600):
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as output:
            output.write(content)
            output.flush()
            os.fsync(output.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, path)
        if os.name == "posix":
            directory = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
            try:
                os.fsync(directory)
            finally:
                os.close(directory)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def consume_shared_infrastructure_maintenance(path: Path, expected: str):
    lines = path.read_text(encoding="utf-8").splitlines()
    prefix = SHARED_INFRASTRUCTURE_MAINTENANCE_SETTING + "="
    matches = [index for index, line in enumerate(lines) if line.startswith(prefix)]
    if len(matches) != 1 or lines[matches[0]] != prefix + expected:
        raise DeploymentError("Shared infrastructure maintenance intent is missing or does not match")
    lines[matches[0]] = prefix + "none"
    atomic_write(path, "\n".join(lines) + "\n")


def validate_reference(reference):
    if (not isinstance(reference, dict) or set(reference) != {"id", "name"}
            or not isinstance(reference["id"], str) or not re.fullmatch(r"[a-f0-9]{64}", reference["id"])
            or not isinstance(reference["name"], str) or not NAME.fullmatch(reference["name"])):
        raise DeploymentError("Invalid container identity in deployment journal")


def validate_edge(edge):
    if (not isinstance(edge, dict) or set(edge) != {"container", "master", "workers"}
            or not re.fullmatch(r"[a-f0-9]{64}", str(edge["container"]))
            or not isinstance(edge["workers"], list) or not edge["workers"]):
        raise DeploymentError("Invalid edge identities")
    for process in [edge["master"], *edge["workers"]]:
        if (not isinstance(process, dict) or set(process) != {"pid", "start"}
                or any(type(value) is not int or value <= 0 for value in process.values())):
            raise DeploymentError("Invalid Nginx process identity")


def validate_state(state):
    if (not isinstance(state, dict) or state.get("version") != 1 or state.get("phase") not in PHASES
            or not isinstance(state.get("authority_exposed"), bool)
            or not isinstance(state.get("cutover_armed"), bool)
            or not re.fullmatch(r"[a-f0-9]{32}", str(state.get("generation", "")))
            or not re.fullmatch(r"[a-f0-9]{64}", str(state.get("fingerprint", "")))):
        raise DeploymentError("Invalid deployment journal; preserving all containers")
    for key in ("original", "candidate", "canonical"):
        pair = state.get(key)
        if not isinstance(pair, dict) or set(pair) != {"api", "front"}:
            raise DeploymentError("Invalid pair in deployment journal")
        for reference in pair.values():
            if reference is not None:
                validate_reference(reference)
    if any(value is None for value in state["original"].values()):
        raise DeploymentError("Missing original pair identity")
    names = state.get("candidate_names", {})
    if set(names) != {"api", "front"} or any(not isinstance(value, str) or not NAME.fullmatch(value) for value in names.values()):
        raise DeploymentError("Invalid candidate names")
    for transition in (state.get("transition"), state.get("route")):
        if transition is None:
            continue
        if not isinstance(transition, dict) or set(transition) != {"edge", "target", "generation"}:
            raise DeploymentError("Invalid routing transition")
        validate_reference(transition["target"])
        if not re.fullmatch(r"[a-f0-9]{32}", str(transition["generation"])):
            raise DeploymentError("Invalid routing generation")
        validate_edge(transition["edge"])
    baseline = state.get("baseline")
    if (not isinstance(baseline, dict) or set(baseline) != {"edge", "marker", "routing_sha"}
            or not isinstance(baseline["marker"], str)
            or not re.fullmatch(r"[a-zA-Z0-9-]{0,128}", baseline["marker"])
            or (baseline["routing_sha"] is not None and not re.fullmatch(r"[a-f0-9]{64}", str(baseline["routing_sha"])))):
        raise DeploymentError("Invalid original routing proof")
    validate_edge(baseline["edge"])
    references = [reference for key in ("original", "candidate", "canonical")
                  for reference in state[key].values() if reference is not None]
    if len({reference["id"] for reference in references}) != len(references):
        raise DeploymentError("Deployment pairs must have disjoint container identities")
    if state["phase"] in {"prepared", "abandoning", "abandoned"} and (state["authority_exposed"] or state.get("transition") is not None):
        raise DeploymentError("An exposed authority cannot be classified as preparation")
    if state["phase"] not in {"prepared", "abandoning", "abandoned"} and not state["authority_exposed"]:
        raise DeploymentError("Inconsistent public authority in deployment journal")
    if state["phase"] not in {"prepared", "abandoning", "abandoned"} and any(value is None for value in state["candidate"].values()):
        raise DeploymentError("Missing candidate pair identity")
    if state["phase"] in {"switch-canonical", "cleanup", "complete"} and any(value is None for value in state["canonical"].values()):
        raise DeploymentError("Missing canonical pair identity")
    if state["phase"] in {"replace-canonical", "switch-canonical", "cleanup", "complete"} and state.get("route") is None:
        raise DeploymentError("Missing confirmed routing identity")


class DeploymentTransaction:
    def __init__(self, runtime, directory: Path):
        self.runtime = runtime
        self.directory = directory
        self.journal = directory / "deployment.json"
        self.routing = directory / "active.conf"
        self.state = None

    def read(self):
        if not self.journal.exists():
            return None
        try:
            state = json.loads(self.journal.read_text(encoding="utf-8"))
            validate_state(state)
        except (ValueError, KeyError, TypeError, OSError) as error:
            raise DeploymentError("Unreadable deployment journal; no cleanup is safe") from error
        return state

    def save(self):
        validate_state(self.state)
        atomic_write(self.journal, json.dumps(self.state, indent=2) + "\n")

    def assert_candidates_known(self):
        if self.state:
            for service in ("api", "front"):
                for key in ("original", "canonical"):
                    reference = self.state[key][service]
                    if reference is not None and reference["name"] != self.runtime.names[service]:
                        raise DeploymentError("Journal canonical role/name mismatch")
                expected_name = f"{self.runtime.project}-{service}-candidate-{self.state['generation'][:16]}"
                reference = self.state["candidate"][service]
                if self.state["candidate_names"][service] != expected_name or (reference is not None and reference["name"] != expected_name):
                    raise DeploymentError("Journal candidate role/name mismatch")
        expected = set(self.state["candidate_names"].values()) if self.state else set()
        actual = self.runtime.candidates()
        if len(actual) > 2 or any(reference["name"] not in expected for reference in actual):
            raise DeploymentError("Unknown deployment candidates; refusing allocation or cleanup")

    def prepare(self):
        self.state = self.read()
        self.runtime.preflight()
        self.assert_candidates_known()
        if self.state and self.state["phase"] not in {"complete", "abandoned"}:
            if self.state["fingerprint"] != self.runtime.fingerprint:
                raise DeploymentError("Pending deployment uses different configuration; recover the installed bundle before installing another")
            print(f"Resuming deployment in phase {self.state['phase']}", flush=True)
            return
        # No stale deletion. Even a completed journal does not license deleting
        # a candidate that appeared later or whose identity cannot be proven.
        if self.runtime.candidates():
            raise DeploymentError("Unexpected candidates after a completed deployment")
        original = {service: self.runtime.find(self.runtime.names[service]) for service in ("api", "front")}
        for reference in original.values():
            self.runtime.wait_healthy(reference)
        generation = uuid.uuid4().hex
        self.state = {"version": 1, "phase": "prepared", "fingerprint": self.runtime.fingerprint,
                      "generation": generation, "authority_exposed": False, "cutover_armed": False,
                      "original": original, "candidate": {"api": None, "front": None},
                      "canonical": {"api": None, "front": None}, "transition": None, "route": None,
                      "baseline": {"edge": self.runtime.edge_processes(), "marker": self.runtime.edge_generation(),
                                   "routing_sha": self.routing_hash()},
                      "candidate_names": {service: f"{self.runtime.project}-{service}-candidate-{generation[:16]}"
                                          for service in ("api", "front")}}
        self.save()

    def routing_hash(self):
        import hashlib
        return hashlib.sha256(self.routing.read_bytes()).hexdigest() if self.routing.exists() else None

    def quiesce_unexposed(self):
        self.state = self.read()
        if (self.state is None or self.state["phase"] not in {"prepared", "abandoning"}
                or self.state["authority_exposed"] or self.state["transition"] is not None):
            raise DeploymentError("Only an unexposed preparation can be quiesced")
        baseline = self.state["baseline"]
        self.same_edge(baseline["edge"], self.runtime.edge_processes())
        if self.runtime.edge_generation() != baseline["marker"] or self.routing_hash() != baseline["routing_sha"]:
            raise DeploymentError("Original route changed; unexposed cleanup cannot be proven")
        self.assert_candidates_known()
        for reference in self.state["original"].values():
            self.runtime.wait_healthy(reference)
        self.phase("abandoning")
        for service in ("front", "api"):
            reference = self.runtime.find(self.state["candidate_names"][service], self.state["generation"])
            if reference is not None:
                def record_exit(details):
                    self.state.setdefault("abandoned_exits", {})[reference["id"]] = details
                    self.save()
                self.runtime.stop_remove(reference, allow_failed_start=True, record_exit=record_exit)

    def abandon_unexposed(self):
        self.quiesce_unexposed()
        if self.state["cutover_armed"]:
            raise DeploymentError("Stopped candidates still require the interrupted business rollback before abandonment")
        self.phase("abandoned")
        print("Unexposed preparation abandoned; original pair retained", flush=True)

    @staticmethod
    def same_edge(expected, actual):
        if expected["container"] != actual["container"] or expected["master"] != actual["master"]:
            raise DeploymentError("Edge/master identity changed; drainage cannot be proven")

    def switch_and_drain(self, target):
        if self.state["transition"] is None:
            self.state["transition"] = {"edge": self.runtime.edge_processes(), "target": target,
                                        "generation": uuid.uuid4().hex}
            # This write also precedes reload on the very first rollout, when
            # existing workers still use the legacy front alias configuration.
            self.save()
        transition = self.state["transition"]
        if transition["target"] != target:
            raise DeploymentError("Pending route does not select the expected pair")
        self.same_edge(transition["edge"], self.runtime.edge_processes())
        content = (f"set $front_backend {target['name']}:4000;\n"
                   f"set $deployment_generation {transition['generation']};\n")
        atomic_write(self.routing, content, 0o644)
        self.runtime.reload_edge()
        deadline = time.monotonic() + self.runtime.drain_timeout
        while True:
            try:
                observed = self.runtime.edge_response()
            except (OSError, TimeoutError):
                observed = {"generation": None, "pid": None}
            # Snapshot AFTER the responding worker attests its configuration.
            # A worker respawned between the initial snapshot and reload is also
            # covered, even before it processes SIGQUIT. Retrying obtains a fresh
            # attestation if the new-generation worker itself respawns.
            current = self.runtime.edge_processes()
            self.same_edge(transition["edge"], current)
            if (observed["generation"] == transition["generation"] and len(current["workers"]) == 1
                    and current["workers"][0]["pid"] == observed["pid"]):
                self.state["route"] = transition
                print(f"Route {target['name']} confirmed; previous Nginx workers drained", flush=True)
                return
            if time.monotonic() >= deadline:
                raise DeploymentError("Drain deadline reached; both pairs are retained and deployment is not successful")
            time.sleep(1)

    def phase(self, name):
        self.state["phase"] = name
        self.state["transition"] = None
        self.save()

    def assert_active_route(self, target):
        route = self.state.get("route")
        if route is None or route["target"] != target:
            raise DeploymentError("No confirmed route protects container retirement")
        observed = self.runtime.edge_response()
        current = self.runtime.edge_processes()
        self.same_edge(route["edge"], current)
        expected = (f"set $front_backend {target['name']}:4000;\n"
                    f"set $deployment_generation {route['generation']};\n")
        if (self.routing.read_text(encoding="utf-8") != expected or observed["generation"] != route["generation"]
                or len(current["workers"]) != 1 or current["workers"][0]["pid"] != observed["pid"]):
            raise DeploymentError("Active routing changed; preserving both pairs")

    def execute(self):
        if self.state is None:
            self.state = self.read()
        if self.state is None or self.state["fingerprint"] != self.runtime.fingerprint:
            raise DeploymentError("Deployment must be prepared under the installation lock")
        if self.state["phase"] in {"abandoning", "abandoned"}:
            raise DeploymentError("Finish unexposed abandonment before preparing another deployment")
        self.assert_candidates_known()
        if self.state["phase"] == "prepared":
            for service in ("api", "front"):
                reference = self.runtime.start_candidate(service, self.state["candidate_names"][service],
                                                         self.state["generation"], self.state["candidate_names"]["api"])
                if self.state["candidate"][service] not in (None, reference):
                    raise DeploymentError("Candidate identity changed while preparing")
                self.state["candidate"][service] = reference
                self.save()
            self.runtime.verify_pair(self.state["candidate"], candidate=True)
            # Conservatively disarm business rollback BEFORE routing intent.
            # A lost reload response may already expose the new authority.
            self.state["authority_exposed"] = True
            self.phase("switch-candidate")
        if self.state["phase"] == "switch-candidate":
            self.runtime.verify_pair(self.state["candidate"], candidate=True)
            self.switch_and_drain(self.state["candidate"]["front"])
            self.phase("replace-canonical")
        if self.state["phase"] == "replace-canonical":
            self.runtime.verify_pair(self.state["candidate"], candidate=True)
            self.assert_active_route(self.state["candidate"]["front"])
            # IDs, never reusable names. A failed front stop leaves its API up.
            for service in ("front", "api"):
                self.runtime.stop_remove(self.state["original"][service])
            for service in ("api", "front"):
                reference = self.runtime.create_canonical(service, self.state["generation"] + "c")
                if self.state["canonical"][service] not in (None, reference):
                    raise DeploymentError("Canonical identity changed during recovery")
                self.state["canonical"][service] = reference
                self.save()
            self.runtime.verify_pair(self.state["canonical"], candidate=False)
            self.phase("switch-canonical")
        if self.state["phase"] == "switch-canonical":
            self.runtime.verify_pair(self.state["canonical"], candidate=False)
            self.switch_and_drain(self.state["canonical"]["front"])
            self.phase("cleanup")
        if self.state["phase"] == "cleanup":
            self.runtime.verify_pair(self.state["canonical"], candidate=False)
            self.assert_active_route(self.state["canonical"]["front"])
            for service in ("front", "api"):
                self.runtime.stop_remove(self.state["candidate"][service])
            self.phase("complete")
        self.runtime.verify_pair(self.state["canonical"], candidate=False)
        self.assert_active_route(self.state["canonical"]["front"])
        print("Deployment committed: canonical pair healthy, durable worker enabled", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("prepare", "deploy", "rollback-safe", "assert-complete",
                                            "arm-cutover", "cutover-pending", "cutover-restored", "abandon-unexposed",
                                            "quiesce-unexposed", "validate-journal", "maintain-mongodb"))
    args = parser.parse_args()
    directory = Path(__file__).resolve().parent.parent
    if args.command in {"prepare", "deploy", "arm-cutover", "cutover-restored", "abandon-unexposed", "quiesce-unexposed",
                        "maintain-mongodb"}:
        import fcntl
        try:
            lock_path = os.environ.get("DEPLOY_LOCK_FILE", "/tmp/amusementpark-deploy.lock")
            lock = os.fstat(9)
            expected = os.stat(lock_path)
            if (lock.st_dev, lock.st_ino) != (expected.st_dev, expected.st_ino):
                raise DeploymentError("Deployment lock identity mismatch")
            fcntl.flock(9, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError as error:
            raise DeploymentError("A held installation lock on FD9 is required before deployment mutations") from error
    # rollback-safe must not depend on Docker availability and never treats an
    # absent/corrupt journal as permission to revert a possibly active authority.
    if args.command in {"rollback-safe", "cutover-pending", "arm-cutover", "cutover-restored", "validate-journal"}:
        transaction = DeploymentTransaction(None, directory / "nginx/runtime")
        state = transaction.read()
        if args.command == "validate-journal":
            return 0 if state is not None else 1
        safe = state is not None and not state["authority_exposed"] and state["phase"] in {"prepared", "abandoning"}
        if args.command in {"arm-cutover", "cutover-restored"}:
            if not safe:
                raise DeploymentError("Cutover state cannot change after possible public exposure")
            transaction.state = state
            state["cutover_armed"] = args.command == "arm-cutover"
            transaction.save()
            return 0
        return 0 if safe and (args.command == "rollback-safe" or state["cutover_armed"]) else 1
    runtime = DockerRuntime(directory)
    if args.command == "maintain-mongodb":
        consume_shared_infrastructure_maintenance(directory / ".env", "mongodb")
        runtime.maintain_mongodb()
        return 0
    transaction = DeploymentTransaction(runtime, directory / "nginx/runtime")
    if args.command == "prepare":
        transaction.prepare()
    elif args.command == "deploy":
        transaction.execute()
    elif args.command == "abandon-unexposed":
        transaction.abandon_unexposed()
    elif args.command == "quiesce-unexposed":
        transaction.quiesce_unexposed()
    else:
        state = transaction.read()
        if state is None or state["phase"] != "complete" or runtime.candidates():
            raise DeploymentError("Deployment is incomplete; no generic candidate cleanup is permitted")
        runtime.verify_pair(state["canonical"], candidate=False)
        transaction.state = state
        transaction.assert_active_route(state["canonical"]["front"])
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (DeploymentError, OSError, ValueError) as error:
        print(f"Deployment stopped safely: {error}", file=__import__("sys").stderr)
        raise SystemExit(1)
