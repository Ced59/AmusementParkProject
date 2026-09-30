import assert from 'node:assert/strict';
import test from 'node:test';

import {
  requiredIncidentIds,
  requiredRunbookHeadings,
  isIncidentRunbookPath,
  validateCatalogStructure,
  validateMonitorConfig,
  validateRunbookContent,
} from './check-incident-readiness.mjs';

function incident(id, overrides = {}) {
  return {
    id,
    title: id,
    product: 'QUAL',
    severity: 'high',
    owner: 'operations',
    detectionMode: 'support-escalation',
    signal: 'signal',
    threshold: 'threshold',
    window: 'window',
    runbook: `docs/operations/incidents/${id}.md`,
    fallback: 'fallback',
    recoveryProof: 'proof',
    ...overrides,
  };
}

test('accepts a complete incident catalog and monitor mapping', () => {
  const incidents = requiredIncidentIds.map((id) => incident(id, id === 'production-rollback' ? { detectionMode: 'automated' } : {}));
  const catalog = { schemaVersion: 1, incidents };
  const config = {
    confirmationAttempts: 2,
    confirmationDelayMilliseconds: 10000,
    baseline: { targets: [{ key: 'health', kind: 'api', incidentId: 'production-rollback' }] },
  };

  assert.deepEqual(validateCatalogStructure(catalog), []);
  assert.deepEqual(validateMonitorConfig(config, catalog), []);
});

test('rejects a missing incident and a monitor bound to a manual runbook', () => {
  const incidents = requiredIncidentIds.slice(1).map((id) => incident(id));
  const catalog = { schemaVersion: 1, incidents };
  const config = {
    confirmationAttempts: 1,
    confirmationDelayMilliseconds: 5000,
    baseline: { targets: [{ key: 'health', kind: 'api', incidentId: 'orphaned-visit' }] },
  };

  assert.ok(validateCatalogStructure(catalog).some((error) => error.includes('ranking-inconsistent')));
  assert.equal(validateMonitorConfig(config, catalog).length, 3);
});

test('requires every recovery section in each runbook', () => {
  const fullContent = requiredRunbookHeadings.join('\nContenu\n');
  assert.deepEqual(validateRunbookContent('incident', fullContent), []);
  assert.deepEqual(
    validateRunbookContent('incident', fullContent.replace('## Vérification', '## Contrôle')),
    ['Runbook incident: section absente ## Vérification.'],
  );
});

test('rejects a runbook path that escapes the incident directory after normalization', () => {
  assert.equal(isIncidentRunbookPath('docs/operations/incidents/privacy-incident.md'), true);
  assert.equal(isIncidentRunbookPath('docs/operations/incidents/../../roadmaps/example.md'), false);
  assert.equal(isIncidentRunbookPath('docs/operations/incidents'), false);
});
