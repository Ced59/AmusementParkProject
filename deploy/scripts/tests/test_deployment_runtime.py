import json
from pathlib import Path
import sys
import unittest
from unittest.mock import Mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from deployment_runtime import DeploymentError, DockerRuntime


class DockerRuntimeInspectTests(unittest.TestCase):
    def setUp(self):
        self.runtime = DockerRuntime.__new__(DockerRuntime)
        self.runtime.run = Mock()

    def test_disappearing_named_container_returns_none(self):
        identifier = "test-front"
        self.runtime.run.side_effect = ["", "a" * 64,
                                        DeploymentError(f"Command docker inspect failed (1): error: no such object: {identifier}")]

        self.assertIsNone(self.runtime.inspect(identifier))
        self.assertEqual(self.runtime.run.call_count, 3)

    def test_disappearing_container_id_returns_none(self):
        identifier = "a" * 64
        self.runtime.run.side_effect = [identifier,
                                        DeploymentError(f"Command docker inspect failed (1): Error: No such object: {identifier}")]

        self.assertIsNone(self.runtime.inspect(identifier))
        self.assertEqual(self.runtime.run.call_count, 2)

    def test_other_inspect_failure_is_not_hidden(self):
        identifier = "test-api"
        self.runtime.run.side_effect = ["", "b" * 64,
                                        DeploymentError("Command docker inspect failed (1): Cannot connect to the Docker daemon")]

        with self.assertRaisesRegex(DeploymentError, "Cannot connect"):
            self.runtime.inspect(identifier)

    def test_existing_container_is_returned(self):
        identifier = "test-api"
        container = {"Id": "b" * 64, "Name": "/test-api"}
        self.runtime.run.side_effect = ["", "b" * 64, json.dumps([container])]

        self.assertEqual(self.runtime.inspect(identifier), container)
