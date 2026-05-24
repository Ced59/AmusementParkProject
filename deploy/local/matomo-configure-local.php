<?php

declare(strict_types=1);

$configPath = '/var/www/html/config/config.ini.php';

if (!is_file($configPath)) {
    fwrite(STDOUT, "Matomo config file not found yet. Skipping trusted hosts update.\n");
    exit(0);
}

$content = file_get_contents($configPath);
if ($content === false) {
    fwrite(STDERR, "Unable to read Matomo config file.\n");
    exit(1);
}

$matomoHttpPort = getenv('MATOMO_HTTP_PORT') ?: '18082';
$npmHttpPort = getenv('NPM_HTTP_PORT') ?: '18080';

$trustedHosts = [
    'localhost:' . $matomoHttpPort,
    '127.0.0.1:' . $matomoHttpPort,
    'matomo.amusement.localhost',
    'matomo.amusement.localhost:' . $npmHttpPort,
];

$content = preg_replace('/^\s*trusted_hosts\[\]\s*=.*$/m', '', $content) ?? $content;
$content = preg_replace('/\n{3,}/', "\n\n", $content) ?? $content;

$trustedHostsBlock = implode("\n", array_map(
    static fn (string $host): string => 'trusted_hosts[] = "' . $host . '"',
    $trustedHosts
));

if (preg_match('/^\[General\]\s*$/m', $content) === 1) {
    $content = preg_replace('/^\[General\]\s*$/m', "[General]\n" . $trustedHostsBlock, $content, 1) ?? $content;
} else {
    $content = "[General]\n" . $trustedHostsBlock . "\n\n" . $content;
}

if (file_put_contents($configPath, $content) === false) {
    fwrite(STDERR, "Unable to write Matomo config file.\n");
    exit(1);
}

fwrite(STDOUT, "Matomo trusted hosts updated for local-prod.\n");
