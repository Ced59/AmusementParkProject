import assert from 'node:assert/strict';
import test from 'node:test';

import {
  appendSsrModeChecks,
  confirmIncidents,
  extractLazyRouteAssetUrl,
  extractRequiredClientAssetUrls,
  extractStaticModuleAssetUrls,
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

test('rejects a CSR fallback and a missing SSR mode for crawler-facing targets', () => {
  const ssrConfig = {
    baseline: {
      targets: [{
        key: 'home',
        kind: 'ssr-page',
        allowedSsrModes: ['SSR_CACHE_HIT', 'SSR_RENDERED'],
      }],
    },
  };
  const fallbackReport = {
    results: [{ key: 'home', ssrModes: ['CSR_FALLBACK'], failures: [] }],
  };
  const missingReport = {
    results: [{ key: 'home', ssrModes: [], failures: [] }],
  };
  const intermittentlyMissingReport = {
    results: [{
      key: 'home',
      ssrModes: ['SSR_RENDERED'],
      missingSsrModeCount: 1,
      failures: [],
    }],
  };

  appendSsrModeChecks(fallbackReport, ssrConfig);
  appendSsrModeChecks(missingReport, ssrConfig);
  appendSsrModeChecks(intermittentlyMissingReport, ssrConfig);

  assert.deepEqual(fallbackReport.results[0].failures, ['mode SSR inattendu: CSR_FALLBACK']);
  assert.deepEqual(missingReport.results[0].failures, ['mode SSR absent de la réponse publique']);
  assert.deepEqual(intermittentlyMissingReport.results[0].failures, ['mode SSR absent de la réponse publique']);
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

  assert.equal(result.assetCount, 0);
  assert.deepEqual(result.failures, ['/main.js: bundle client invalide']);
});

test('finds and verifies the lazy-loaded chunk for the exact client route', async () => {
  const mainSource = 'const routes=[{path:"park-fit",loadComponent:()=>import("./chunk-PARK.js").then(i=>i.ParkFitStartPageComponent)}];export{routes};';
  assert.equal(
    extractLazyRouteAssetUrl('https://amusement-parks.fun/main.js', mainSource, 'park-fit'),
    'https://amusement-parks.fun/chunk-PARK.js',
  );

  const responses = new Map([
    ['https://amusement-parks.fun/fr/park-fit', new Response('<base href="/"><script src="main.js"></script>', {
      status: 200,
      headers: { 'content-type': 'text/html' },
    })],
    ['https://amusement-parks.fun/main.js', new Response(mainSource, {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
    ['https://amusement-parks.fun/chunk-PARK.js', new Response('import{Evidence}from"./chunk-EVIDENCE.js";export class ParkFitStartPageComponent {}', {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
    ['https://amusement-parks.fun/chunk-EVIDENCE.js', new Response('', {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
  ]);
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200, clientRoutePath: 'park-fit' },
    async (url) => responses.get(String(url)),
  );

  assert.equal(result.assetCount, 3);
  assert.equal(result.lazyRouteAsset, '/chunk-PARK.js');
  assert.deepEqual(result.failures, []);
});

test('extracts static same-origin imports without following unrelated dynamic routes', () => {
  const source = `
    import{One}from"./chunk-ONE.js";
    import "./chunk-TWO.js";
    const lazy=()=>import("./chunk-OTHER-ROUTE.js");
    export{Three}from"https://cdn.example/chunk-THREE.js";`;

  assert.deepEqual(
    extractStaticModuleAssetUrls('https://amusement-parks.fun/chunk-PARK.js', source),
    [
      'https://amusement-parks.fun/chunk-ONE.js',
      'https://amusement-parks.fun/chunk-TWO.js',
    ],
  );
});

test('reports a missing transitive static import from the Park Fit route chunk', async () => {
  const responses = new Map([
    ['https://amusement-parks.fun/fr/park-fit', new Response('<base href="/"><script src="main.js"></script>', {
      status: 200,
      headers: { 'content-type': 'text/html' },
    })],
    ['https://amusement-parks.fun/main.js', new Response('const routes=[{path:"park-fit",loadComponent:()=>import("./chunk-PARK.js")}];export{routes};', {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
    ['https://amusement-parks.fun/chunk-PARK.js', new Response('import{Missing}from"./chunk-MISSING.js";', {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
    ['https://amusement-parks.fun/chunk-MISSING.js', new Response('missing', {
      status: 404,
      headers: { 'content-type': 'text/html' },
    })],
  ]);
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200, clientRoutePath: 'park-fit' },
    async (url) => responses.get(String(url)),
  );

  assert.deepEqual(result.failures, ['/chunk-MISSING.js: bundle client invalide']);
});

test('rejects a malformed client bundle even when HTTP and MIME type are valid', async () => {
  const responses = new Map([
    ['https://amusement-parks.fun/fr/park-fit', new Response('<base href="/"><script src="main.js"></script>', {
      status: 200,
      headers: { 'content-type': 'text/html' },
    })],
    ['https://amusement-parks.fun/main.js', new Response('export const broken = ;', {
      status: 200,
      headers: { 'content-type': 'text/javascript' },
    })],
  ]);
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200 },
    async (url) => responses.get(String(url)),
  );

  assert.equal(result.assetCount, 0);
  assert.deepEqual(result.failures, ['/main.js: syntaxe JavaScript invalide']);
});

test('bounds a stalled CSR shell request with the configured timeout', async () => {
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200, clientRoutePath: 'park-fit' },
    async (_url, options) => new Promise((_resolve, reject) => {
      options.signal.addEventListener('abort', () => reject(new Error('aborted')));
    }),
    5,
  );

  assert.deepEqual(result.failures, ['page cliente inaccessible ou expirée pendant le contrôle des bundles']);
});

test('keeps the timeout active while reading a stalled response body', async () => {
  const result = await probeRequiredClientAssets(
    'https://amusement-parks.fun',
    { path: '/fr/park-fit', expectedStatus: 200, clientRoutePath: 'park-fit' },
    async (_url, options) => ({
      status: 200,
      text: () => new Promise((_resolve, reject) => {
        options.signal.addEventListener('abort', () => reject(new Error('body aborted')));
      }),
    }),
    5,
  );

  assert.deepEqual(result.failures, ['page cliente inaccessible ou expirée pendant le contrôle des bundles']);
});
