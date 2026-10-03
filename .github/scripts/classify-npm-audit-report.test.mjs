import assert from 'node:assert/strict';
import test from 'node:test';

import { classifyNpmAuditReport } from './classify-npm-audit-report.mjs';

const exceptionPolicy = JSON.stringify({
  exceptions: [{
    advisorySource: 1240991,
    ghsaId: 'GHSA-ch52-4w7c-c8xp',
    package: 'http-cache-semantics',
    severity: 'high',
    scope: 'development-only',
    expiresOn: '2026-10-17',
    reason: 'No patched upstream release is available.'
  }]
});

function createHighReport({ severity = 'high', source = 1240991 } = {}) {
  return JSON.stringify({
    vulnerabilities: {
      '@angular/cli': {
        severity,
        via: ['make-fetch-happen'],
        nodes: ['node_modules/@angular/cli']
      },
      'make-fetch-happen': {
        severity,
        via: ['http-cache-semantics'],
        nodes: ['node_modules/make-fetch-happen']
      },
      'http-cache-semantics': {
        severity,
        via: [{
          source,
          name: 'http-cache-semantics',
          severity,
          url: source === 1240991
            ? 'https://github.com/advisories/GHSA-ch52-4w7c-c8xp'
            : 'https://github.com/advisories/GHSA-xxxx-yyyy-zzzz'
        }],
        nodes: ['node_modules/http-cache-semantics']
      }
    },
    metadata: { vulnerabilities: { high: severity === 'high' ? 3 : 0, critical: severity === 'critical' ? 3 : 0 } }
  });
}

function createPackageLock(overrides = {}) {
  return JSON.stringify({
    packages: {
      'node_modules/@angular/cli': { dev: true },
      'node_modules/make-fetch-happen': { dev: true },
      'node_modules/http-cache-semantics': { dev: true },
      ...overrides
    }
  });
}

test('classifies a report without high or critical vulnerabilities as clean', () => {
  const result = classifyNpmAuditReport(JSON.stringify({
    metadata: { vulnerabilities: { high: 0, critical: 0 } }
  }), 0);

  assert.equal(result.kind, 'clean');
});

test('classifies reported high vulnerabilities separately from scanner errors', () => {
  const result = classifyNpmAuditReport(createHighReport({ source: 9999999 }), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock(),
    currentDate: '2026-10-03'
  });

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

test('allows one active advisory only when its complete chain is development-only', () => {
  const result = classifyNpmAuditReport(createHighReport(), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock(),
    currentDate: '2026-10-03'
  });

  assert.equal(result.kind, 'clean-with-exceptions');
  assert.match(result.message, /GHSA-ch52-4w7c-c8xp/);
});

test('blocks an excepted advisory when one affected node is not development-only', () => {
  const result = classifyNpmAuditReport(createHighReport(), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock({ 'node_modules/make-fetch-happen': { dev: false } }),
    currentDate: '2026-10-03'
  });

  assert.equal(result.kind, 'vulnerabilities');
});

test('blocks an expired exception', () => {
  const result = classifyNpmAuditReport(createHighReport(), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock(),
    currentDate: '2026-10-18'
  });

  assert.equal(result.kind, 'vulnerabilities');
});

test('blocks a different high advisory', () => {
  const result = classifyNpmAuditReport(createHighReport({ source: 9999999 }), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock(),
    currentDate: '2026-10-03'
  });

  assert.equal(result.kind, 'vulnerabilities');
});

test('never applies a high-severity exception to a critical vulnerability', () => {
  const result = classifyNpmAuditReport(createHighReport({ severity: 'critical' }), 1, {
    exceptionsText: exceptionPolicy,
    packageLockText: createPackageLock(),
    currentDate: '2026-10-03'
  });

  assert.equal(result.kind, 'vulnerabilities');
});
