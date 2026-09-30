import assert from 'node:assert/strict';
import test from 'node:test';

import { confirmIncidents, runConfirmedProductionProbe } from './production-alert-monitor.mjs';

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
  });

  assert.equal(result.status, 'incident');
  assert.equal(result.incidents[0].incidentId, 'production-rollback');
  assert.equal(pauseCalls, 1);
});
