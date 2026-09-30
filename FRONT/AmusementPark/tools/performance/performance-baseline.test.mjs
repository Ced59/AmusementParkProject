import assert from 'node:assert/strict';
import test from 'node:test';

import {
  classifyHttpOutcome,
  evaluateSummary,
  nearestRankPercentile,
  summarizeSamples,
} from './performance-baseline.mjs';

test('classifies stable HTTP outcome families', () => {
  assert.equal(classifyHttpOutcome(200), 'success');
  assert.equal(classifyHttpOutcome(302), 'redirection');
  assert.equal(classifyHttpOutcome(404), 'client_error');
  assert.equal(classifyHttpOutcome(503), 'server_error');
});

test('uses the nearest-rank percentile without interpolating observed timings', () => {
  assert.equal(nearestRankPercentile([40, 10, 30, 20, 50], 50), 30);
  assert.equal(nearestRankPercentile([40, 10, 30, 20, 50], 95), 50);
  assert.equal(nearestRankPercentile([], 95), null);
});

test('summarizes errors separately and evaluates explicit budgets', () => {
  const target = {
    key: 'home',
    path: '/fr/home',
    kind: 'ssr-page',
    expectedStatus: 200,
    maximumP95Milliseconds: 100,
    maximumBytes: 1000,
  };
  const summary = summarizeSamples(target, [
    { status: 200, outcome: 'success', durationMilliseconds: 45, bytes: 800, buildVersion: '5.4.21', ssrMode: 'SSR_RENDERED', seoReady: 'true' },
    { status: 503, outcome: 'server_error', durationMilliseconds: 20, bytes: 100, buildVersion: '5.4.21', ssrMode: null, seoReady: null },
  ]);

  assert.equal(summary.p50Milliseconds, 45);
  assert.deepEqual(summary.statuses, { 200: 1, 503: 1 });
  assert.deepEqual(summary.outcomes, { success: 1, server_error: 1 });
  assert.equal(summary.missingSsrModeCount, 1);
  assert.deepEqual(summary.seoReadiness, ['true']);
  assert.equal(summary.missingSeoReadyCount, 1);
  assert.deepEqual(evaluateSummary(target, summary), ['1 réponse(s) avec un statut inattendu']);
});
