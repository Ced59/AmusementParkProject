#!/usr/bin/env python3
"""Restore one complete captured application pair using the existing transaction.

No database restore, shared-service recreation, old scripts or old secrets. The
checkpoint contains image identities and a config hash, never rendered config.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys

from deployment_runtime import DeploymentError, DockerRuntime
from deployment_transaction import DeploymentTransaction, atomic_write


HEX = re.compile(r"[0-9a-f]{64}\Z")
IMAGE_ID = re.compile(r"sha256:[0-9a-f]{64}\Z")
SERVICES = ("api", "front")


def load_environment(path):
    # Same literal format as env-loader.sh; never evaluate shell expressions.
    values = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        name, separator, value = line.partition("=")
        name, value = name.strip(), value.strip()
        if not separator or not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name) or name in values:
            raise DeploymentError("Invalid or duplicate environment setting")
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        values[name] = value
    os.environ.update(values)
    return values


def configuration_hash(runtime):
    config = json.loads(json.dumps(runtime.config))
    for service in SERVICES:
        config["services"][service]["image"] = "application-image-only"
    return hashlib.sha256(json.dumps(config, sort_keys=True).encode()).hexdigest()


def checkpoint_path(directory):
    return directory / "nginx/runtime/application-rollback.json"


def identifier(record):
    payload = {key: value for key, value in record.items() if key not in {"id", "phase"}}
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode()).hexdigest()


def repository(reference):
    return reference.split("@", 1)[0].rsplit(":", 1)[0]


def read_checkpoint(directory):
    path = checkpoint_path(directory)
    if not path.exists():
        return None
    if path.is_symlink() or path.stat().st_size > 16384:
        raise DeploymentError("Invalid application rollback checkpoint file")
    record = json.loads(path.read_text(encoding="utf-8"))
    if (set(record) != {"version", "id", "phase", "project", "configuration", "previous", "forward"}
            or record["version"] != 1 or record["phase"] not in {"ready", "restoring", "restored"}
            or not HEX.fullmatch(record["id"]) or identifier(record) != record["id"]
            or not HEX.fullmatch(record["configuration"])):
        raise DeploymentError("Invalid application rollback checkpoint")
    for group in ("previous", "forward"):
        if set(record[group]) != set(SERVICES):
            raise DeploymentError("Rollback requires both application images")
        for image in record[group].values():
            if (set(image) != {"reference", "id"} or not IMAGE_ID.fullmatch(image["id"])
                    or not isinstance(image["reference"], str) or len(image["reference"]) > 512):
                raise DeploymentError("Invalid rollback image identity")
    for service in SERVICES:
        previous = record["previous"][service]["reference"]
        forward = record["forward"][service]["reference"]
        # A GHCR production image must be pinned to a registry digest. Locally
        # built images can use their immutable ID (also exercised by Docker CI).
        if previous != record["previous"][service]["id"] and not re.fullmatch(
                re.escape(repository(forward)) + r"@sha256:[0-9a-f]{64}", previous):
            raise DeploymentError("Rollback image repository or digest changed")
        if forward.startswith("ghcr.io/") and IMAGE_ID.fullmatch(previous):
            raise DeploymentError("A production rollback needs a retrievable registry digest")
    return record


def save_checkpoint(directory, record):
    record["id"] = identifier(record)
    atomic_write(checkpoint_path(directory), json.dumps(record, sort_keys=True) + "\n")


def image_identity(runtime, reference, configured):
    image = json.loads(runtime.run("docker", "image", "inspect", reference))[0]
    if not IMAGE_ID.fullmatch(image["Id"]):
        raise DeploymentError("Invalid Docker image identity")
    digests = [value for value in image.get("RepoDigests", [])
               if re.fullmatch(re.escape(repository(configured)) + r"@sha256:[0-9a-f]{64}", value)]
    if configured.startswith("ghcr.io/") and len(digests) != 1:
        raise DeploymentError("Cannot capture one retrievable production image digest")
    if configured.startswith("ghcr.io/"):
        runtime.run("docker", "manifest", "inspect", digests[0])
    return {"reference": digests[0] if len(digests) == 1 else image["Id"], "id": image["Id"]}


def current_pair(runtime):
    pair = {service: runtime.find(runtime.names[service]) for service in SERVICES}
    for service, reference in pair.items():
        runtime.wait_healthy(reference)
        container = runtime.inspect(reference["id"])
        labels = container["Config"].get("Labels") or {}
        if (labels.get("com.docker.compose.project") != runtime.project
                or labels.get("com.docker.compose.service") != service):
            raise DeploymentError("Application container ownership changed")
    return pair


def capture(runtime, directory):
    record = read_checkpoint(directory)
    if record and record["phase"] == "restoring":
        raise DeploymentError("Resume the exact application rollback before another deployment")
    forward = {service: {"reference": runtime.config["services"][service]["image"],
                         "id": json.loads(runtime.run("docker", "image", "inspect",
                                        runtime.config["services"][service]["image"]))[0]["Id"]}
               for service in SERVICES}
    transaction = DeploymentTransaction(runtime, directory / "nginx/runtime")
    state = transaction.read()
    if state and state["phase"] not in {"complete", "abandoned"}:
        if record and record["forward"] == forward and record["configuration"] == configuration_hash(runtime):
            return record  # Resume; never replace the pre-exposure checkpoint.
        raise DeploymentError("Pending transaction has no matching previous application pair")
    runtime.preflight()
    if runtime.candidates():
        raise DeploymentError("Unknown candidates prevent application rollback capture")
    pair = current_pair(runtime)
    runtime.verify_pair(pair, candidate=False)
    previous = {service: image_identity(runtime, runtime.inspect(pair[service]["id"])["Image"],
                                       forward[service]["reference"]) for service in SERVICES}
    if all(previous[service]["id"] == forward[service]["id"] for service in SERVICES):
        return record  # Re-deploy of the same generation; preserve its checkpoint.
    record = {"version": 1, "phase": "ready", "project": runtime.project,
              "configuration": configuration_hash(runtime), "previous": previous, "forward": forward}
    save_checkpoint(directory, record)
    return record


def replace_application_images(path, images):
    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
    for service in SERVICES:
        name = service.upper() + "_IMAGE"
        matches = [index for index, line in enumerate(lines) if line.partition("=")[0].strip() == name]
        if len(matches) != 1:
            raise DeploymentError("Rollback requires exactly one setting for each application image")
        lines[matches[0]] = name + "=" + images[service]["reference"] + "\n"
    atomic_write(path, "".join(lines))  # Both identities change in one atomic write.


def restore(runtime, directory, expected):
    record = read_checkpoint(directory)
    if record is None or record["id"] != expected:
        raise DeploymentError("Exact application rollback checkpoint approval required")
    if runtime.project != record["project"] or configuration_hash(runtime) != record["configuration"]:
        raise DeploymentError("Configuration changed; image-only rollback is not safe")
    if os.environ.get("SHARED_INFRASTRUCTURE_MAINTENANCE", "none") != "none":
        raise DeploymentError("Application rollback cannot authorize shared infrastructure maintenance")
    runtime.preflight()
    transaction = DeploymentTransaction(runtime, directory / "nginx/runtime")
    state = transaction.read()
    if record["phase"] != "restoring":
        if not state or state["phase"] != "complete" or runtime.candidates():
            raise DeploymentError("Finish the installed transaction before restoring a previous pair")
        pair = current_pair(runtime)
        expected_images = record["previous"] if record["phase"] == "restored" else record["forward"]
        if any(runtime.inspect(pair[service]["id"])["Image"] != expected_images[service]["id"] for service in SERVICES):
            raise DeploymentError("Active application images no longer match this checkpoint")
        runtime.verify_pair(pair, candidate=False)
        transaction.state = state
        transaction.assert_active_route(pair["front"])
        if record["phase"] == "restored":
            return record
    # Render the rollback configuration before any active write. A resume may
    # only finish its own transaction, never reinterpret another pending one.
    prior_environment = {service.upper() + "_IMAGE": os.environ.get(service.upper() + "_IMAGE") for service in SERVICES}
    try:
        for service in SERVICES:
            os.environ[service.upper() + "_IMAGE"] = record["previous"][service]["reference"]
        restored_runtime = DockerRuntime(directory)
    finally:
        for name, value in prior_environment.items():
            if value is None:
                os.environ.pop(name, None)
            else:
                os.environ[name] = value
    if configuration_hash(restored_runtime) != record["configuration"]:
        raise DeploymentError("Rollback changed more than the application images")
    if record["phase"] == "restoring":
        if not state:
            raise DeploymentError("Rollback journal disappeared; refusing an ambiguous resume")
        if state["phase"] not in {"complete", "abandoned"} and state["fingerprint"] != restored_runtime.fingerprint:
            raise DeploymentError("Another transaction cannot be resumed as an application rollback")
        if state["phase"] in {"complete", "abandoned"}:
            pair = current_pair(runtime)
            actual = {service: runtime.inspect(pair[service]["id"])["Image"] for service in SERVICES}
            if not any(all(actual[service] == record[group][service]["id"] for service in SERVICES)
                       for group in ("previous", "forward")):
                raise DeploymentError("Active pair changed during rollback interruption")
    # Prove both exact images exist before writing the environment or exposing
    # anything. Pull only application digests, never edge/MongoDB/MinIO.
    for image in record["previous"].values():
        if not IMAGE_ID.fullmatch(image["reference"]):
            runtime.run("docker", "pull", image["reference"])
        actual = json.loads(runtime.run("docker", "image", "inspect", image["reference"]))[0]
        if actual["Id"] != image["id"]:
            raise DeploymentError("Previous application image identity changed")
    record["phase"] = "restoring"
    save_checkpoint(directory, record)  # Persist intent before changing .env.
    replace_application_images(directory / ".env", record["previous"])
    for service in SERVICES:
        os.environ[service.upper() + "_IMAGE"] = record["previous"][service]["reference"]
    restored_transaction = DeploymentTransaction(restored_runtime, directory / "nginx/runtime")
    restored_transaction.prepare()
    restored_transaction.execute()  # Same pair/worker/callback/drain/edge proof.
    state = restored_transaction.read()
    if any(restored_runtime.inspect(state["canonical"][service]["id"])["Image"] != record["previous"][service]["id"]
           for service in SERVICES):
        raise DeploymentError("Restored application images do not match the approved previous pair")
    restored_runtime.verify_pair(state["canonical"], candidate=False)
    restored_transaction.state = state
    restored_transaction.assert_active_route(state["canonical"]["front"])
    record["phase"] = "restored"
    save_checkpoint(directory, record)
    return record


def hold_lock():
    import fcntl
    path = Path(os.environ.get("DEPLOY_LOCK_FILE", "/tmp/amusementpark-deploy.lock"))
    if os.environ.get("DEPLOY_LOCK_INHERITED") == "true":
        held, expected = os.fstat(9), path.stat()
        if (held.st_dev, held.st_ino) != (expected.st_dev, expected.st_ino):
            raise DeploymentError("Application rollback lock identity mismatch")
        fcntl.flock(9, fcntl.LOCK_EX | fcntl.LOCK_NB)
    else:
        # Same lock implementation as the uploaded installer, without creating
        # another owner or changing the existing timeout/lease semantics.
        import importlib.util
        spec = importlib.util.spec_from_file_location("application_installer", Path(__file__).with_name("install-deployment.py"))
        installer = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(installer)
        descriptor = installer.acquire_lock(path, int(os.environ.get("DEPLOY_LOCK_TIMEOUT_SECONDS", "900")))
        os.dup2(descriptor, 9, inheritable=True)
        if descriptor != 9:
            os.close(descriptor)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("checkpoint", "restore", "status", "assert-forward-safe"))
    parser.add_argument("--expect")
    args = parser.parse_args()
    if args.command == "restore" and (not args.expect or not HEX.fullmatch(args.expect)):
        raise DeploymentError("A full checkpoint SHA-256 is required")
    hold_lock()  # Before reading .env or active runtime state.
    directory = Path(__file__).resolve().parent.parent
    if (directory / ".installation-pending.json").exists():
        raise DeploymentError("Finish the pending bundle installation first")
    if args.command == "assert-forward-safe":
        record = read_checkpoint(directory)
        if record and record["phase"] == "restoring":
            raise DeploymentError("Resume the exact application rollback before a forward deployment")
        return
    load_environment(directory / ".env")
    if args.command == "status":
        record = read_checkpoint(directory)
    else:
        runtime = DockerRuntime(directory)
        record = capture(runtime, directory) if args.command == "checkpoint" else restore(runtime, directory, args.expect)
    print(json.dumps({"applicationRollback": record}, sort_keys=True))


if __name__ == "__main__":
    try:
        main()
    except (DeploymentError, OSError, ValueError, KeyError, TypeError) as error:
        print(f"Application rollback stopped safely: {error}", file=sys.stderr)
        raise SystemExit(1)
