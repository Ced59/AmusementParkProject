import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    'npm_fallback', Path(__file__).resolve().parents[1] / 'repair-npm-fallback.py')
repair = importlib.util.module_from_spec(spec)
spec.loader.exec_module(repair)

DEFAULT = '''server {
    listen 80;
    set $server "127.0.0.1";
    set $port "80";
    server_name localhost-nginx-proxy-manager;
    include conf.d/include/assets.conf;
    include conf.d/include/block-exploits.conf;
    include conf.d/include/letsencrypt-acme-challenge.conf;
    location / {
        root /var/www/html;
        index index.html;
    }
}
server {
    listen 443 ssl;
    include conf.d/include/ssl-ciphers.conf;
    ssl_reject_handshake on;
    return 444;
}
'''
COMPOSE = '''services:
  nginx-proxy-manager:
    image: jc21/nginx-proxy-manager:latest
    environment:
      EXAMPLE: unchanged
    volumes:
      - ./data:/data
      - ./letsencrypt:/etc/letsencrypt
  another-service:
    volumes:
      - ./other:/data
networks:
  default:
    external: true
'''


class FallbackConfigurationTests(unittest.TestCase):
    def test_removes_only_recursive_asset_location_and_preserves_security(self):
        fixed = repair.repair_default_config(DEFAULT)
        self.assertNotIn('include conf.d/include/assets.conf;', fixed)
        self.assertEqual(DEFAULT.replace('include conf.d/include/assets.conf;', repair.MARKER), fixed)
        self.assertEqual(fixed, repair.repair_default_config(fixed))

    def test_refuses_changed_or_ambiguous_fallback_configuration(self):
        for candidate in [DEFAULT.replace('127.0.0.1', 'backend'),
                          DEFAULT + 'include conf.d/include/assets.conf;\n',
                          DEFAULT.replace('include conf.d/include/assets.conf;', '')]:
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                repair.repair_default_config(candidate)

    def test_adds_read_only_mount_only_to_selected_service(self):
        fixed = repair.add_compose_mount(COMPOSE, 'nginx-proxy-manager', 'npm-fallback-static.conf')
        mount = '      - ./npm-fallback-static.conf:/etc/nginx/conf.d/default.conf:ro\n'
        self.assertEqual(COMPOSE.replace('    volumes:\n', '    volumes:\n' + mount, 1), fixed)
        self.assertEqual(fixed, repair.add_compose_mount(fixed, 'nginx-proxy-manager', 'npm-fallback-static.conf'))

    def test_preserves_crlf_and_refuses_unknown_mount_or_inline_list(self):
        windows = COMPOSE.replace('\n', '\r\n')
        fixed = repair.add_compose_mount(windows, 'nginx-proxy-manager', 'npm-fallback-static.conf')
        self.assertNotIn('\n', fixed.replace('\r\n', ''))
        for candidate in [COMPOSE.replace('    volumes:', '    volumes: []', 1),
                          COMPOSE.replace('./data:/data', './custom:/etc/nginx/conf.d/default.conf:rw')]:
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                repair.add_compose_mount(candidate, 'nginx-proxy-manager', 'npm-fallback-static.conf')


class FallbackInstallationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.compose = self.directory / 'compose.yml'
        self.compose.write_text(COMPOSE)
        self.live = DEFAULT.encode()
        self.commands = []

    def command(self, args):
        self.commands.append(args)
        if args[1] == 'inspect':
            return b'container-id\n'
        if args[1] == 'cp':
            self.live = Path(args[2]).read_bytes()
        if args[1] == 'exec' and args[3] == 'cat':
            return self.live
        return b''

    def test_dry_run_does_not_write_or_reload(self):
        with patch.object(repair, 'command', self.command):
            result = repair.install(self.compose, 'npm', 'nginx-proxy-manager', False)
        self.assertFalse(result['applied'])
        self.assertEqual([self.compose], list(self.directory.iterdir()))
        self.assertFalse(any('reload' in c for c in self.commands))

    def test_install_is_persistent_and_only_gracefully_reloads(self):
        with patch.object(repair, 'command', self.command):
            result = repair.install(self.compose, 'npm', 'nginx-proxy-manager', True)
            again = repair.install(self.compose, 'npm', 'nginx-proxy-manager', True)
        self.assertTrue(result['applied'])
        self.assertTrue(again['alreadyApplied'])
        self.assertIn(':ro', self.compose.read_text())
        self.assertEqual(self.live, (self.directory / 'npm-fallback-static.conf').read_bytes())
        self.assertEqual(1, sum('reload' in c for c in self.commands))
        self.assertFalse(any(c[1] in ['restart', 'stop', 'rm'] for c in self.commands))

    def test_validation_failure_restores_live_file_and_preserves_compose(self):
        failed = False
        def reject_candidate(args):
            nonlocal failed
            result = self.command(args)
            if args[-1] == '-t' and not failed:
                failed = True
                raise RuntimeError('injected invalid configuration')
            return result
        with patch.object(repair, 'command', reject_candidate), self.assertRaises(RuntimeError):
            repair.install(self.compose, 'npm', 'nginx-proxy-manager', True)
        self.assertEqual(DEFAULT.encode(), self.live)
        self.assertEqual(COMPOSE, self.compose.read_text())
        self.assertFalse((self.directory / 'npm-fallback-static.conf').exists())

    def test_reload_failure_restores_both_configurations(self):
        failed = False
        def reject_reload(args):
            nonlocal failed
            result = self.command(args)
            if args[-1] == 'reload' and not failed:
                failed = True
                raise RuntimeError('injected reload failure')
            return result
        with patch.object(repair, 'command', reject_reload), self.assertRaises(RuntimeError):
            repair.install(self.compose, 'npm', 'nginx-proxy-manager', True)
        self.assertEqual(DEFAULT.encode(), self.live)
        self.assertEqual(COMPOSE, self.compose.read_text())
        self.assertFalse((self.directory / 'npm-fallback-static.conf').exists())

    def test_existing_operator_configuration_is_never_overwritten(self):
        persistent = self.directory / 'npm-fallback-static.conf'
        persistent.write_text('operator configuration')
        with patch.object(repair, 'command', self.command), self.assertRaises(ValueError):
            repair.install(self.compose, 'npm', 'nginx-proxy-manager', True)
        self.assertEqual('operator configuration', persistent.read_text())
        self.assertFalse(any(c[1] == 'cp' for c in self.commands))


if __name__ == '__main__':
    unittest.main()
