import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import MagicMock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import application_rollback as rollback
from deployment_runtime import DeploymentError


class ApplicationRollbackTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.directory = Path(temporary.name)
        self.runtime = MagicMock()
        self.runtime.project = "test"
        self.runtime.names = {service: "test-" + service for service in rollback.SERVICES}
        self.runtime.config = {"services": {
            service: {"image": "ghcr.io/test/" + service + ":" + "a" * 40,
                      "environment": {"SECRET": "never-in-checkpoint"}}
            for service in rollback.SERVICES}}
        self.runtime.candidates.return_value = []
        self.old = {service: "sha256:" + digit * 64 for service, digit in [("api", "1"), ("front", "2")]}
        self.new = {service: "sha256:" + digit * 64 for service, digit in [("api", "3"), ("front", "4")]}
        self.active = dict(self.old)

        def inspect(reference):
            service = reference.removeprefix("test-")
            return {"Image": self.active[service], "Config": {"Labels": {
                "com.docker.compose.project": "test", "com.docker.compose.service": service}}}

        def run(*arguments):
            if arguments[:3] == ("docker", "image", "inspect"):
                reference = arguments[3]
                if reference in [*self.old.values(), *self.new.values()]:
                    service = next(key for images in [self.old, self.new] for key, value in images.items() if value == reference)
                    return json.dumps([{"Id": reference, "RepoDigests": ["ghcr.io/test/" + service + "@" + reference]}])
                for service in rollback.SERVICES:
                    for images in (self.old, self.new):
                        if reference == "ghcr.io/test/" + service + "@" + images[service]:
                            return json.dumps([{"Id": images[service]}])
                    if reference == self.runtime.config["services"][service]["image"]:
                        return json.dumps([{"Id": self.new[service]}])
                raise AssertionError("Unexpected image " + reference)
            if arguments[:2] == ("docker", "pull"):
                return ""
            if arguments[:3] == ("docker", "manifest", "inspect"):
                return "{}"
            raise AssertionError(arguments)

        self.set_up_image_run = run
        self.runtime.run.side_effect = run
        self.runtime.find.side_effect = lambda name: {"id": name, "name": name}
        self.runtime.inspect.side_effect = inspect
        self.transaction = MagicMock(spec=rollback.DeploymentTransaction)
        self.transaction.execute.side_effect = lambda: setattr(self, "active", dict(self.old))
        self.transaction.read.return_value = {"phase": "complete", "canonical": {
            service: {"id": "test-" + service, "name": "test-" + service} for service in rollback.SERVICES}}
        factory = patch.object(rollback, "DeploymentTransaction", return_value=self.transaction)
        factory.start()
        self.addCleanup(factory.stop)
        environment = patch.dict(os.environ, {"SHARED_INFRASTRUCTURE_MAINTENANCE": "none"})
        environment.start()
        self.addCleanup(environment.stop)
        self.env = self.directory / ".env"
        self.env.write_text("# unchanged\nSECRET='literal $() value'\nAPI_IMAGE=new-api\nFRONT_IMAGE=new-front\n", encoding="utf-8")

    def capture(self):
        return rollback.capture(self.runtime, self.directory)

    def test_capture_pins_both_actual_previous_images_without_secrets(self):
        record = self.capture()
        self.assertEqual(set(record["previous"]), {"api", "front"})
        for service in rollback.SERVICES:
            self.assertEqual(record["previous"][service]["id"], self.old[service])
            self.assertEqual(record["previous"][service]["reference"], "ghcr.io/test/" + service + "@" + self.old[service])
        self.assertNotIn("SECRET", rollback.checkpoint_path(self.directory).read_text())
        self.assertNotIn("never-in-checkpoint", rollback.checkpoint_path(self.directory).read_text())
        self.assertEqual(rollback.read_checkpoint(self.directory), record)
        manifests = [call.args for call in self.runtime.run.call_args_list if call.args[:3] == ("docker", "manifest", "inspect")]
        self.assertEqual(len(manifests), 2)

    def test_configuration_hash_excludes_only_the_two_image_values(self):
        original = rollback.configuration_hash(self.runtime)
        self.runtime.config["services"]["api"]["image"] = "different-api"
        self.assertEqual(rollback.configuration_hash(self.runtime), original)
        self.runtime.config["services"]["api"]["environment"]["SECRET"] = "changed"
        self.assertNotEqual(rollback.configuration_hash(self.runtime), original)

    def test_retry_preserves_previous_pair_and_unknown_pending_transaction_refuses(self):
        record = self.capture()
        self.active = dict(self.new)
        self.assertEqual(self.capture(), record)
        self.transaction.read.return_value = {"phase": "switch-candidate"}
        self.assertEqual(self.capture(), record)
        self.runtime.config["services"]["api"]["image"] = "other"
        self.runtime.run.side_effect = None
        self.runtime.run.return_value = json.dumps([{"Id": "sha256:" + "9" * 64}])
        with self.assertRaisesRegex(DeploymentError, "Pending transaction"):
            self.capture()

    def test_unknown_candidates_and_wrong_ownership_refuse_before_checkpoint_write(self):
        self.runtime.candidates.return_value = ["unknown"]
        with self.assertRaisesRegex(DeploymentError, "Unknown candidates"):
            self.capture()
        self.assertFalse(rollback.checkpoint_path(self.directory).exists())
        self.runtime.candidates.return_value = []
        self.runtime.inspect.side_effect = None
        self.runtime.inspect.return_value = {"Config": {"Labels": {"com.docker.compose.project": "other"}}}
        with self.assertRaisesRegex(DeploymentError, "ownership"):
            self.capture()

    def test_production_images_without_matching_registry_digest_refuse(self):
        self.runtime.run.side_effect = None
        self.runtime.run.return_value = json.dumps([{"Id": self.old["api"], "RepoDigests": []}])
        with self.assertRaisesRegex(DeploymentError, r"matching=0, recorded=0, repositories=none"):
            rollback.image_identity(self.runtime, self.old["api"], "ghcr.io/test/api:tag")

    def test_digest_diagnostics_report_repository_mismatch_without_mutation(self):
        self.runtime.run.side_effect = None
        for digests, expected in [
                (["ghcr.io/other/api@" + self.old["api"]],
                 "matching=0, recorded=1, repositories=ghcr.io/other/api")]:
            with self.subTest(expected=expected):
                self.runtime.run.reset_mock()
                self.runtime.run.return_value = json.dumps([{"Id": self.old["api"], "RepoDigests": digests,
                                                            "Config": {"Env": ["SECRET=never-log-this"]}}])
                with self.assertRaises(DeploymentError) as caught:
                    rollback.image_identity(self.runtime, self.old["api"], "ghcr.io/test/api:tag")
                self.assertIn(expected, str(caught.exception))
                self.assertNotIn("never-log-this", str(caught.exception))
                self.runtime.run.assert_called_once_with("docker", "image", "inspect", self.old["api"])

    def test_equivalent_manifest_aliases_pin_the_same_running_image_deterministically(self):
        aliases = ["ghcr.io/test/api@sha256:" + digit * 64 for digit in "654321"]
        for order in (aliases, list(reversed(aliases))):
            with self.subTest(order=order):
                self.runtime.run.reset_mock()
                self.runtime.run.side_effect = [
                    json.dumps([{"Id": self.old["api"], "RepoDigests": order}]),
                    json.dumps([{"Id": self.old["api"]}]), "{}"]
                result = rollback.image_identity(self.runtime, self.old["api"], "ghcr.io/test/api:tag")
                self.assertEqual(result, {"reference": min(aliases), "id": self.old["api"]})
                self.assertEqual([call.args for call in self.runtime.run.call_args_list], [
                    ("docker", "image", "inspect", self.old["api"]),
                    ("docker", "image", "inspect", min(aliases)),
                    ("docker", "manifest", "inspect", min(aliases))])

    def test_mismatched_or_unavailable_digest_refuses_before_active_writes(self):
        for fault in ("identity", "registry"):
            with self.subTest(fault=fault):
                self.runtime.run.reset_mock()
                original_run = self.set_up_image_run
                digest = "ghcr.io/test/api@" + self.old["api"]

                def fail(*arguments):
                    if fault == "identity" and arguments == ("docker", "image", "inspect", digest):
                        return json.dumps([{"Id": self.new["api"]}])
                    if fault == "registry" and arguments == ("docker", "manifest", "inspect", digest):
                        raise DeploymentError("Registry unavailable")
                    return original_run(*arguments)

                self.runtime.run.side_effect = fail
                original = self.env.read_bytes()
                with self.assertRaises(DeploymentError):
                    self.capture()
                self.assertFalse(rollback.checkpoint_path(self.directory).exists())
                self.assertEqual(self.env.read_bytes(), original)
                self.transaction.prepare.assert_not_called()
                self.transaction.execute.assert_not_called()
                if fault == "identity":
                    self.assertFalse(any(call.args[:3] == ("docker", "manifest", "inspect")
                                         for call in self.runtime.run.call_args_list))

    def test_digest_diagnostics_are_bounded_and_reject_unsafe_repository_text(self):
        self.runtime.run.side_effect = None
        digests = ["ghcr.io/other/repo" + str(number) + "@" + self.old["api"] for number in range(8)]
        digests.extend(["SECRET=never-log-this\n@" + self.old["api"], "x" * 10000 + "@" + self.old["api"]])
        self.runtime.run.return_value = json.dumps([{"Id": self.old["api"], "RepoDigests": digests}])
        with self.assertRaises(DeploymentError) as caught:
            rollback.image_identity(self.runtime, self.old["api"], "ghcr.io/test/api:tag")
        message = str(caught.exception)
        self.assertIn("matching=0, recorded=10", message)
        self.assertIn("ghcr.io/other/repo3", message)
        self.assertNotIn("ghcr.io/other/repo4", message)
        self.assertNotIn("SECRET", message)
        self.assertNotIn("\n", message)
        self.assertLess(len(message), 1000)

    def test_capture_failure_identifies_service_and_preserves_checkpoint_and_environment(self):
        original = self.env.read_bytes()
        with patch.object(rollback, "image_identity", side_effect=DeploymentError("matching=2")):
            with self.assertRaisesRegex(DeploymentError, "capture for api: matching=2"):
                self.capture()
        self.assertFalse(rollback.checkpoint_path(self.directory).exists())
        self.assertEqual(self.env.read_bytes(), original)
        self.transaction.prepare.assert_not_called()
        self.transaction.execute.assert_not_called()

    def test_exact_approval_and_unchanged_configuration_are_required_before_any_write(self):
        record = self.capture()
        original = self.env.read_bytes()
        with self.assertRaisesRegex(DeploymentError, "Exact application"):
            rollback.restore(self.runtime, self.directory, "0" * 64)
        self.runtime.config["services"]["api"]["environment"]["SECRET"] = "changed"
        with self.assertRaisesRegex(DeploymentError, "Configuration changed"):
            rollback.restore(self.runtime, self.directory, record["id"])
        self.assertEqual(self.env.read_bytes(), original)
        self.transaction.execute.assert_not_called()

    def test_forward_transaction_and_shared_maintenance_cannot_be_bypassed(self):
        record = self.capture()
        self.transaction.read.return_value = {"phase": "cleanup"}
        with self.assertRaisesRegex(DeploymentError, "Finish the installed"):
            rollback.restore(self.runtime, self.directory, record["id"])
        os.environ["SHARED_INFRASTRUCTURE_MAINTENANCE"] = "mongodb"
        with self.assertRaisesRegex(DeploymentError, "shared infrastructure"):
            rollback.restore(self.runtime, self.directory, record["id"])
        self.transaction.execute.assert_not_called()

    def test_restore_changes_only_both_image_settings_and_reuses_the_transaction(self):
        record = self.capture()
        self.active = dict(self.new)
        with patch.object(rollback, "DockerRuntime", return_value=self.runtime):
            result = rollback.restore(self.runtime, self.directory, record["id"])
        self.assertEqual(result["phase"], "restored")
        self.assertEqual(result["id"], record["id"])
        self.assertTrue(self.env.read_text().startswith("# unchanged\nSECRET='literal $() value'\n"))
        for service in rollback.SERVICES:
            self.assertIn(service.upper() + "_IMAGE=" + record["previous"][service]["reference"], self.env.read_text())
        self.transaction.prepare.assert_called_once()
        self.transaction.execute.assert_called_once()
        pulls = [call.args for call in self.runtime.run.call_args_list if call.args[:2] == ("docker", "pull")]
        self.assertEqual(len(pulls), 2)
        self.assertTrue(all("ghcr.io/test/api@sha256:" in call[2] or "ghcr.io/test/front@sha256:" in call[2] for call in pulls))

    def test_interruption_persists_intent_blocks_forward_install_and_can_resume(self):
        record = self.capture()
        self.active = dict(self.new)
        self.transaction.execute.side_effect = RuntimeError("interrupted")
        with patch.object(rollback, "DockerRuntime", return_value=self.runtime):
            with self.assertRaisesRegex(RuntimeError, "interrupted"):
                rollback.restore(self.runtime, self.directory, record["id"])
        self.assertEqual(rollback.read_checkpoint(self.directory)["phase"], "restoring")
        with self.assertRaisesRegex(DeploymentError, "Resume the exact"):
            self.capture()
        self.transaction.execute.side_effect = lambda: setattr(self, "active", dict(self.old))
        with patch.object(rollback, "DockerRuntime", return_value=self.runtime):
            self.assertEqual(rollback.restore(self.runtime, self.directory, record["id"])["phase"], "restored")

    def test_missing_or_changed_previous_image_refuses_before_active_write(self):
        record = self.capture()
        self.active = dict(self.new)
        original = self.env.read_bytes()
        previous_run = self.runtime.run.side_effect

        def changed(*arguments):
            if arguments[:2] == ("docker", "pull"):
                raise DeploymentError("Registry unavailable")
            return previous_run(*arguments)

        self.runtime.run.side_effect = changed
        with patch.object(rollback, "DockerRuntime", return_value=self.runtime):
            with self.assertRaisesRegex(DeploymentError, "Registry unavailable"):
                rollback.restore(self.runtime, self.directory, record["id"])
        self.assertEqual(self.env.read_bytes(), original)
        self.assertEqual(rollback.read_checkpoint(self.directory)["phase"], "ready")
        self.transaction.execute.assert_not_called()

    def test_interrupted_rollback_never_reinterprets_another_pending_transaction(self):
        record = self.capture()
        record["phase"] = "restoring"
        rollback.save_checkpoint(self.directory, record)
        self.transaction.read.return_value = {"phase": "switch-candidate", "fingerprint": "other"}
        original = self.env.read_bytes()
        with patch.object(rollback, "DockerRuntime", return_value=self.runtime):
            with self.assertRaisesRegex(DeploymentError, "Another transaction"):
                rollback.restore(self.runtime, self.directory, record["id"])
        self.assertEqual(self.env.read_bytes(), original)
        self.transaction.execute.assert_not_called()

    def test_workflow_keeps_fresh_master_and_serial_production_guards(self):
        workflow = (Path(__file__).resolve().parents[3] / ".github/workflows/production.yml").read_text(encoding="utf-8")
        self.assertIn("group: production-deploy-master", workflow)
        self.assertIn("cancel-in-progress: false", workflow)
        self.assertIn("github.ref_name == 'main' || github.ref_name == 'master'", workflow)
        self.assertIn('if [ "${GITHUB_SHA}" != "${latest_sha}" ]', workflow)
        self.assertIn('[[ "${APPLICATION_ROLLBACK_CHECKPOINT}" =~ ^[0-9a-f]{64}$ ]]', workflow)
        self.assertIn('Application rollback cannot maintain shared infrastructure.', workflow)
        self.assertEqual(workflow.count("&& (inputs.application_rollback_checkpoint || '') == ''"), 3)
        restore_branch = workflow.split('if [ -n "${APPLICATION_ROLLBACK_CHECKPOINT}" ]; then')[-1].split('exit 0')[0]
        self.assertIn("application_rollback.py' restore --expect", restore_branch)
        self.assertNotIn("install-deployment", restore_branch)
        source = (Path(__file__).resolve().parents[1] / "application_rollback.py").read_text()
        self.assertNotIn('"mongosh"', source)
        self.assertNotIn('"compose", "up"', source)
        deploy = (Path(__file__).resolve().parents[1] / "deploy.sh").read_text()
        self.assertLess(deploy.index("application_rollback.py assert-forward-safe"), deploy.index("load_env_file .env"))

    def test_corrupt_or_incomplete_checkpoint_and_duplicate_image_setting_refuse(self):
        record = self.capture()
        for transform in [lambda value: value["previous"].pop("api"),
                          lambda value: value.update(id="0" * 64),
                          lambda value: value["previous"]["api"].update(reference="ghcr.io/other/api@" + self.old["api"])]:
            changed = copy.deepcopy(record)
            transform(changed)
            if changed["id"] != "0" * 64:
                changed["id"] = rollback.identifier(changed)
            rollback.checkpoint_path(self.directory).write_text(json.dumps(changed))
            with self.assertRaises(DeploymentError):
                rollback.read_checkpoint(self.directory)
        original = self.env.read_text()
        self.env.write_text(original + "API_IMAGE=duplicate\n")
        with self.assertRaisesRegex(DeploymentError, "exactly one"):
            rollback.replace_application_images(self.env, record["previous"])
        self.assertEqual(self.env.read_text(), original + "API_IMAGE=duplicate\n")

    def test_environment_is_literal_and_duplicates_fail_without_executing_shell(self):
        with patch.dict(os.environ, {}, clear=True):
            values = rollback.load_environment(self.env)
            self.assertEqual(values["SECRET"], "literal $() value")
            self.env.write_text("API_IMAGE=one\nAPI_IMAGE=two\n")
            with self.assertRaisesRegex(DeploymentError, "duplicate"):
                rollback.load_environment(self.env)


if __name__ == "__main__":
    unittest.main()
