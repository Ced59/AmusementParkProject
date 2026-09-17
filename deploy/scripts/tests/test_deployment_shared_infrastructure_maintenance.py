import unittest
from unittest.mock import Mock, call
from pathlib import Path
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from deployment_runtime import DeploymentError, DockerRuntime
from deployment_transaction import consume_shared_infrastructure_maintenance


class SharedInfrastructureMaintenanceTests(unittest.TestCase):
    @staticmethod
    def container(identifier, volume="amusementpark_mongodb_data"):
        return {
            "Id": identifier,
            "Name": "/amusementpark-mongodb",
            "Config": {
                "Image": "mongo:8.0",
                "Labels": {
                    "com.docker.compose.project": "amusementpark",
                    "com.docker.compose.service": "mongodb",
                    "com.docker.compose.config-hash": "stable-config",
                },
            },
            "Mounts": [
                {
                    "Type": "volume",
                    "Name": volume,
                    "Destination": "/data/db",
                },
            ],
        }

    @staticmethod
    def runtime():
        runtime = DockerRuntime.__new__(DockerRuntime)
        runtime.project = "amusementpark"
        runtime.config = {
            "services": {
                "mongodb": {
                    "image": "mongo:8.0",
                    "volumes": [
                        {
                            "type": "volume",
                            "source": "mongodb_data",
                            "target": "/data/db",
                        },
                    ],
                },
            },
            "volumes": {
                "mongodb_data": {
                    "name": "amusementpark_mongodb_data",
                },
            },
        }
        runtime.compose = Mock()
        runtime.inspect = Mock()
        runtime.wait_healthy = Mock()
        return runtime

    def test_recreates_only_mongodb_and_retains_the_named_data_volume(self):
        runtime = self.runtime()
        before = self.container("a" * 64)
        after = self.container("b" * 64)
        runtime.compose.side_effect = [before["Id"], "mongodb stable-config", "", after["Id"]]
        runtime.inspect.side_effect = [before, after]

        runtime.maintain_mongodb()

        self.assertEqual(runtime.compose.call_args_list, [
            call("ps", "-q", "mongodb"),
            call("config", "--hash", "mongodb"),
            call("up", "-d", "--no-deps", "mongodb"),
            call("ps", "-q", "mongodb"),
        ])
        runtime.wait_healthy.assert_called_once_with({
            "id": after["Id"],
            "name": "amusementpark-mongodb",
        })

    def test_refuses_a_changed_data_volume_after_recreation(self):
        runtime = self.runtime()
        before = self.container("a" * 64)
        after = self.container("b" * 64, "unexpected")
        runtime.compose.side_effect = [before["Id"], "mongodb stable-config", "", after["Id"]]
        runtime.inspect.side_effect = [before, after]

        with self.assertRaisesRegex(DeploymentError, "volume identity changed"):
            runtime.maintain_mongodb()

        runtime.wait_healthy.assert_not_called()

    def test_refuses_a_different_declared_major_before_touching_the_container(self):
        runtime = self.runtime()
        runtime.config["services"]["mongodb"]["image"] = "mongo:9.0"

        with self.assertRaisesRegex(DeploymentError, "declared floating 8.0 image"):
            runtime.maintain_mongodb()

        runtime.compose.assert_not_called()
        runtime.inspect.assert_not_called()

    def test_refuses_to_bootstrap_a_missing_database(self):
        runtime = self.runtime()
        runtime.compose.return_value = ""

        with self.assertRaisesRegex(DeploymentError, "cannot bootstrap"):
            runtime.maintain_mongodb()

        runtime.inspect.assert_not_called()
        runtime.wait_healthy.assert_not_called()

    def test_refuses_non_image_configuration_drift_before_recreation(self):
        runtime = self.runtime()
        before = self.container("a" * 64)
        runtime.compose.side_effect = [before["Id"], "mongodb changed-config"]
        runtime.inspect.return_value = before

        with self.assertRaisesRegex(DeploymentError, "non-image configuration changed"):
            runtime.maintain_mongodb()

        self.assertEqual(runtime.compose.call_args_list, [
            call("ps", "-q", "mongodb"),
            call("config", "--hash", "mongodb"),
        ])
        runtime.wait_healthy.assert_not_called()

    def test_refuses_a_container_owned_by_another_compose_project(self):
        runtime = self.runtime()
        container = self.container("a" * 64)
        container["Config"]["Labels"]["com.docker.compose.project"] = "other"
        runtime.compose.return_value = container["Id"]
        runtime.inspect.return_value = container

        with self.assertRaisesRegex(DeploymentError, "belongs to another project"):
            runtime.maintain_mongodb()

        runtime.wait_healthy.assert_not_called()

    def test_consumes_the_manual_intent_without_changing_other_environment_values(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / ".env"
            path.write_text(
                "SECRET=value-with-maintenance-word\n"
                "SHARED_INFRASTRUCTURE_MAINTENANCE=mongodb\n"
                "OTHER=value\n",
                encoding="utf-8")

            consume_shared_infrastructure_maintenance(path, "mongodb")

            self.assertEqual(
                path.read_text(encoding="utf-8"),
                "SECRET=value-with-maintenance-word\n"
                "SHARED_INFRASTRUCTURE_MAINTENANCE=none\n"
                "OTHER=value\n")

    def test_refuses_to_consume_a_missing_or_already_consumed_intent(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / ".env"
            path.write_text(
                "SHARED_INFRASTRUCTURE_MAINTENANCE=none\n",
                encoding="utf-8")

            with self.assertRaisesRegex(DeploymentError, "missing or does not match"):
                consume_shared_infrastructure_maintenance(path, "mongodb")


if __name__ == "__main__":
    unittest.main()
