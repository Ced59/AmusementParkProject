import assert from 'node:assert/strict';
import test from 'node:test';

import {
  confirmIncidents,
  extractRequiredClientAssetUrls,
  probeRequiredClientAssets,
  runConfirmedProductionProbe,
} from './production-alert-monitor.mjs';

const config = {
  confirmationAttempts: 2,
  confirmationDelayMilliseconds: 10000,
  baseline: {
    targets: [{ key: 'api-health', path: '/api/health', incidentId: 'production-rollback' }],
  },
};

function report(failures) {
  return {
    measuredAtUtc: '2026-09-30T00:00:00.000Z',
    results: [{
      key: 'api-health',
      failures,
      statuses: failures.length === 0 ? { 200: 2 } : { 503: 2 },
      outcomes: failures.length === 0 ? { success: 2 } : { server_error: 2 },
      p95Milliseconds: 20,
    }],
  };
}

test('confirms only a target failing every required attempt', () => {
  assert.equal(confirmIncidents(config, [report(['HTTP 503']), report(['HTTP 503'])]).length, 1);
  assert.deepEqual(confirmIncidents(config, [report(['HTTP 503']), report([])]), []);
});

test('does not wait for confirmation when the first probe is healthy', async () => {
  let probeCalls = 0;
  let pauseCalls = 0;
  const result = await runConfirmedProductionProbe(config, {
    runBaseline: async () => {
      probeCalls += 1;
      return report([]);
    },
    pause: async () => {
      pauseCalls += 1;
    },
    skipClientAssetChecks: true,
  });

  assert.equal(result.status, 'healthy');
  assert.equal(probeCalls, 1);
  assert.equal(pauseCalls, 0);
});

test('waits and reports an incident after two failed probes', async () => {
  let pauseCalls = 0;
  const result = await runConfirmedProductionProbe(config, {
    runBaseline: async () => report(['HTTP 503']),
    pause: async (milliseconds) => {
      assert.equal(milliseconds, 10000);
      pauseCalls += 1;
    },
    skipClientAssetChecks: true,
  });

  assert.equal(result.status, 'incident');
  assert.equal(result.incidents[0].incidentId, 'production-rollback');
  assert.equal(pauseCalls, 1);
});

test('extracts only unique same-origin scripts and module preloads', () => {
  const html = `
    <base href="/">
    <script src="main-ABC.js" type="module"></script>
    <link rel="modulepreload" href="/chunk-ONE.js">
    <script src="https://accounts.example/client.js"></script>
    <link rel="stylesheet" href="styles.css">
    <script src="main-ABC.js"></script>`;

  assert.deepEqual(
    extractRequiredClientAssetUrls('https://amusement-parks.fun/fr/park-fit', html),
    [
      'https://amusement-parks.fun/main-ABC.js',
      'https://amusement-parks.fun/chunk-ONE.js',
    ],
  );
});

test('reports a missing client bundle even when the CSR shell returns HTTP 200', async () => {
  const responses = new Map([
    ['https://amusement-parks.fun/fr/park-fit', new Response('<base href="/"><script src="main.js"></script>', {
      status: 200,
      headers: { 'content-type': 'text/html' },
    })],
    ['https://amusement-parks.fun/main.js', new Response('missing', {
      status: 404,
      headers: { 'content-type': 'text/html' },
    })],
  ]);
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200 },
    async (url) => responses.get(String(url)),
  );

  assert.equal(result.assetCount, 1);
  assert.deepEqual(result.failures, ['/main.js: bundle client invalide']);
});
