import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

import {
  computeDocumentsDigest,
  parseDocumentShape,
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

test('the persisted schema digest includes type, nullability and BSON mapping', () => {
  const baseline = parseDocumentShape(`
    public sealed class ExampleDocument
    {
      [BsonElement("email")]
      public string? Email { get; set; }
    }
  `, 'ExampleDocument.cs');
  const changedType = parseDocumentShape(`
    public sealed class ExampleDocument
    {
      [BsonElement("email")]
      public string Email { get; set; } = string.Empty;
    }
  `, 'ExampleDocument.cs');
  const changedMapping = parseDocumentShape(`
    public sealed class ExampleDocument
    {
      [BsonElement("contactEmail")]
      public string? Email { get; set; }
    }
  `, 'ExampleDocument.cs');

  const baselineDigest = computeDocumentsDigest([baseline]);

  assert.notEqual(computeDocumentsDigest([changedType]), baselineDigest);
  assert.notEqual(computeDocumentsDigest([changedMapping]), baselineDigest);
});
