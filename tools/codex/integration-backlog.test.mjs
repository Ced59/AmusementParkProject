import test from 'node:test';
import assert from 'node:assert/strict';
import { claimEntry, countryCounts, preserveHistory, recordIntegration, renderCountry, renderSummary, validateCountry } from './integration-backlog.mjs';

const fixture = () => ({ schemaVersion: 1, countryCode: 'BE', countryName: 'Belgique', inventoryDate: '2026-10-22', geographicScope: 'Europe', entries: [{ entryId: 'BE-example', name: 'Parc exemple', locality: 'Ville', entityKind: 'Park', siteId: 'park-1', editorialStatus: 'ToIntegrate', presence: 'Hidden', assignedTo: null, lastDataIntegrationAt: null, integrationHistory: [], sources: [], nextAction: 'Intégrer.' }] });
const options = () => ({ entry: 'BE-example', event: 'receipt-1', at: '2026-10-22T12:00:00Z', outcome: 'completed', entityId: 'park-1', kind: 'Park', evidence: 'receipt:verified-1', audit: 'audit:verified-1', summary: 'Inventaire et textes intégrés, audit final accepté.' });
const now = new Date('2026-10-22T18:00:00Z');

test('completion retains the entry and records an auditable UTC integration date', () => {
  const original = fixture();
  const result = recordIntegration(original, options(), now);
  assert.equal(result.entries.length, 1);
  assert.equal(result.entries[0].editorialStatus, 'Integrated');
  assert.equal(result.entries[0].lastDataIntegrationAt, options().at);
  assert.equal(result.entries[0].integrationHistory.length, 1);
  assert.equal(original.entries[0].lastDataIntegrationAt, null);
  assert.match(renderCountry(result), /2026-10-22T12:00:00Z/);
});

test('partial work updates the data date without declaring completion', () => {
  const result = recordIntegration(fixture(), { ...options(), outcome: 'partial', audit: undefined }, now);
  assert.equal(result.entries[0].editorialStatus, 'InProgress');
  assert.equal(result.entries[0].lastDataIntegrationAt, options().at);
});

test('same receipt is idempotent and conflicting reuse is rejected', () => {
  const first = recordIntegration(fixture(), options(), now);
  assert.deepEqual(recordIntegration(first, options(), now), first);
  assert.throws(() => recordIntegration(first, { ...options(), summary: 'Changed' }, now), /contenu différent/);
});

test('claiming preserves unknown integration date and prevents conflicting owners', () => {
  const claimed = claimEntry(fixture(), { entry: 'BE-example', owner: 'conversation-belgique' });
  assert.equal(claimed.entries[0].lastDataIntegrationAt, null);
  assert.throws(() => claimEntry(claimed, { entry: 'BE-example', owner: 'another' }), /Déjà attribué/);
});

test('completion requires final audit and an applied data receipt', () => {
  assert.throws(() => recordIntegration(fixture(), { ...options(), audit: undefined }, now), /audit/);
  assert.throws(() => recordIntegration(fixture(), { ...options(), evidence: '' }, now), /evidence/);
  assert.throws(() => recordIntegration(fixture(), { ...options(), at: '2027-10-22T12:00:00Z' }, now), /future/);
  assert.throws(() => recordIntegration(fixture(), { ...options(), at: '2026-02-30T12:00:00Z' }, now), /valide/);
});

test('refresh cannot erase entries, rewrite history, or invent an integration date', () => {
  const first = recordIntegration(fixture(), options(), now);
  assert.throws(() => preserveHistory(first, { ...first, entries: [] }), /Suppression interdite/);
  const rewrite = structuredClone(first);
  rewrite.entries[0].integrationHistory[0].summary = 'Rewritten';
  assert.throws(() => preserveHistory(first, rewrite), /réécrit/);
  const invented = fixture();
  invented.entries[0].lastDataIntegrationAt = options().at;
  assert.throws(() => validateCountry(invented), /ne correspond pas/);
});

test('standalone migration retains the stable backlog ID and the legacy site ID', () => {
  const result = recordIntegration(fixture(), { ...options(), kind: 'StandaloneAttraction', entityId: 'standalone-1' }, now);
  assert.equal(result.entries[0].entryId, 'BE-example');
  assert.equal(result.entries[0].entityKind, 'StandaloneAttraction');
  assert.equal(result.entries[0].siteId, 'standalone-1');
  assert.deepEqual(result.entries[0].legacySiteIds, ['park-1']);
});

test('subsequent integrations append history and reject an older last date', () => {
  const first = recordIntegration(fixture(), options(), now);
  const second = recordIntegration(first, { ...options(), event: 'receipt-2', at: '2026-10-22T13:00:00Z' }, now);
  assert.equal(second.entries[0].integrationHistory.length, 2);
  assert.equal(second.entries[0].lastDataIntegrationAt, '2026-10-22T13:00:00Z');
  assert.throws(() => recordIntegration(second, { ...options(), event: 'receipt-3' }, now), /ancienne/);
});

test('counts separate hidden shells, migrated legacy, candidates and uncertain identities', () => {
  const doc = fixture();
  const base = doc.entries[0];
  doc.entries.push(
    { ...base, entryId: 'legacy', editorialStatus: 'Duplicate' },
    { ...base, entryId: 'review', editorialStatus: 'Excluded', siteReviewStatusAtInventory: 'NotRelevant' },
    { ...base, entryId: 'new-park', siteId: null, presence: 'NotFound', proposedEntityKind: 'Park', editorialStatus: 'ToQualify' },
    { ...base, entryId: 'new-standalone', siteId: null, presence: 'NotFound', entityKind: 'ToQualify', proposedEntityKind: 'StandaloneAttraction', editorialStatus: 'ToQualify' },
    { ...base, entryId: 'uncertain', siteId: null, presence: 'PossibleExisting', proposedEntityKind: 'Park', editorialStatus: 'ToQualify' },
  );
  const counts = countryCounts(doc);
  assert.equal(counts.parkHidden, 1);
  assert.equal(counts.parkReview, 1);
  assert.equal(counts.parkAbsent, 1);
  assert.equal(counts.standaloneAbsent, 1);
  assert.equal(counts.possibleExisting, 1);
  assert.equal(counts.legacy, 1);
  assert.match(renderSummary([doc]), /Belgique/);
});

test('newly created and completed hidden entries leave the work remaining counts', () => {
  const doc = fixture();
  doc.entries[0].siteId = null;
  doc.entries[0].presence = 'NotFound';
  doc.entries[0].proposedEntityKind = 'Park';
  const created = recordIntegration(doc, options(), now);
  assert.equal(countryCounts(created).parkAbsent, 0);
  assert.equal(countryCounts(created).visibilityUnknown, 1);
  const hidden = recordIntegration(fixture(), options(), now);
  assert.equal(countryCounts(hidden).parkHidden, 0);
  assert.equal(countryCounts(hidden).integratedHidden, 1);
});

test('a later partial integration cannot be declared completed using an older audit', () => {
  const completed = recordIntegration(fixture(), options(), now);
  const partial = recordIntegration(completed, { ...options(), event: 'later', at: '2026-10-22T13:00:00Z', outcome: 'partial', audit: undefined }, now);
  partial.entries[0].editorialStatus = 'Integrated';
  assert.throws(() => validateCountry(partial), /dernier audit/);
});
