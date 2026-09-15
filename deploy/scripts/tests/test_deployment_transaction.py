import copy
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from deployment_runtime import DeploymentError, DockerRuntime, validate_single_worker_configuration
from deployment_transaction import DeploymentTransaction, validate_state


class Interrupted(RuntimeError):
    pass


class RuntimeFake:
    project = "test"
    names = {"front": "front", "api": "api", "edge": "edge"}
    fingerprint = "a" * 64
    drain_timeout = 0

    def __init__(self, directory):
        self.directory = directory
        self.next_id = 1
        self.containers = {}
        self.events = []
        self.fail_at = None
        self.block_drain = False
        self.edge = {"container": "e" * 64, "master": {"pid": 1, "start": 100},
                     "workers": [{"pid": 2, "start": 101}]}
        self.marker = "bootstrap"
        self.active_pid = 2
        for service in ("api", "front"):
            self.add(service, "old")

    def add(self, name, generation):
        reference = {"id": f"{self.next_id:064x}", "name": name}
        self.next_id += 1
        self.containers[name] = {"reference": reference, "generation": generation}
        self.assert_bound()
        return reference

    def event(self, value):
        self.events.append(value)
        if len(self.events) == self.fail_at:
            raise Interrupted(value)

    def assert_bound(self):
        assert len(self.containers) <= 4, "A third pair was created"

    def candidates(self):
        return [entry["reference"] for name, entry in self.containers.items() if "candidate" in name]

    def preflight(self):
        pass

    def find(self, name, generation=None):
        entry = self.containers.get(name)
        if entry and generation is not None and entry["generation"] != generation:
            raise DeploymentError("Generation mismatch")
        return entry["reference"] if entry else None

    def wait_healthy(self, reference):
        if reference is None or self.find(reference["name"]) != reference:
            raise DeploymentError("Missing identity")

    def start_candidate(self, service, name, generation, api_name):
        existing = self.find(name, generation)
        if existing is None:
            existing = self.add(name, generation)
            self.event("start-" + service)
        return existing

    def create_canonical(self, service, generation):
        existing = self.find(service, generation)
        if existing is None:
            existing = self.add(service, generation)
            self.event("canonical-" + service)
        return existing

    def verify_pair(self, pair, candidate):
        for reference in pair.values():
            self.wait_healthy(reference)
            assert ("candidate" in reference["name"]) == candidate

    def stop_remove(self, reference, allow_failed_start=False, record_exit=None):
        current = self.find(reference["name"])
        if current is None or current["id"] != reference["id"]:
            return  # Old canonical IDs must never remove their replacements.
        self.event("before-stop-" + reference["name"])
        if record_exit is not None:
            record_exit({"exit_code": 0, "oom_killed": False})
        del self.containers[reference["name"]]
        self.event("removed-" + reference["name"])

    def edge_processes(self):
        return copy.deepcopy(self.edge)

    def reload_edge(self):
        config = (self.directory / "active.conf").read_text()
        self.marker = config.split("$deployment_generation ")[1].split(";")[0]
        self.active_pid = self.edge["workers"][-1]["pid"] + 1
        active = {"pid": self.active_pid, "start": 200}
        self.edge["workers"] = [*self.edge["workers"], active] if self.block_drain else [active]
        self.event("reload")

    def edge_generation(self):
        return self.marker

    def edge_response(self):
        return {"generation": self.marker, "pid": self.active_pid}


class DeploymentTransactionTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.runtime = RuntimeFake(self.directory)
        self.transaction = DeploymentTransaction(self.runtime, self.directory)

    def test_commits_only_after_canonical_pair_and_second_drain(self):
        self.transaction.prepare()
        self.transaction.execute()
        self.assertEqual(self.transaction.read()["phase"], "complete")
        self.assertEqual(set(self.runtime.containers), {"api", "front"})
        events = self.runtime.events
        self.assertLess(events.index("reload"), events.index("before-stop-front"))
        self.assertLess(events.index("removed-front"), events.index("before-stop-api"))
        self.assertLess(events.index("removed-api"), events.index("canonical-api"))
        self.assertGreater(events.index("canonical-front"), events.index("canonical-api"))

    def test_recovers_every_mutation_boundary_without_third_pair(self):
        self.transaction.prepare()
        self.transaction.execute()
        mutation_count = len(self.runtime.events)
        for fail_at in range(1, mutation_count + 1):
            with self.subTest(fail_at=fail_at), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary)
                runtime = RuntimeFake(directory)
                transaction = DeploymentTransaction(runtime, directory)
                transaction.prepare()
                runtime.fail_at = fail_at
                with self.assertRaises(Interrupted):
                    transaction.execute()
                runtime.fail_at = None
                recovered = DeploymentTransaction(runtime, directory)
                recovered.prepare()
                recovered.execute()
                self.assertEqual(recovered.read()["phase"], "complete")
                self.assertEqual(set(runtime.containers), {"api", "front"})

    def test_slow_old_worker_retains_both_pairs_then_resumes(self):
        self.transaction.prepare()
        self.runtime.block_drain = True
        with self.assertRaisesRegex(DeploymentError, "Drain deadline"):
            self.transaction.execute()
        state = self.transaction.read()
        self.assertTrue(state["authority_exposed"])
        self.assertEqual(state["phase"], "switch-candidate")
        self.assertEqual(len(self.runtime.containers), 4)
        self.assertFalse(any(event.startswith("before-stop") for event in self.runtime.events))
        self.runtime.block_drain = False
        self.transaction.execute()
        self.assertEqual(self.transaction.read()["phase"], "complete")

    def test_front_stop_failure_preserves_its_api_and_selected_candidate(self):
        self.transaction.prepare()
        self.runtime.fail_at = 4  # first before-stop, after the first reload
        with self.assertRaises(Interrupted):
            self.transaction.execute()
        self.assertEqual(self.transaction.read()["phase"], "replace-canonical")
        self.assertEqual(len(self.runtime.containers), 4)
        self.assertIsNotNone(self.runtime.find("api"))

    def test_active_candidate_and_partial_canonical_api_resume_exact_ids(self):
        self.transaction.prepare()
        self.runtime.fail_at = 8  # canonical API created, response lost
        with self.assertRaises(Interrupted):
            self.transaction.execute()
        api = self.runtime.find("api")
        self.assertIsNotNone(api)
        self.assertIsNone(self.runtime.find("front"))
        self.assertEqual(len(self.runtime.candidates()), 2)
        self.runtime.fail_at = None
        recovered = DeploymentTransaction(self.runtime, self.directory)
        recovered.prepare()
        recovered.execute()
        self.assertEqual(self.runtime.find("api"), api)
        self.assertEqual(self.runtime.events.count("canonical-api"), 1)

    def test_changed_edge_master_on_resume_never_retires_a_pair(self):
        self.transaction.prepare()
        self.runtime.block_drain = True
        with self.assertRaises(DeploymentError):
            self.transaction.execute()
        self.runtime.edge["master"]["start"] += 1
        with self.assertRaisesRegex(DeploymentError, "identity changed"):
            self.transaction.execute()
        self.assertEqual(len(self.runtime.containers), 4)

    def test_unknown_legacy_candidate_blocks_prepare(self):
        self.runtime.add("test-api-candidate-old", "unknown")
        with self.assertRaisesRegex(DeploymentError, "Unknown deployment candidates"):
            self.transaction.prepare()
        self.assertFalse(self.runtime.events)

    def test_corrupt_journal_cannot_grant_cleanup_or_rollback(self):
        self.transaction.journal.write_text('{"phase":"complete"}')
        with self.assertRaises(DeploymentError):
            self.transaction.prepare()
        self.assertFalse(self.runtime.events)

    def test_different_bundle_cannot_overwrite_pending_recovery(self):
        self.transaction.prepare()
        self.runtime.fingerprint = "b" * 64
        with self.assertRaisesRegex(DeploymentError, "different configuration"):
            self.transaction.prepare()
        self.assertFalse(self.runtime.events)

    def test_intent_is_persisted_before_reload_can_expose_new_authority(self):
        self.transaction.prepare()
        self.runtime.fail_at = 3  # reload applied, response lost
        with self.assertRaises(Interrupted):
            self.transaction.execute()
        state = self.transaction.read()
        self.assertTrue(state["authority_exposed"])
        self.assertIsNotNone(state["transition"])
        validate_state(state)


    def test_worker_respawn_missing_from_initial_snapshot_still_blocks_retirement(self):
        self.transaction.prepare()
        original_reload = self.runtime.reload_edge
        def respawn_before_reload():
            # Worker 2 died; worker 50 was born after the journal snapshot and
            # holds an A request while the B worker can already answer /healthz.
            self.runtime.edge["workers"] = [{"pid": 50, "start": 150}]
            self.runtime.block_drain = True
            original_reload()
        self.runtime.reload_edge = respawn_before_reload
        with self.assertRaisesRegex(DeploymentError, "Drain deadline"):
            self.transaction.execute()
        self.assertFalse(any(event.startswith("before-stop") for event in self.runtime.events))
        self.assertEqual(len(self.runtime.containers), 4)

    def test_all_pairs_require_distinct_ids_before_any_retirement(self):
        self.transaction.prepare()
        self.transaction.state["candidate"]["front"] = self.transaction.state["original"]["front"]
        with self.assertRaisesRegex(DeploymentError, "disjoint"):
            self.transaction.save()

    def test_unexposed_abandon_stops_candidates_before_business_rollback_is_allowed_to_finish(self):
        self.transaction.prepare()
        self.transaction.state["cutover_armed"] = True
        self.transaction.save()
        self.runtime.fail_at = 2
        with self.assertRaises(Interrupted):
            self.transaction.execute()
        self.runtime.fail_at = None
        self.transaction.quiesce_unexposed()
        self.assertFalse(self.runtime.candidates())
        self.assertEqual(set(self.runtime.containers), {"front", "api"})
        self.assertTrue(self.transaction.read()["cutover_armed"])
        with self.assertRaisesRegex(DeploymentError, "business rollback"):
            self.transaction.abandon_unexposed()
        # Equivalent to successful Mongo rollback followed by cutover-restored.
        self.transaction.state["cutover_armed"] = False
        self.transaction.save()
        self.transaction.abandon_unexposed()
        self.assertEqual(self.transaction.read()["phase"], "abandoned")
        self.assertEqual(len(self.transaction.read()["abandoned_exits"]), 2)

    def test_changed_baseline_forbids_abandon_and_rollback_quiescence(self):
        self.transaction.prepare()
        self.runtime.marker = "unexpected"
        with self.assertRaisesRegex(DeploymentError, "Original route changed"):
            self.transaction.quiesce_unexposed()
        self.assertFalse(self.runtime.events)

    def test_worker_configuration_precondition_is_explicit(self):
        validate_single_worker_configuration("worker_processes 1;\n")
        for value in ("worker_processes 2;", "worker_processes auto;", "", "worker_processes 1;\nworker_processes 1;"):
            with self.subTest(value=value), self.assertRaises(DeploymentError):
                validate_single_worker_configuration(value)


class CandidateConfigurationTests(unittest.TestCase):
    def test_custom_allowed_hosts_also_allow_the_exact_canonical_api(self):
        config = {"services": {service: {"container_name": service} for service in ("api", "front", "edge")}}
        with patch.dict(__import__("os").environ, {"ALLOWED_HOSTS": "public.test", "SSR_INTERNAL_BASE_URL": "http://edge:4000"}, clear=True):
            with patch.object(DockerRuntime, "compose", return_value=json.dumps(config)):
                runtime = DockerRuntime(Path("."))
        self.assertEqual(runtime.environment["ALLOWED_HOSTS"], "public.test;api")
        self.assertEqual(runtime.environment["FRONT_SSR_API_INTERNAL_URL"], "http://api:8080")

    def test_candidate_commands_keep_unique_pair_and_disable_only_candidate_worker(self):
        runtime = object.__new__(DockerRuntime)
        runtime.environment = {"ALLOWED_HOSTS": "public.test;localhost"}
        calls = []
        current = {}

        def compose(*args, **kwargs):
            calls.append((args, kwargs))
            name = args[args.index("--name") + 1]
            current[name] = {"id": "a" * 64, "name": name}

        runtime.compose = compose
        runtime.find = lambda name, generation: current.get(name)
        runtime.wait_healthy = lambda reference: None
        runtime.start_candidate("api", "api-candidate-one", "generation", "api-candidate-one")
        runtime.start_candidate("front", "front-candidate-one", "generation", "api-candidate-one")
        self.assertIn("DurableBackgroundJobs__Worker__Enabled=false", calls[0][0])
        self.assertIn("MongoDB__CompleteFactualEventMigrationsOnStartup=false", calls[0][0])
        self.assertIn("AllowedHosts=public.test;localhost;api-candidate-one", calls[0][0])
        self.assertIn("SSR_API_INTERNAL_URL=http://api-candidate-one:8080", calls[1][0])
        self.assertNotIn("--use-aliases", calls[0][0])
        self.assertNotIn("--use-aliases", calls[1][0])


class EdgeReloadTests(unittest.TestCase):
    def runtime(self, fail_validation=False):
        runtime = object.__new__(DockerRuntime)
        runtime.names = {"edge": "edge"}
        runtime.find = lambda name: {"id": "e" * 64, "name": name}
        calls = []
        def run(*args):
            calls.append(args)
            if fail_validation and "-t" in args:
                raise DeploymentError("Invalid edge configuration")
            return ""
        runtime.run = run
        return runtime, calls

    def test_configuration_is_validated_before_graceful_reload_of_same_edge(self):
        runtime, calls = self.runtime()
        runtime.reload_edge()
        prefix = ("docker", "exec", "e" * 64, "nginx")
        config = ("-c", "/etc/nginx/amusementpark/edge.conf")
        self.assertEqual(calls, [prefix + ("-t",) + config, prefix + ("-s", "reload") + config])

    def test_invalid_configuration_never_signals_reload(self):
        runtime, calls = self.runtime(fail_validation=True)
        with self.assertRaisesRegex(DeploymentError, "Invalid edge configuration"):
            runtime.reload_edge()
        self.assertEqual(len(calls), 1)
        self.assertNotIn("reload", calls[0])


class StopPolicyTests(unittest.TestCase):
    def runtime(self, exit_code, running=True, generation="new", oom=False):
        runtime = object.__new__(DockerRuntime)
        runtime.project = "test"
        runtime.stop_timeout = 1
        container = {"Id": "a" * 64, "Name": "/front", "Config": {"Labels": {
            "com.docker.compose.project": "test", "com.docker.compose.service": "front",
            "fun.amusement-parks.deployment-generation": generation}},
            "State": {"Running": running, "ExitCode": exit_code, "OOMKilled": oom}}
        calls = []
        def run(*args, **kwargs):
            calls.append(args)
            if args[1] == "stop":
                container["State"]["Running"] = False
            return ""
        runtime.inspect = lambda identifier: copy.deepcopy(container)
        runtime.run = run
        return runtime, calls

    def test_forced_or_failed_normal_stop_never_removes_the_front(self):
        for code, oom in ((1, False), (137, False), (0, True)):
            with self.subTest(code=code, oom=oom):
                runtime, calls = self.runtime(code, oom=oom)
                with self.assertRaisesRegex(DeploymentError, "Abnormal stop"):
                    runtime.stop_remove({"id": "a" * 64, "name": "front"})
                self.assertFalse(any(call[1] == "rm" for call in calls))

    def test_first_rollout_accepts_old_node_sigterm_exit_after_public_drain(self):
        runtime, calls = self.runtime(143, generation="unmanaged")
        runtime.stop_remove({"id": "a" * 64, "name": "front"})
        self.assertEqual(calls[-1][1], "rm")

    def test_already_failed_unexposed_start_can_be_removed_with_recorded_cause(self):
        runtime, calls = self.runtime(1, running=False)
        recorded = []
        runtime.stop_remove({"id": "a" * 64, "name": "front"}, allow_failed_start=True, record_exit=recorded.append)
        self.assertEqual(recorded, [{"exit_code": 1, "oom_killed": False}])
        self.assertEqual(calls[-1][1], "rm")

    def test_unexposed_permission_does_not_hide_a_new_shutdown_failure(self):
        runtime, calls = self.runtime(1, running=True)
        with self.assertRaises(DeploymentError):
            runtime.stop_remove({"id": "a" * 64, "name": "front"}, allow_failed_start=True)
        self.assertFalse(any(call[1] == "rm" for call in calls))


if __name__ == "__main__":
    unittest.main()
