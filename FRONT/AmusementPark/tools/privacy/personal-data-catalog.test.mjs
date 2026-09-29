import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

import {
  personalDataCatalogPath,
  validateCatalog,
} from './check-personal-data-catalog.mjs';

function readCatalog() {
  return JSON.parse(readFileSync(personalDataCatalogPath, 'utf8'));
}

test('the reviewed personal-data catalog matches every tracked document field', () => {
  const result = validateCatalog(readCatalog());

  assert.deepEqual(result.errors, []);
  assert.ok(result.documentCount >= 100);
  assert.ok(result.fieldCount >= 1000);
});

test('a missing privacy dimension fails validation', () => {
  const catalog = readCatalog();
  delete catalog.surfaces[0].policy.deletion;

  const result = validateCatalog(catalog);

  assert.ok(result.errors.some((error) => error.includes('deletion')));
});

test('an unreviewed persisted shape fails validation', () => {
  const catalog = readCatalog();
  catalog.surfaces[0].reviewedSchemaSha256 = 'unreviewed';

  const result = validateCatalog(catalog);

  assert.ok(result.errors.some((error) => error.includes('forme persistée a changé')));
});
