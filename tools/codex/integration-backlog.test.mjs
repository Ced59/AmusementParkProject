import test from 'node:test';
import assert from 'node:assert/strict';
import { claimEntry, countryCounts, preserveHistory, preserveRegistryHistory, recordIntegration, renderCountry, renderCountryPages, renderSummary, validateCountry } from './integration-backlog.mjs';

const fixture = () => ({ schemaVersion: 1, countryCode: 'BE', countryName: 'Belgique', inventoryDate: '2026-10-22', geographicScope: 'Europe', entries: [{ entryId: 'BE-example', name: 'Parc exemple', locality: 'Ville', entityKind: 'Park', proposedEntityKind: 'Park', siteId: 'park-1', editorialStatus: 'ToIntegrate', presence: 'Hidden', assignedTo: null, lastDataIntegrationAt: null, integrationHistory: [], sources: [], nextAction: 'Intégrer.' }] });
const options = () => ({ entry: 'BE-example', event: 'receipt-1', at: '2026-10-22T12:00:00Z', outcome: 'completed', entityId: 'park-1', kind: 'Park', evidence: 'receipt:verified-1', audit: 'audit:verified-1', summary: 'Inventaire et textes intégrés, audit final accepté.' });
const now = new Date('2026-10-22T18:00:00Z');

test('standalone investigations distinguish Park subsets, uncertain aliases and internal rides', () => {
  const doc = fixture();
  const base = doc.entries[0];
  base.standaloneAssessment = { relation: 'ParentToResolve', attractions: [] };
  doc.entries.push(
    { ...base, entryId: 'published-parent', presence: 'Published' },
    { ...base, entryId: 'uncertain-standalone', entityKind: 'ToQualify', siteId: null, presence: 'PossibleExisting', proposedEntityKind: 'StandaloneAttraction' },
    { ...base, entryId: 'new-standalone', entityKind: 'ToQualify', siteId: null, presence: 'NotFound', proposedEntityKind: 'StandaloneAttraction', standaloneAssessment: { relation: 'IndependentCandidate', attractions: [] } },
    { ...base, entryId: 'real-park', standaloneAssessment: { relation: 'InsidePark', attractions: [] } },
  );
  const counts = countryCounts(doc);
  assert.equal(counts.standaloneLegacyHidden, 1);
  assert.equal(counts.standaloneLegacyPublished, 1);
  assert.equal(counts.standaloneUnresolved, 1);
  assert.equal(counts.standaloneAbsent, 1);
  assert.equal(counts.standaloneInsidePark, 1);
  assert.equal(counts.parkHidden, 2);
  assert.match(renderCountry(doc), /pistes dans des fiches Park masquées/);
});

test('resolving a standalone investigation updates classification and retains evidence', () => {
  const doc = fixture();
  doc.entries[0].standaloneAssessment = { relation: 'IndependentCandidate', attractions: [{ name: 'Luge', source: 'Constructeur', url: 'https://example.org/ride' }] };
  const migrated = recordIntegration(doc, { ...options(), kind: 'StandaloneAttraction', entityId: 'standalone-1' }, now);
  assert.equal(migrated.entries[0].standaloneAssessment.relation, 'AlreadyStandalone');
  assert.equal(migrated.entries[0].standaloneAssessment.attractions.length, 1);
  assert.equal(countryCounts(migrated).standaloneLegacyHidden, 0);
  const confirmedPark = recordIntegration(doc, { ...options(), outcome: 'partial' }, now);
  assert.equal(confirmedPark.entries[0].standaloneAssessment.relation, 'InsidePark');
  assert.equal(countryCounts(confirmedPark).standaloneLegacyHidden, 0);
});

test('inconsistent standalone classifications cannot inflate creation counts', () => {
  const doc = fixture();
  doc.entries[0].standaloneAssessment = { relation: 'InsidePark', attractions: [] };
  doc.entries[0].proposedEntityKind = 'StandaloneAttraction';
  assert.throws(() => validateCountry(doc), /Attraction interne/);
  doc.entries[0].proposedEntityKind = 'Park';
  doc.entries[0].standaloneAssessment.relation = 'AlreadyStandalone';
  assert.throws(() => validateCountry(doc), /incohérents/);
});

test('missing-country proposals remain distinct from corrections of populated country codes', () => {
  const doc = fixture();
  doc.entries[0].countryResolution = { countryCode: 'BE' };
  doc.entries[0].countryCodeAtInventory = null;
  doc.entries.push({ ...doc.entries[0], entryId: 'country-correction', countryCodeAtInventory: 'FR' });
  assert.equal(countryCounts(doc).countryResolvedBySource, 1);
  assert.equal(countryCounts(doc).countryCorrectedBySource, 1);
});

test('country correction preserves stable identity and integration history worldwide', () => {
  const unresolved = recordIntegration(fixture(), options(), now);
  unresolved.countryCode = 'ZZ';
  unresolved.entries[0].entryId = 'ZZ-original-id';
  const destination = { ...fixture(), entries: [] };
  const previous = [unresolved, destination];
  const next = [{ ...unresolved, entries: [] }, { ...destination, entries: structuredClone(unresolved.entries) }];
  assert.doesNotThrow(() => preserveRegistryHistory(previous, next));
  const rewrite = structuredClone(next);
  rewrite[1].entries[0].integrationHistory[0].summary = 'Rewritten during move';
  assert.throws(() => preserveRegistryHistory(previous, rewrite), /réécrit/);
  const deletion = next.map(document => ({ ...document, entries: [] }));
  assert.throws(() => preserveRegistryHistory(previous, deletion), /Suppression interdite/);
});

test('global retention rejects duplicated identities and lost country registries', () => {
  const original = fixture();
  const empty = { ...fixture(), countryCode: 'ZZ', entries: [] };
  assert.throws(() => preserveRegistryHistory([original, empty], [original]), /Suppression de pays/);
  assert.throws(() => preserveRegistryHistory([original, empty], [original, { ...empty, entries: structuredClone(original.entries) }]), /Identifiant dupliqué/);
});

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
    { ...base, entryId: 'legacy', editorialStatus: 'Duplicate', duplicateOf: 'migrated-standalone' },
    { ...base, entryId: 'migrated-standalone', entityKind: 'StandaloneAttraction', presence: 'Published', editorialStatus: 'PublishedDateUnknown' },
    { ...base, entryId: 'duplicate-candidate', siteId: null, presence: 'NotFound', editorialStatus: 'Duplicate', duplicateOf: 'BE-example' },
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

test('reordering history cannot use an older completion audit for newer partial data', () => {
  const first = recordIntegration(fixture(), options(), now);
  const later = recordIntegration(first, { ...options(), event: 'later', at: '2026-10-22T13:00:00Z', outcome: 'partial', audit: undefined }, now);
  const reordered = structuredClone(later);
  reordered.entries[0].integrationHistory.reverse();
  reordered.entries[0].editorialStatus = 'Integrated';
  assert.throws(() => validateCountry(reordered), /non chronologique/);
  assert.throws(() => preserveHistory(later, reordered), /non chronologique/);
});

test('a same-date closure is allowed but existing same-date events cannot be reordered', () => {
  const partial = recordIntegration(fixture(), { ...options(), outcome: 'partial', audit: undefined }, now);
  const completed = recordIntegration(partial, { ...options(), event: 'closure' }, now);
  assert.equal(completed.entries[0].editorialStatus, 'Integrated');
  assert.equal(completed.entries[0].lastDataIntegrationAt, options().at);
  const reordered = structuredClone(completed);
  reordered.entries[0].integrationHistory.reverse();
  reordered.entries[0].editorialStatus = 'InProgress';
  assert.throws(() => preserveHistory(completed, reordered), /réécrit/);
});

test('migrating a published park never assumes the replacement standalone is published', () => {
  const doc = fixture();
  doc.entries[0].presence = 'Published';
  doc.entries[0].presenceObservedAt = '2026-10-22';
  const result = recordIntegration(doc, { ...options(), kind: 'StandaloneAttraction', entityId: 'standalone-1' }, now);
  assert.equal(result.entries[0].presence, 'ExistsVisibilityUnknown');
  assert.equal(result.entries[0].presenceObservedAt, null);
  assert.equal(countryCounts(result).standalonePublished, 0);
  assert.equal(countryCounts(result).visibilityUnknown, 1);
});

test('completed NotRelevant work no longer appears in the unresolved review count', () => {
  const doc = fixture();
  doc.entries[0].siteReviewStatusAtInventory = 'NotRelevant';
  const completed = recordIntegration(doc, options(), now);
  assert.equal(countryCounts(completed).parkReview, 0);
  assert.equal(countryCounts(completed).integratedHidden, 1);
});

test('invalid presence or missing classification cannot silently disappear from counts', () => {
  const doc = fixture();
  doc.entries[0].presence = 'Typo';
  assert.throws(() => validateCountry(doc), /Présence/);
  doc.entries[0].presence = 'NotFound';
  assert.throws(() => validateCountry(doc), /incohérents/);
  doc.entries[0].siteId = null;
  delete doc.entries[0].proposedEntityKind;
  assert.throws(() => validateCountry(doc), /type proposé/);
});

test('large national views paginate without losing or repeating any entry', () => {
  const doc = fixture();
  const base = doc.entries[0];
  doc.entries = Array.from({ length: 501 }, (_, index) => ({ ...base, entryId: `BE-entry-${String(index).padStart(3, '0')}` }));
  const pages = renderCountryPages(doc);
  assert.deepEqual(Object.keys(pages), ['001.md', '002.md', '003.md']);
  const renderedIds = Object.values(pages).join('\n').match(/BE-entry-\d{3}/g);
  assert.equal(renderedIds.length, 501);
  assert.equal(new Set(renderedIds).size, 501);
  assert.match(renderCountry(doc), /BE\/003\.md/);
});

test('world summary separates geographic groups and the unresolved-country queue', () => {
  const europe = fixture();
  europe.geographicGroup = 'Europe';
  const asia = { ...fixture(), countryCode: 'JP', countryName: 'Japon', geographicGroup: 'Asie' };
  const unknown = { ...fixture(), countryCode: 'ZZ', countryName: 'Pays à déterminer', geographicGroup: 'Pays à déterminer' };
  const summary = renderSummary([europe, asia, unknown]);
  assert.match(summary, /2 pays et territoires/);
  assert.match(summary, /## Asie/);
  assert.match(summary, /## Pays à déterminer/);
});
