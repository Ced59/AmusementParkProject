#!/usr/bin/env python3
"""Install a run-specific bundle only while holding the deployment lock.

Executed from the uploaded immutable installer, never from the active scripts.
FD 9 and its flock are inherited by deploy.sh; no second lock owner is created.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import tarfile
import tempfile
import time


def digest_file(path: Path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def sync_directory(path: Path):
    if os.name == "posix":
        descriptor = os.open(path, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(descriptor)
        finally:
            os.close(descriptor)


def mark_installation(target: Path, archive: Path):
    descriptor, temporary = tempfile.mkstemp(dir=target)
    with os.fdopen(descriptor, "w", encoding="utf-8") as output:
        json.dump({"version": 1, "archive": str(archive.resolve()), "sha256": digest_file(archive)}, output)
        output.flush()
        os.fsync(output.fileno())
    os.replace(temporary, target / ".installation-pending.json")
    sync_directory(target)


def acquire_lock(path: Path, timeout: int) -> int:
    import fcntl  # Production and CI are Linux; no simulated Windows lock.
    descriptor = os.open(path, os.O_RDWR | os.O_CREAT, 0o600)
    deadline = time.monotonic() + timeout
    while True:
        try:
            fcntl.flock(descriptor, fcntl.LOCK_EX | fcntl.LOCK_NB)
            return descriptor
        except BlockingIOError:
            if time.monotonic() >= deadline:
                os.close(descriptor)
                raise RuntimeError("Deployment lock timeout; active files were not changed")
            time.sleep(min(1, max(0, deadline - time.monotonic())))


def install_bundle(archive: Path, target: Path) -> None:
    allowed_roots = {".env", "compose.prod.yml", "mongo-init.js", "scripts", "nginx"}
    with tarfile.open(archive, "r:gz") as bundle:
        members = bundle.getmembers()
        for member in members:
            path = Path(member.name)
            if (path.is_absolute() or ".." in path.parts or not path.parts
                    or path.parts[0] not in allowed_roots
                    or (len(path.parts) > 1 and path.parts[:2] == ("nginx", "runtime"))
                    or not (member.isfile() or member.isdir())):
                raise RuntimeError(f"Unsupported deployment bundle entry: {member.name}")
        files = {member.name for member in members if member.isfile()}
        if not {".env", "compose.prod.yml", "scripts/deploy.sh"}.issubset(files):
            raise RuntimeError("Incomplete deployment bundle")
        target.mkdir(parents=True, exist_ok=True)
        mark_installation(target, archive)
        # Validate the whole archive before the first active write. Each file is
        # replaced atomically, including directory-mounted Nginx configuration.
        # Install the entrypoint guard before changing .env/Compose. A direct
        # invocation cannot consume a partial installation on future attempts.
        for member in sorted(members, key=lambda entry: entry.name != "scripts/deploy.sh"):
            destination = target / member.name
            if member.isdir():
                destination.mkdir(parents=True, exist_ok=True)
                continue
            destination.parent.mkdir(parents=True, exist_ok=True)
            with bundle.extractfile(member) as source:
                descriptor, temporary = tempfile.mkstemp(dir=destination.parent)
                try:
                    with os.fdopen(descriptor, "wb") as output:
                        shutil.copyfileobj(source, output)
                        output.flush()
                        os.fsync(output.fileno())
                    mode = 0o600 if member.name == ".env" else stat.S_IMODE(member.mode)
                    if member.name.startswith("scripts/") and member.name.endswith(".sh"):
                        mode |= 0o111
                    os.chmod(temporary, mode)
                    os.replace(temporary, destination)
                    sync_directory(destination.parent)
                finally:
                    if os.path.exists(temporary):
                        os.unlink(temporary)
        (target / ".installation-pending.json").unlink()
        sync_directory(target)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("target", type=Path)
    args = parser.parse_args()
    lock_path = Path(os.environ.get("DEPLOY_LOCK_FILE", "/tmp/amusementpark-deploy.lock"))
    descriptor = acquire_lock(lock_path, int(os.environ.get("DEPLOY_LOCK_TIMEOUT_SECONDS", "900")))
    os.dup2(descriptor, 9, inheritable=True)
    if descriptor != 9:
        os.close(descriptor)
    os.environ["DEPLOY_LOCK_FILE"] = str(lock_path)
    os.environ["DEPLOY_LOCK_INHERITED"] = "true"
    target = args.target.resolve()
    installation = target / ".installation-pending.json"
    if installation.exists():
        pending = json.loads(installation.read_text(encoding="utf-8"))
        if (set(pending) != {"version", "archive", "sha256"} or pending["version"] != 1
                or not isinstance(pending["archive"], str)):
            raise RuntimeError("Invalid pending installation marker")
        pending_archive = Path(pending["archive"])
        if not pending_archive.is_absolute() or digest_file(pending_archive) != pending["sha256"]:
            raise RuntimeError("Pending installation bundle is missing or changed; active configuration remains blocked")
        install_bundle(pending_archive, target)
    journal = target / "nginx/runtime/deployment.json"
    if journal.exists():
        subprocess.run(["python3", str(target / "scripts/deployment_transaction.py"), "validate-journal"],
                       check=True, pass_fds=(9,))
        state = json.loads(journal.read_text(encoding="utf-8"))
        if state.get("version") != 1 or state.get("phase") not in {
                "prepared", "abandoning", "switch-candidate", "replace-canonical", "switch-canonical", "cleanup", "complete", "abandoned"}:
            raise RuntimeError("Invalid installed deployment journal; refusing to replace recovery configuration")
        if state["phase"] not in {"complete", "abandoned"}:
            # Finish using the installed config/images before replacing .env or
            # scripts. A new workflow never creates a third pair to recover B.
            print("Recovering the installed deployment before installing the next bundle", flush=True)
            recovery_environment = dict(os.environ)
            if state["phase"] in {"prepared", "abandoning"} and state.get("authority_exposed") is False:
                recovery_environment["DEPLOY_ABANDON_UNEXPOSED"] = "true"
            subprocess.run(["bash", str(target / "scripts/deploy.sh")], check=True, pass_fds=(9,), env=recovery_environment)
            if json.loads(journal.read_text(encoding="utf-8")).get("phase") not in {"complete", "abandoned"}:
                raise RuntimeError("Installed deployment did not complete; new bundle remains untouched")
    install_bundle(args.archive.resolve(strict=True), target)
    os.execvp("bash", ["bash", str(args.target.resolve() / "scripts/deploy.sh")])


if __name__ == "__main__":
    main()
