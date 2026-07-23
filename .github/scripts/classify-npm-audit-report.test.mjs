import assert from 'node:assert/strict';
import test from 'node:test';

import { classifyNpmAuditReport } from './classify-npm-audit-report.mjs';

test('classifies a report without high or critical vulnerabilities as clean', () => {
  const result = classifyNpmAuditReport(JSON.stringify({
    metadata: { vulnerabilities: { high: 0, critical: 0 } }
  }), 0);

  assert.equal(result.kind, 'clean');
});

test('classifies reported high vulnerabilities separately from scanner errors', () => {
  const result = classifyNpmAuditReport(JSON.stringify({
    metadata: { vulnerabilities: { high: 2, critical: 0 } }
  }), 1);

  assert.equal(result.kind, 'vulnerabilities');
});

test('classifies a registry error response as a scanner error', () => {
  const result = classifyNpmAuditReport(JSON.stringify({
    error: { code: 'E503', summary: 'Service Unavailable' }
  }), 1);

  assert.equal(result.kind, 'scan-error');
});

test('classifies invalid JSON and unexpected exit codes as scanner errors', () => {
  assert.equal(classifyNpmAuditReport('Service Unavailable', 1).kind, 'scan-error');
  assert.equal(classifyNpmAuditReport('{}', 2).kind, 'scan-error');
});
