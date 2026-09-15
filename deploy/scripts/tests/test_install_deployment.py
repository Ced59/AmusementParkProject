import importlib.util
import io
import os
from pathlib import Path
import subprocess
import sys
import tarfile
import tempfile
import time
import unittest
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("install_deployment", SCRIPTS / "install-deployment.py")
installer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)


def bundle(path, label, extra=None):
    entrypoint = '''#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
[ /proc/self/fd/9 -ef "$DEPLOY_LOCK_FILE" ] && flock -n 9
[ ! -e .installation-pending.json ]
source .env
printf '%s\\n' "$LABEL" >> executions
touch "$LABEL.started"
while [ ! -e "$LABEL.release" ]; do sleep 0.03; done
'''
    contents = {".env": f"LABEL={label}\n", "compose.prod.yml": "services: {}\n",
                "scripts/deploy.sh": entrypoint, "nginx/edge.conf": f"# {label}\n", **(extra or {})}
    with tarfile.open(path, "w:gz") as archive:
        for name, content in contents.items():
            data = content.encode()
            item = tarfile.TarInfo(name)
            item.size = len(data)
            item.mode = 0o644
            archive.addfile(item, io.BytesIO(data))
    return contents


def until(predicate, timeout=10):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if predicate():
            return
        time.sleep(0.03)
    raise AssertionError("Rendezvous timed out")


class InstallationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.target = self.directory / "active"
        self.archive = self.directory / "run-one.tgz"
        self.contents = bundle(self.archive, "one")

    def test_interrupted_file_replacement_leaves_guard_then_reinstall_completes(self):
        replace = os.replace

        def interrupt(source, destination):
            if Path(destination).name == ".env":
                raise OSError("injected interruption")
            replace(source, destination)

        with patch.object(installer.os, "replace", side_effect=interrupt):
            with self.assertRaisesRegex(OSError, "interruption"):
                installer.install_bundle(self.archive, self.target)
        self.assertTrue((self.target / ".installation-pending.json").exists())
        self.assertIn("installation-pending", (self.target / "scripts/deploy.sh").read_text())
        installer.install_bundle(self.archive, self.target)
        self.assertFalse((self.target / ".installation-pending.json").exists())
        for name, content in self.contents.items():
            self.assertEqual((self.target / name).read_text(), content)

    def test_runtime_state_and_traversal_are_rejected_before_active_writes(self):
        self.target.mkdir()
        sentinel = self.target / ".env"
        sentinel.write_text("old")
        for entry in ("nginx/runtime/active.conf", "../outside", "/absolute"):
            with self.subTest(entry=entry):
                bundle(self.archive, "bad", {entry: "bad"})
                with self.assertRaisesRegex(RuntimeError, "Unsupported"):
                    installer.install_bundle(self.archive, self.target)
                self.assertEqual(sentinel.read_text(), "old")
                self.assertFalse((self.target / ".installation-pending.json").exists())

    @unittest.skipUnless(sys.platform == "linux", "Real Linux flock is tested in CI")
    def test_two_real_installers_never_extract_or_read_new_env_before_lock(self):
        second_archive = self.directory / "run-two.tgz"
        bundle(second_archive, "two")
        environment = {**os.environ, "DEPLOY_LOCK_FILE": str(self.directory / "deploy.lock"),
                       "DEPLOY_LOCK_TIMEOUT_SECONDS": "10"}
        processes = []
        try:
            first = subprocess.Popen([sys.executable, str(SCRIPTS / "install-deployment.py"),
                                      str(self.archive), str(self.target)], env=environment)
            processes.append(first)
            until(lambda: (self.target / "one.started").exists())
            second = subprocess.Popen([sys.executable, str(SCRIPTS / "install-deployment.py"),
                                       str(second_archive), str(self.target)], env=environment)
            processes.append(second)
            time.sleep(0.2)
            self.assertIsNone(second.poll())
            self.assertEqual((self.target / ".env").read_text(), "LABEL=one\n")
            self.assertFalse((self.target / "two.started").exists())
            (self.target / "one.release").touch()
            self.assertEqual(first.wait(timeout=10), 0)
            until(lambda: (self.target / "two.started").exists())
            self.assertEqual((self.target / ".env").read_text(), "LABEL=two\n")
            (self.target / "two.release").touch()
            self.assertEqual(second.wait(timeout=10), 0)
            self.assertEqual((self.target / "executions").read_text().splitlines(), ["one", "two"])
            self.assertEqual((self.target / ".env").stat().st_mode & 0o777, 0o600)
        finally:
            for process in processes:
                if process.poll() is None:
                    process.kill()
                    process.wait(timeout=10)

    @unittest.skipUnless(sys.platform == "linux", "Installed recovery process is tested in CI")
    def test_exposed_journal_recovers_old_configuration_before_next_bundle_installation(self):
        import shutil
        sys.path.insert(0, str(SCRIPTS / "tests"))
        from test_deployment_transaction import RuntimeFake, Interrupted
        from deployment_transaction import DeploymentTransaction
        installer.install_bundle(self.archive, self.target)
        directory = self.target / "nginx/runtime"
        runtime = RuntimeFake(directory)
        transaction = DeploymentTransaction(runtime, directory)
        transaction.prepare()
        runtime.fail_at = 3
        with self.assertRaises(Interrupted):
            transaction.execute()
        pending = transaction.journal.read_bytes()
        runtime.fail_at = None
        transaction.execute()
        completed = self.target / "completed-fixture.json"
        completed.write_bytes(transaction.journal.read_bytes())
        transaction.journal.write_bytes(pending)
        for name in ("deployment_transaction.py", "deployment_runtime.py"):
            shutil.copy2(SCRIPTS / name, self.target / "scripts" / name)
        old_deploy = self.target / "scripts/deploy.sh"
        old_deploy.write_text("""#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
[ /proc/self/fd/9 -ef "$DEPLOY_LOCK_FILE" ] && flock -n 9
[ "$(cat .env)" = "LABEL=one" ]
printf 'recovered-old-config\\n' >> executions
cp completed-fixture.json nginx/runtime/deployment.json
""")
        second = self.directory / "run-two.tgz"
        bundle(second, "two")
        (self.target / "two.release").touch()
        result = subprocess.run([sys.executable, str(SCRIPTS / "install-deployment.py"), str(second), str(self.target)],
                                env={**os.environ, "DEPLOY_LOCK_FILE": str(self.directory / "lock")},
                                capture_output=True, text=True, timeout=15)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.target / "executions").read_text().splitlines(), ["recovered-old-config", "two"])
        self.assertEqual((self.target / ".env").read_text(), "LABEL=two\n")

    @unittest.skipUnless(sys.platform == "linux", "Real entrypoint/lock is tested in CI")
    def test_direct_entrypoint_refuses_incomplete_installation_before_env_read(self):
        scripts = self.target / "scripts"
        scripts.mkdir(parents=True)
        (scripts / "deploy.sh").write_bytes((SCRIPTS / "deploy.sh").read_bytes())
        (self.target / ".installation-pending.json").write_text("{}")
        result = subprocess.run(["bash", str(scripts / "deploy.sh")], capture_output=True, text=True,
                                env={**os.environ, "DEPLOY_LOCK_FILE": str(self.directory / "lock")})
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("installation is incomplete", result.stderr)
        self.assertNotIn("Missing .env", result.stderr)


if __name__ == "__main__":
    unittest.main()
