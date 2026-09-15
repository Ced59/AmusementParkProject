#!/usr/bin/env python3
"""CI ONLY: real Nginx/Node/Docker interruptions, no external HTTP requests."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import http.client
import fcntl
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

SCRIPTS = Path(__file__).resolve().parents[1]
REPO = SCRIPTS.parents[1]
sys.path.insert(0, str(SCRIPTS))
from deployment_runtime import DockerRuntime, GENERATION_LABEL
from deployment_transaction import DeploymentTransaction


def until(predicate, label, seconds=90):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        value = predicate()
        if value:
            return value
        time.sleep(0.1)
    raise AssertionError(f"Timed out: {label}")


def main():
    if os.environ.get("CI") != "true":
        raise SystemExit("This real-server test runs only in CI")
    with tempfile.TemporaryDirectory(prefix="amusementpark-deployment-test-") as temporary:
        directory = Path(temporary)
        for name in ("control", "fixture", "scripts", "seo/current"):
            (directory / name).mkdir(parents=True)
        shutil.copytree(SCRIPTS.parent / "nginx", directory / "nginx", ignore=shutil.ignore_patterns("runtime"))
        for name in ("deployment_transaction.py", "deployment_runtime.py"):
            shutil.copy2(SCRIPTS / name, directory / "scripts" / name)
        shutil.copy2(SCRIPTS / "tests/fixtures/deployment-compose.yml", directory / "compose.prod.yml")
        shutil.copy2(SCRIPTS / "tests/fixtures/deployment-server.mjs", directory / "fixture/deployment-server.mjs")
        shutil.copy2(REPO / "FRONT/AmusementPark/server-graceful-shutdown.ts", directory / "fixture/server-graceful-shutdown.ts")
        xml = b'<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><url><loc>https://fixture.test/fr/home</loc></url></urlset>'
        (directory / "seo/current/sitemap.xml").write_bytes(xml)
        with socket.socket() as reservation:
            reservation.bind(("127.0.0.1", 0))
            port = reservation.getsockname()[1]
        project = f"drain-test-{os.getpid()}"
        lock_path = directory / "deploy.lock"
        descriptor = os.open(lock_path, os.O_RDWR | os.O_CREAT, 0o600)
        os.dup2(descriptor, 9, inheritable=True)
        if descriptor != 9:
            os.close(descriptor)
        fcntl.flock(9, fcntl.LOCK_EX)
        os.environ.update({"COMPOSE_PROJECT_NAME": project, "PUBLIC_HTTP_PORT": str(port),
                           "PUBLIC_DOMAIN": "fixture.test", "FIXTURE_VERSION": "old",
                           "FRONT_SSR_API_INTERNAL_URL": f"http://{project}-api:8080",
                           "SSR_INTERNAL_BASE_URL": f"http://{project}-edge:4000",
                           "ALLOWED_HOSTS": f"fixture.test;localhost;127.0.0.1;{project}-api", "DEPLOY_HEALTH_TIMEOUT_SECONDS": "90",
                           "DEPLOY_LOCK_FILE": str(lock_path),
                           "DEPLOY_DRAIN_TIMEOUT_SECONDS": "90", "DEPLOY_STOP_TIMEOUT_SECONDS": "15"})
        command = ["docker", "compose", "--project-name", project, "-f", str(directory / "compose.prod.yml")]
        children = []
        requests = ThreadPoolExecutor(max_workers=3)

        def get(path, data=None):
            request = urllib.request.Request(f"http://127.0.0.1:{port}{path}", data=data,
                                             headers={"Host": "fixture.test", "Connection": "close"})
            with urllib.request.urlopen(request, timeout=120) as response:
                if path == "/slow?key=body":
                    prefix = response.readline()
                    (directory / "control/client-body-started").write_bytes(prefix)
                    return prefix + response.read(), dict(response.headers)
                return response.read(), dict(response.headers)

        def document(path, data=None):
            return json.loads(get(path, data)[0])

        def start_transaction():
            log = open(directory / f"attempt-{len(children)}.log", "w")
            process = subprocess.Popen([sys.executable, str(directory / "scripts/deployment_transaction.py"), "deploy"],
                                       cwd=directory, stdout=log, stderr=subprocess.STDOUT, pass_fds=(9,))
            children.append((process, log))
            return process

        def phase(name):
            state = transaction.read()
            return state if state["phase"] == name else None

        try:
            subprocess.run([*command, "up", "-d"], check=True, cwd=directory)
            old_runtime = DockerRuntime(directory)
            for service in ("edge", "api", "front"):
                old_runtime.wait_healthy(old_runtime.find(old_runtime.names[service]))
            original = document("/pair")
            assert original["front"]["version"] == original["api"]["version"] == "old"
            before_xml, before_headers = get("/sitemap.xml")
            assert before_xml == xml and before_headers.get("X-AmusementPark-SEO-Source") == "static"
            os.environ["FIXTURE_VERSION"] = "new"
            # Customized hosts intentionally omit the canonical internal name.
            # The runtime must add exactly that name, and the fixture enforces it.
            os.environ["ALLOWED_HOSTS"] = "fixture.test;localhost;127.0.0.1"
            runtime = DockerRuntime(directory)
            transaction = DeploymentTransaction(runtime, directory / "nginx/runtime")
            transaction.prepare()
            headers_request = requests.submit(get, "/slow?key=headers")
            body_request = requests.submit(get, "/slow?key=body")
            write_request = requests.submit(document, "/api/write", b"one mutation")
            for key in ("headers", "body", "write"):
                until(lambda: (directory / f"control/started-{key}").exists(), "old request began")
            until(lambda: (directory / "control/client-body-started").exists(), "client received initial body bytes")
            first = start_transaction()
            state = until(lambda: phase("switch-candidate"), "candidate routing intent")
            until(lambda: state["transition"] if (state := transaction.read()).get("transition")
                  and runtime.edge_generation() == state["transition"]["generation"] else None, "candidate route active")
            selected = document("/pair")
            assert selected["front"]["version"] == selected["api"]["version"] == "new"
            selected_pair = transaction.read()["candidate"]
            # With Docker's default hostname, the fixture exposes the actual
            # container ID prefix, not an identity derived from its API URL.
            for service in ("front", "api"):
                assert selected_pair[service]["id"][:12] == selected[service]["name"]
            callback = document("/api/callback")
            assert callback["status"] == 200 and callback["callback"]["name"] == selected["front"]["name"]
            assert len((directory / "control/writes").read_text().splitlines()) == 1
            assert not headers_request.done() and not body_request.done() and not write_request.done()
            first.kill()
            first.wait(timeout=10)
            assert len(runtime.candidates()) == 2
            assert runtime.find(runtime.names["front"]) is not None
            assert runtime.find(runtime.names["api"]) is not None
            assert transaction.read()["authority_exposed"] is True

            second = start_transaction()
            for key in ("headers", "body", "write"):
                (directory / f"control/release-{key}").touch()
            assert headers_request.result(timeout=20)[0] == f"end:{original['front']['name']}\n".encode()
            assert body_request.result(timeout=20)[0] == f"start:{original['front']['name']}\nend:{original['front']['name']}\n".encode()
            assert write_request.result(timeout=20)["name"] == original["api"]["name"]
            assert len((directory / "control/writes").read_text().splitlines()) == 1
            def partial_canonical_api():
                current = phase("replace-canonical")
                if not current or runtime.find(runtime.names["front"]) is not None:
                    return None
                api = runtime.inspect(runtime.names["api"])
                if not api or api["Config"]["Labels"].get(GENERATION_LABEL) != current["generation"]:
                    return None
                assert api["Id"] != current["original"]["api"]["id"]
                assert api["State"].get("Health", {}).get("Status") != "healthy"
                return runtime.reference(api)
            partial_api = until(partial_canonical_api, "B active with new-generation C API incomplete")
            second.kill()
            second.wait(timeout=10)
            assert len(runtime.candidates()) == 2
            candidate_request = requests.submit(get, "/slow?key=candidatebody")
            until(lambda: (directory / "control/started-candidatebody").exists(), "candidate request began")
            third = start_transaction()
            (directory / "control/canonical-api-ready").touch()
            until(lambda: phase("switch-canonical"), "canonical routing intent")
            until(lambda: state["transition"] if (state := transaction.read()).get("transition")
                  and runtime.edge_generation() == state["transition"]["generation"] else None, "canonical route active")
            canonical = document("/pair")
            canonical_pair = transaction.read()["canonical"]
            for service in ("front", "api"):
                assert canonical_pair[service]["id"][:12] == canonical[service]["name"]
                assert canonical[service]["name"] != selected[service]["name"]
            assert runtime.find(runtime.names["api"]) == partial_api
            assert len(runtime.candidates()) == 2 and not candidate_request.done()
            (directory / "control/release-candidatebody").touch()
            assert candidate_request.result(timeout=20)[0] == f"start:{selected['front']['name']}\nend:{selected['front']['name']}\n".encode()
            assert third.wait(timeout=90) == 0
            state = transaction.read()
            assert state["phase"] == "complete" and not runtime.candidates()
            runtime.verify_pair(state["canonical"], candidate=False)
            callback = document("/api/callback")
            assert callback["callback"]["name"] == canonical["front"]["name"]
            try:
                get("/internal/cache/invalidate", b"{}")
                raise AssertionError("Unauthenticated callback accepted")
            except urllib.error.HTTPError as error:
                assert error.code == 403
            after_xml, after_headers = get("/sitemap.xml")
            assert hashlib.sha256(after_xml).digest() == hashlib.sha256(before_xml).digest()
            assert after_headers.get("Content-Type") == before_headers.get("Content-Type")
            assert after_headers.get("X-AmusementPark-SEO-Source") == "static"
            stopped = (directory / "control/stopped").read_text().splitlines()
            assert original["front"]["name"] in stopped and selected["front"]["name"] in stopped

            # Separate proof: the actual Node shutdown helper also waits for a
            # direct HTTP request still active when SIGTERM is delivered.
            networks = runtime.inspect(state["canonical"]["front"]["id"])["NetworkSettings"]["Networks"]
            address = next(iter(networks.values()))["IPAddress"]
            def direct_request():
                connection = http.client.HTTPConnection(address, 4000, timeout=30)
                try:
                    connection.request("GET", "/slow?key=directbody")
                    response = connection.getresponse()
                    assert response.status == 200
                    return response.read()
                finally:
                    connection.close()
            direct = requests.submit(direct_request)
            until(lambda: (directory / "control/started-directbody").exists(), "direct request began")
            stopping = requests.submit(runtime.stop_remove, state["canonical"]["front"])
            until(lambda: (directory / f"control/signal-received-{canonical['front']['name']}").exists(), "SIGTERM received")
            assert not stopping.done() and not direct.done()
            (directory / "control/release-directbody").touch()
            assert direct.result(timeout=20) == f"start:{canonical['front']['name']}\nend:{canonical['front']['name']}\n".encode()
            stopping.result(timeout=20)
            print("Real Docker transaction passed: headers/body drained, two interruptions recovered, pairs/callbacks/XML preserved")
        finally:
            # Release every fixture rendezvous before joining outstanding I/O.
            for key in ("headers", "body", "write", "candidatebody", "directbody"):
                (directory / f"control/release-{key}").touch()
            requests.shutdown(wait=True)
            for process, log in children:
                if process.poll() is None:
                    process.kill()
                    process.wait(timeout=10)
                log.close()
            for path in directory.glob("attempt-*.log"):
                print(path.name, path.read_text())
            subprocess.run([*command, "logs", "--tail", "30"], cwd=directory, check=False)
            subprocess.run([*command, "down", "--remove-orphans", "--volumes"], cwd=directory, check=False)
            os.close(9)


if __name__ == "__main__":
    main()
