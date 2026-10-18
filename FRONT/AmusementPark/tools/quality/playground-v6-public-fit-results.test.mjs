import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { Script } from 'node:vm';
import { accountLayoutFixtureOrigin, accountLayoutFixtureResponse, createAccountLayoutFixture } from '../design-v6/account-layout-fixtures.mjs';
import { publicFitLayoutRequest } from '../design-v6/public-fit-layout-fixtures.mjs';
import { exercisePublicFitResults } from '../design-v6/exercise-public-fit-results.mjs';

const fixture = () => createAccountLayoutFixture('standard', 1792144800, 'pl');
const request = (path, body, changes = {}) => ({ method: 'POST', url: accountLayoutFixtureOrigin + path, postData: JSON.stringify(body), ...changes });
const search = '/api/public/park-fit/search';
const telemetry = '/api/public/park-fit/pilot-events';

test('rich Park Fit input is isolated and exercises available/capped/suspended/excluded without inventing production data', () => {
  const data = fixture();
  const response = accountLayoutFixtureResponse(request(search, publicFitLayoutRequest()), data);
  assert.equal(response.status, 200);
  assert.equal(response.body.parks.length, 6);
  assert.equal(response.body.parks.filter(park => park.scoreState !== 'Excluded').length, 5);
  assert.deepEqual(response.body.parks.slice(0, 3).map(park => park.scoreState), ['Available', 'Capped', 'Suspended']);
  assert.equal(response.body.parks[2].comparativeScore, null);
  assert.equal(response.body.parks[2].calendarSourceUrl, null);
  assert.deepEqual(response.body.parks[2].criticalSources, []);
  assert.ok(response.body.parks.every(park => park.parkName.includes('fictif CI') && park.distanceKilometers === null));
  response.body.parks.length = 0;
  assert.equal(data.publicFitLayoutData.parks.length, 6, 'responses cannot mutate the shared fixture');
  assert.equal(accountLayoutFixtureResponse(request(search, publicFitLayoutRequest()), createAccountLayoutFixture('incomplete', 1792144800)).status, 200);
});

test('rich Park Fit accepts only one exact bounded payload shape on the local origin, never arbitrary writes or location', () => {
  const data = fixture();
  for (const changes of [
    { method: 'GET' }, { method: 'DELETE' }, { postData: undefined }, { postData: '{' },
    { postData: ' '.repeat(2049) }, { url: 'https://amusement-parks.fun' + search },
    { url: accountLayoutFixtureOrigin + search + '?anything=1' }, { url: accountLayoutFixtureOrigin + search + '#fragment' },
  ]) assert.equal(accountLayoutFixtureResponse(request(search, publicFitLayoutRequest(), changes), data), null);
  for (const change of [
    { evaluationDate: '2027-01-01' }, { members: [] }, { maximumResults: 100 }, { unknownDataPolicy: 'KnownOnly' },
    { originLatitude: 50.1, originLongitude: 3.2 }, { countryCode: 'FR' }, { preferIndoor: true }, { privateNote: 'not allowed' },
  ]) assert.equal(accountLayoutFixtureResponse(request(search, { ...publicFitLayoutRequest(), ...change }), data), null);
  for (const kind of ['expired', 'admin-shell']) {
    assert.equal(accountLayoutFixtureResponse(request(search, publicFitLayoutRequest()), createAccountLayoutFixture(kind, 1792144800)), null);
  }
  for (const path of ['/api/public/park-fit/reports', '/api/me/park-fit/group-profiles', search + '/extra']) {
    assert.equal(accountLayoutFixtureResponse(request(path, publicFitLayoutRequest()), data), null);
  }
});

test('only the telemetry envelopes actually emitted by the isolated rich recipe are accepted, no generic event sink', () => {
  const data = fixture();
  const completed = { eventKind: 'SearchCompleted', resultBand: 'FiveOrMore', unknownLevel: 'Significant',
    durationBand: 'UnderHalfSecond', methodVersion: 'park-fit-2026-01', qualityIssues: [] };
  for (const body of [{ eventKind: 'SearchStarted' }, { eventKind: 'ExplanationViewed' }, completed,
    { eventKind: 'ComparisonOpened', comparisonSize: 'Two' }, { eventKind: 'ComparisonOpened', comparisonSize: 'Four' }]) {
    assert.equal(accountLayoutFixtureResponse(request(telemetry, body), data).status, 200);
    assert.equal(accountLayoutFixtureResponse(request(telemetry, { ...body, email: 'private@example.test' }), data), null);
  }
  for (const body of [{}, { eventKind: 'SearchFailed' }, { ...completed, qualityIssues: ['unapproved'] },
    { ...completed, resultBand: 'One' }, { ...completed, durationBand: 'unknown' }, { eventKind: 'ComparisonOpened', comparisonSize: 'Five' }]) {
    assert.equal(accountLayoutFixtureResponse(request(telemetry, body), data), null);
  }
});

async function recipe({ extraRead } = {}) {
  const data = fixture(), result = { apiReads: [] }, captures = [], expressions = [], checks = [], navigations = [];
  await exercisePublicFitResults({ fixture: data, result, navigateDocument: async path => navigations.push(path),
    evaluate: async expression => {
      expressions.push(expression);
      new Script(expression);
      if (expression === `document.querySelectorAll('.park-fit-comparison__row').length`) return 14;
      if (expression === `document.querySelector('.park-fit-submit button[type="submit"]').click()`) {
        result.apiReads.push({ method: 'POST', path: search, status: 200 });
        if (extraRead) result.apiReads.push(extraRead);
      }
    }, waitUntil: async (expression, label) => { new Script(expression); checks.push({ expression, label }); },
    captureLayouts: async (selector, page) => captures.push({ selector, page }) });
  return { result, captures, expressions, checks, navigations };
}

test('rich browser recipe uses real controls/router links, rank ordering, five sections, selection limit and reversible differences', async () => {
  const { result, captures, expressions, checks, navigations } = await recipe();
  assert.deepEqual(navigations, ['/pl/park-fit'], 'full reload is only used before searching, preserving the real in-memory facade');
  assert.equal(captures.length, 12);
  assert.ok(captures.some(capture => capture.page === 'public-fit-results-suspended'));
  assert.ok(captures.some(capture => capture.page === 'public-fit-comparison-4-matrix'));
  assert.ok(checks.some(check => check.label.includes('Excluded candidate stays absent')));
  assert.ok(checks.some(check => check.label.includes('disable the fifth candidate')));
  assert.ok(checks.some(check => check.label.includes('show-all restores')));
  for (const expression of expressions) assert.doesNotMatch(expression, /fetch\(|XMLHttpRequest|geolocation|ng\.get|\.injector|localStorage|\.submit\(|requestSubmit\(/);
  assert.deepEqual(result.publicFitResults, { language: 'pl', visibleCandidates: 5, excludedCandidates: 1,
    comparisonSizes: [2, 4], selectionLimit: 4, differencesFilterExercised: true,
    localFixturesOnly: true, realSearchScoringPersistenceReportsAndLocationNotExercised: true });
  const capture = readFileSync(new URL('../design-v6/capture-account-layout.mjs', import.meta.url), 'utf8');
  assert.ok(capture.includes('await exercisePublicFitResults({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result });'));
});

test('rich browser recipe refuses a second search, a report or any unexpected endpoint instead of silently accepting writes', async () => {
  for (const extraRead of [{ method: 'POST', path: search, status: 200 }, { method: 'POST', path: '/api/public/park-fit/reports', status: 200 },
    { method: 'GET', path: '/api/me/park-fit/group-profiles', status: 200 }]) {
    await assert.rejects(() => recipe({ extraRead }), /one exact local search/);
  }
});
