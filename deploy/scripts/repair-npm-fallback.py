#!/usr/bin/env python3
"""Remove the NPM default host's self-proxy without restarting shared sites.

Opt-in maintenance only: never called by an application deployment. The Compose
file stays on the server; neither its contents nor environment are printed.
"""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile


MARKER = "# Serve fallback assets locally; proxying to this listener loops."
TARGET = "/etc/nginx/conf.d/default.conf"
ASSET_INCLUDE = re.compile(r"(?m)^(\s*)include conf\.d/include/assets\.conf;[ \t]*$")


def repair_default_config(content):
    required = ['server_name localhost-nginx-proxy-manager;',
                'set $server "127.0.0.1";', 'set $port "80";',
                'root /var/www/html;', 'ssl_reject_handshake on;']
    if not all(value in content for value in required):
        raise ValueError("Unrecognized default host configuration; manual review required")
    matches = list(ASSET_INCLUDE.finditer(content))
    if not matches and MARKER in content:
        return content
    if len(matches) != 1:
        raise ValueError("Expected exactly one default-host asset proxy include")
    return ASSET_INCLUDE.sub(lambda match: match.group(1) + MARKER, content, count=1)


def add_compose_mount(content, service, filename):
    if not re.fullmatch(r"[A-Za-z0-9_.-]+", filename):
        raise ValueError("The bind mount filename must be a simple relative filename")
    lines = content.splitlines(keepends=True)
    service_pattern = re.compile(r"^( +)" + re.escape(service) + r":\s*(?:#.*)?$")
    starts = [(i, len(match.group(1))) for i, line in enumerate(lines)
              if (match := service_pattern.match(line.rstrip("\r\n")))]
    if len(starts) != 1:
        raise ValueError("Expected one unambiguous Compose service")
    start, indent = starts[0]
    end = next((i for i in range(start + 1, len(lines))
                if lines[i].strip() and not lines[i].lstrip().startswith('#')
                and len(lines[i]) - len(lines[i].lstrip()) <= indent), len(lines))
    block = lines[start:end]
    expected = f"./{filename}:{TARGET}:ro"
    mounted = [line.strip().removeprefix('- ').strip('\"\'')
               for line in block if TARGET in line and not line.lstrip().startswith('#')]
    if mounted:
        if mounted == [expected]:
            return content
        raise ValueError("A different default configuration mount already exists")
    volumes = [i for i in range(start + 1, end)
               if re.fullmatch(r" +volumes:\s*(?:#.*)?", lines[i].rstrip("\r\n"))]
    if len(volumes) != 1:
        raise ValueError("Expected one block-style volumes list")
    index = volumes[0]
    volume_indent = len(lines[index]) - len(lines[index].lstrip())
    first = index + 1
    while first < end and not lines[first].strip():
        first += 1
    if first == end or not lines[first].lstrip().startswith('- '):
        raise ValueError("Expected a block-style volume list item")
    item_indent = len(lines[first]) - len(lines[first].lstrip())
    if item_indent <= volume_indent:
        raise ValueError("Invalid volume indentation")
    newline = '\r\n' if '\r\n' in content else '\n'
    lines.insert(index + 1, ' ' * item_indent + '- ' + expected + newline)
    return ''.join(lines)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def atomic_write(path, data, mode):
    fd, temporary = tempfile.mkstemp(prefix=path.name + '.', dir=path.parent)
    try:
        with os.fdopen(fd, 'wb') as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def command(args):
    result = subprocess.run(args, capture_output=True, timeout=30)
    if result.returncode:
        # A Compose error may contain interpolated configuration: never print it.
        raise RuntimeError(f"Command failed ({result.returncode}): {args[0]}")
    return result.stdout


def install(compose, container, service, apply):
    compose = compose.resolve(strict=True)
    original_compose = compose.read_bytes()
    original_default = command(['docker', 'exec', container, 'cat', TARGET])
    repaired = repair_default_config(original_default.decode()).encode()
    filename = 'npm-fallback-static.conf'
    persisted = compose.parent / filename
    candidate_compose = add_compose_mount(original_compose.decode(), service, filename).encode()
    if persisted.exists() and persisted.read_bytes() != repaired:
        raise ValueError("The persistent configuration already contains other changes")
    identity = command(['docker', 'inspect', container, '--format', '{{.Id}}']).strip().decode()
    report = {'container': container, 'containerId': identity, 'composePath': str(compose),
              'defaultBeforeSha256': digest(original_default), 'defaultAfterSha256': digest(repaired),
              'composeChanged': candidate_compose != original_compose, 'applied': False}
    if not apply:
        return report
    if repaired == original_default and candidate_compose == original_compose and persisted.exists():
        command(['docker', 'exec', identity, 'nginx', '-t'])
        return {**report, 'applied': True, 'alreadyApplied': True}
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    backup = compose.parent / 'seo-maintenance-backups' / stamp
    backup.mkdir(parents=True, mode=0o700)
    os.chmod(backup.parent, 0o700)
    atomic_write(backup / 'default.conf', original_default, 0o600)
    atomic_write(backup / 'compose.yml', original_compose, 0o600)
    candidate = backup / 'candidate-default.conf'
    atomic_write(candidate, repaired, 0o644)
    validation_compose = compose.parent / ('.seo-compose-' + stamp + '.yml')
    existed = persisted.exists()
    copied = compose_written = False
    try:
        atomic_write(validation_compose, candidate_compose, 0o600)
        command(['docker', 'compose', '-f', str(validation_compose), 'config', '--quiet', '--no-interpolate'])
        if compose.read_bytes() != original_compose:
            raise RuntimeError("Compose changed during preparation")
        if command(['docker', 'exec', identity, 'cat', TARGET]) != original_default:
            raise RuntimeError("Nginx configuration changed during preparation")
        if command(['docker', 'inspect', container, '--format', '{{.Id}}']).strip().decode() != identity:
            raise RuntimeError("NPM was replaced during preparation")
        if repaired != original_default:
            copied = True
            command(['docker', 'cp', str(candidate), identity + ':' + TARGET])
        command(['docker', 'exec', identity, 'nginx', '-t'])
        atomic_write(persisted, repaired, 0o644)
        atomic_write(compose, candidate_compose, compose.stat().st_mode & 0o777)
        compose_written = True
        command(['docker', 'exec', identity, 'nginx', '-s', 'reload'])
        return {**report, 'applied': True, 'backupDirectory': str(backup),
                'persistentConfig': str(persisted), 'activation': 'graceful reload',
                'persistence': 'read-only bind mount on the next Compose recreation'}
    except Exception:
        if compose_written:
            if compose.read_bytes() != candidate_compose:
                raise RuntimeError("Concurrent Compose change prevents automatic rollback")
            shutil.copyfile(backup / 'compose.yml', compose)
        if copied:
            restore = backup / 'restore-default.conf'
            atomic_write(restore, original_default, 0o644)
            command(['docker', 'cp', str(restore), identity + ':' + TARGET])
            command(['docker', 'exec', identity, 'nginx', '-t'])
            command(['docker', 'exec', identity, 'nginx', '-s', 'reload'])
        if not existed and persisted.exists() and persisted.read_bytes() == repaired:
            persisted.unlink()
        raise
    finally:
        validation_compose.unlink(missing_ok=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--compose-file', type=Path, required=True)
    parser.add_argument('--container', default='nginx-proxy-manager')
    parser.add_argument('--service', default='nginx-proxy-manager')
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    print(json.dumps(install(args.compose_file, args.container, args.service, args.apply)))
