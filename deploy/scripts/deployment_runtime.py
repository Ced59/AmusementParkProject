"""Docker/Nginx boundary for the deployment transaction. No shell interpolation."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import time
import urllib.request


GENERATION_LABEL = "fun.amusement-parks.deployment-generation"
NAME = re.compile(r"[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}\Z")


class DeploymentError(RuntimeError):
    pass


def validate_single_worker_configuration(configuration: str):
    if re.findall(r"(?m)^\s*worker_processes\s+([^;]+);", configuration) != ["1"]:
        raise DeploymentError("Routing attestation requires exactly one configured Nginx worker")


class DockerRuntime:
    def __init__(self, directory: Path):
        self.directory = directory
        self.project = os.environ.get("COMPOSE_PROJECT_NAME", "amusementpark")
        if not NAME.fullmatch(self.project):
            raise DeploymentError("Invalid Compose project")
        self.environment = dict(os.environ)
        self.environment["DEPLOYMENT_GENERATION"] = "unmanaged"
        self.command_timeout = int(os.environ.get("DEPLOY_COMPOSE_UP_TIMEOUT_SECONDS", "300"))
        self.health_timeout = int(os.environ.get("DEPLOY_HEALTH_TIMEOUT_SECONDS", "180"))
        self.drain_timeout = int(os.environ.get("DEPLOY_DRAIN_TIMEOUT_SECONDS", "190"))
        self.stop_timeout = int(os.environ.get("DEPLOY_STOP_TIMEOUT_SECONDS", "190"))
        self.public_domain = os.environ.get("PUBLIC_DOMAIN", "amusement-parks.fun")
        self.edge_url = "http://127.0.0.1:" + os.environ.get("PUBLIC_HTTP_PORT", "18080")
        config = json.loads(self.compose("config", "--format", "json"))
        self.names = {service: config["services"][service]["container_name"] for service in ("front", "api", "edge")}
        if any(not NAME.fullmatch(name) for name in self.names.values()):
            raise DeploymentError("Invalid application container name")
        self.environment["FRONT_SSR_API_INTERNAL_URL"] = f"http://{self.names['api']}:8080"
        allowed_hosts = self.environment.get("ALLOWED_HOSTS", "").split(";")
        if self.names["api"] not in allowed_hosts:
            allowed_hosts.append(self.names["api"])
        self.environment["ALLOWED_HOSTS"] = ";".join(allowed_hosts)
        expected_callback = f"http://{self.names['edge']}:4000"
        if os.environ.get("SSR_INTERNAL_BASE_URL", expected_callback).rstrip("/") != expected_callback:
            raise DeploymentError("Transactional deployment requires SSR_INTERNAL_BASE_URL to select the edge")
        self.environment["SSR_INTERNAL_BASE_URL"] = expected_callback
        self.config = json.loads(self.compose("config", "--format", "json"))
        self.fingerprint = hashlib.sha256(json.dumps(self.config, sort_keys=True).encode()).hexdigest()

    def run(self, *arguments: str, environment=None, timeout=None) -> str:
        result = subprocess.run(arguments, cwd=self.directory, env=environment or self.environment,
                                capture_output=True, text=True, timeout=timeout or self.command_timeout)
        if result.returncode:
            # Do not include rendered Compose config or environment in diagnostics.
            raise DeploymentError(f"Command {arguments[0]} {arguments[1]} failed ({result.returncode}): {result.stderr[-2000:]}")
        return result.stdout.strip()

    def compose(self, *arguments: str, generation=None) -> str:
        environment = dict(self.environment)
        if generation is not None:
            environment["DEPLOYMENT_GENERATION"] = generation
        return self.run("docker", "compose", "--project-name", self.project, "-f", "compose.prod.yml",
                        *arguments, environment=environment)

    def inspect(self, identifier: str):
        # A missing container is different from a daemon/transport failure.
        identifiers = self.run("docker", "ps", "-aq", "--no-trunc", "--filter", f"id={identifier}")
        if not identifiers and re.fullmatch(r"[0-9a-f]{64}", identifier):
            return None
        if not re.fullmatch(r"[0-9a-f]{64}", identifier):
            identifiers = self.run("docker", "ps", "-aq", "--no-trunc", "--filter", f"name=^/{identifier}$")
            if not identifiers:
                return None
        return json.loads(self.run("docker", "inspect", identifier))[0]

    @staticmethod
    def reference(container):
        return {"id": container["Id"], "name": container["Name"].lstrip("/")}

    def find(self, name: str, generation=None):
        container = self.inspect(name)
        if container is None:
            return None
        labels = container["Config"].get("Labels") or {}
        if labels.get("com.docker.compose.project") != self.project:
            raise DeploymentError(f"Container {name} belongs to another project")
        if generation is not None and labels.get(GENERATION_LABEL) != generation:
            raise DeploymentError(f"Container {name} has an unexpected deployment generation")
        return self.reference(container)

    def candidates(self):
        identifiers = self.run("docker", "ps", "-aq", "--no-trunc", "--filter", f"label=com.docker.compose.project={self.project}")
        if not identifiers:
            return []
        containers = json.loads(self.run("docker", "inspect", *identifiers.splitlines()))
        return [self.reference(container) for container in containers
                if (container["Config"].get("Labels") or {}).get("com.docker.compose.service") in {"api", "front"}
                and container["Name"].lstrip("/") not in {self.names["api"], self.names["front"]}]

    def preflight(self):
        for service in ("edge", "mongodb", "minio"):
            identifier = self.compose("ps", "-q", service)
            if not identifier:
                raise DeploymentError(f"Missing {service}; initial installation/infra changes require a separate maintenance deployment")
            container = self.inspect(identifier)
            expected_hash = self.compose("config", "--hash", service).split()[-1]
            if container["Config"]["Labels"].get("com.docker.compose.config-hash") != expected_hash:
                raise DeploymentError(f"{service} configuration changed; refusing to recreate shared infrastructure during a rolling deployment")
            desired_image = json.loads(self.run("docker", "image", "inspect", self.config["services"][service]["image"]))[0]["Id"]
            if desired_image != container["Image"]:
                raise DeploymentError(f"{service} image changed; shared infrastructure needs a separate maintenance deployment")
        self.wait_healthy(self.find(self.names["edge"]))
        validate_single_worker_configuration(self.run("docker", "exec", self.names["edge"], "nginx", "-T", "-c",
                                                      "/etc/nginx/amusementpark/edge.conf"))

    def wait_healthy(self, reference):
        if reference is None:
            raise DeploymentError("Missing required deployment container")
        deadline = time.monotonic() + self.health_timeout
        while True:
            container = self.inspect(reference["id"])
            if container is None or container["Name"].lstrip("/") != reference["name"]:
                raise DeploymentError("Deployment container identity changed")
            state = container["State"]
            if state.get("Health", {}).get("Status") == "healthy":
                return
            if state["Status"] in ("exited", "dead") or time.monotonic() >= deadline:
                raise DeploymentError(f"Container {reference['name']} did not become healthy")
            time.sleep(2)

    def start_candidate(self, service: str, name: str, generation: str, api_name: str):
        existing = self.find(name, generation)
        if existing is None:
            arguments = ["run", "-d", "--no-deps", "--name", name,
                         "--label", f"{GENERATION_LABEL}={generation}"]
            if service == "api":
                # The candidate overlaps the previous API. It may scan migrations,
                # but only the canonical API started after that writer stops may complete them.
                arguments += ["-e", "DurableBackgroundJobs__Worker__Enabled=false", "-e",
                              "MongoDB__CompleteFactualEventMigrationsOnStartup=false", "-e",
                              f"AllowedHosts={self.environment['ALLOWED_HOSTS']};{name}"]
            else:
                arguments += ["-e", f"SSR_API_INTERNAL_URL=http://{api_name}:8080"]
            self.compose(*arguments, service, generation=generation)
            existing = self.find(name, generation)
        self.wait_healthy(existing)
        return existing

    def create_canonical(self, service: str, generation: str):
        existing = self.find(self.names[service], generation)
        if existing is None:
            self.compose("up", "-d", "--no-deps", service, generation=generation)
            existing = self.find(self.names[service], generation)
        self.wait_healthy(existing)
        return existing

    def stop_remove(self, reference, allow_failed_start=False, record_exit=None):
        container = self.inspect(reference["id"])
        if container is None:
            return
        if container["Name"].lstrip("/") != reference["name"]:
            raise DeploymentError("Refusing to stop a renamed deployment container")
        labels = container["Config"].get("Labels") or {}
        if (labels.get("com.docker.compose.project") != self.project
                or labels.get("com.docker.compose.service") not in {"api", "front"}):
            raise DeploymentError("Refusing to stop a container outside the application pair")
        # Never rm -f. An error/timeout returns before the caller touches its API.
        was_running = container["State"]["Running"]
        self.run("docker", "stop", "--time", str(self.stop_timeout), reference["id"], timeout=self.stop_timeout + 15)
        stopped = self.inspect(reference["id"])
        if stopped is None:
            raise DeploymentError("Stopped container disappeared before its exit status was checked")
        state = stopped["State"]
        if state["Running"]:
            raise DeploymentError("Container did not stop")
        legacy = labels.get(GENERATION_LABEL) in (None, "unmanaged")
        accepted_exit_codes = {0, 143} if legacy else {0}
        if allow_failed_start and not was_running:
            # Broken entrypoints/startup can be discarded ONLY after the caller
            # proved non-exposure. Forced kills/OOM still require investigation.
            accepted_exit_codes |= {1, 2, 126, 127}
        if state.get("OOMKilled") or state.get("ExitCode") not in accepted_exit_codes:
            raise DeploymentError(f"Abnormal stop for {reference['name']} (exit {state.get('ExitCode')}); container and dependent API retained")
        if record_exit is not None:
            record_exit({"exit_code": state["ExitCode"], "oom_killed": bool(state.get("OOMKilled"))})
        self.run("docker", "rm", reference["id"])

    def edge_processes(self):
        edge = self.find(self.names["edge"])
        if edge is None:
            raise DeploymentError("Edge container disappeared")
        # Linux process start times defeat PID reuse. Children include retiring
        # workers, so a later switch also drains workers from an interrupted reload.
        output = self.run("docker", "exec", edge["id"], "sh", "-ec",
                          'm=$(cat /var/run/nginx.pid); cat /proc/$m/stat; '
                          'for p in $(cat /proc/$m/task/$m/children); do '
                          'if value=$(cat /proc/$p/stat 2>/dev/null); then printf "%s\\n" "$value"; '
                          'elif [ -d /proc/$p ]; then echo "Unreadable live worker stat" >&2; exit 1; fi; done')
        processes = []
        for line in output.splitlines():
            closing = line.rfind(")")
            fields = line[closing + 2:].split()
            processes.append({"pid": int(line.split(" ", 1)[0]), "start": int(fields[19])})
        if len(processes) < 2:
            raise DeploymentError("Cannot establish Nginx master/worker identities")
        return {"container": edge["id"], "master": processes[0], "workers": processes[1:]}

    def reload_edge(self):
        edge = self.find(self.names["edge"])
        self.run("docker", "exec", edge["id"], "nginx", "-t", "-c", "/etc/nginx/amusementpark/edge.conf")
        self.run("docker", "exec", edge["id"], "nginx", "-s", "reload", "-c", "/etc/nginx/amusementpark/edge.conf")

    def edge_response(self):
        request = urllib.request.Request(self.edge_url + "/edge-healthz", headers={"Host": self.public_domain, "Connection": "close"})
        with urllib.request.urlopen(request, timeout=5) as response:
            if response.status != 204:
                raise DeploymentError("Unexpected edge attestation status")
            worker = response.headers.get("X-AmusementPark-Worker", "")
            return {"generation": response.headers.get("X-AmusementPark-Deployment", ""),
                    "pid": int(worker) if worker.isdigit() else None}

    def edge_generation(self):
        return self.edge_response()["generation"]

    def verify_pair(self, pair, candidate: bool):
        for service in ("api", "front"):
            self.wait_healthy(pair[service])
        api = self.inspect(pair["api"]["id"])
        front = self.inspect(pair["front"]["id"])
        for service, container in (("api", api), ("front", front)):
            labels = container["Config"].get("Labels") or {}
            if (labels.get("com.docker.compose.project") != self.project
                    or labels.get("com.docker.compose.service") != service):
                raise DeploymentError("Pair contains an unexpected container role")
        api_env = dict(value.split("=", 1) for value in api["Config"]["Env"])
        front_env = dict(value.split("=", 1) for value in front["Config"]["Env"])
        if front_env.get("SSR_API_INTERNAL_URL", "").rstrip("/") != f"http://{pair['api']['name']}:8080":
            raise DeploymentError("Front is not pinned to its own API")
        if api_env.get("Ssr__InternalBaseUrl", "").rstrip("/") != f"http://{self.names['edge']}:4000":
            raise DeploymentError("API callback does not select the active edge")
        if (api_env.get("DurableBackgroundJobs__Worker__Enabled", "true").lower() == "false") != candidate:
            raise DeploymentError("Unexpected durable-worker role for this deployment pair")
        if candidate:
            for service, container in (("api", api), ("front", front)):
                aliases = {alias for network in container["NetworkSettings"]["Networks"].values()
                           for alias in (network.get("Aliases") or [])}
                if aliases & {service, self.names[service], f"amusementpark-{service}"}:
                    raise DeploymentError("Candidate shares a production alias")
        # Healthcheck localhost alone cannot prove that ASP.NET accepts the
        # unique internal Host emitted by SSR. Check from this exact front.
        self.run("docker", "exec", pair["front"]["id"], "node", "--input-type=module", "-e",
                 "const response = await fetch(new URL('/health', process.env.SSR_API_INTERNAL_URL), "
                 "{signal: AbortSignal.timeout(10000)}); "
                 "if (response.status !== 200) { throw new Error('Paired API health HTTP ' + response.status); }",
                 timeout=15)
