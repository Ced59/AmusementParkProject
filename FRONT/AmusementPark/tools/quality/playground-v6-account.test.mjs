import assert from 'node:assert/strict';
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import test from 'node:test';
import { Script, runInNewContext } from 'node:vm';
import { exerciseAnonymousAuth } from '../design-v6/exercise-anonymous-auth.mjs';
import { accountLayoutFixtureOrigin, createAccountLayoutFixture, accountLayoutFixtureResponse, accountLayoutCases } from '../design-v6/account-layout-fixtures.mjs';
import { accountLayoutCaptureAllowed } from '../design-v6/capture-account-layout.mjs';
import { exerciseAccountLists } from '../design-v6/exercise-account-lists.mjs';
import { exerciseAccountProfileControls } from '../design-v6/exercise-account-profile-controls.mjs';
import { exerciseAccountPassportAndFit } from '../design-v6/exercise-account-passport-and-fit.mjs';
import { exerciseAccountAuthPages } from '../design-v6/exercise-account-auth-pages.mjs';
import { exerciseAccountNotifications } from '../design-v6/exercise-account-notifications.mjs';
import { exerciseAccountTripPages } from '../design-v6/exercise-account-trip-pages.mjs';
import { exerciseAccountPassportStatistics } from '../design-v6/exercise-account-passport-statistics.mjs';
import { accountComparisonFixtureToken } from '../design-v6/account-passport-statistics-fixtures.mjs';
import { exerciseAccountVisitEditor } from '../design-v6/exercise-account-visit-editor.mjs';
import { exerciseAnonymousDraftPages, openUnlockedAnonymousDraft } from '../design-v6/exercise-anonymous-draft-pages.mjs';
import { createAnonymousDraftFixtures, seedAnonymousDraftBrowser, anonymousDraftParkId } from '../design-v6/account-anonymous-draft-fixtures.mjs';
import { historyLayoutDocuments, historyLayoutParkId, historyLayoutSubjectId } from '../design-v6/public-history-layout-fixtures.mjs';
import { exerciseIsolatedHistoryPages } from '../design-v6/exercise-isolated-history-pages.mjs';
import { isolatedLayoutPreflightPlan } from '../design-v6/isolated-layout-preflight.mjs';
import { accountContrastIssue, accountLayoutMustStop, accountLayoutsHaveFailure } from '../design-v6/account-layout-acceptance.mjs';
import { positionAccountCaptureTarget } from '../design-v6/browser-account-target.mjs';
import { collectAdminShellLayout, adminShellLayoutIssues } from '../design-v6/browser-admin-shell.mjs';
import { exercisePublicFitForm } from '../design-v6/exercise-public-fit-form.mjs';
import { settleAccountDocument } from '../design-v6/settle-account-document.mjs';
import { withDoubledRootFont } from '../design-v6/account-layout-stress.mjs';
import { advanceMainDocument, forgetRetiredDocumentOwners, isRetiredDocumentInterceptionError } from '../design-v6/browser-interception-errors.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

test('enlarged-font evidence measures twice the root size and restores the inline value, even after failure', async () => {
  for (const fail of [false, true]) {
    const style = { fontSize: '' };
    const sandbox = { document: { documentElement: { style } }, getComputedStyle: () => ({ fontSize: style.fontSize || '16px' }), parseFloat,
      requestAnimationFrame: callback => callback() };
    const evaluate = async expression => JSON.parse(JSON.stringify(await runInNewContext(expression, sandbox)));
    const operation = () => withDoubledRootFont({ evaluate, capture: async ({ rootFontSizePx }) => {
      assert.equal(rootFontSizePx, 32);
      assert.equal(style.fontSize, '32px');
      if (fail) throw new Error('capture refused');
    } });
    if (fail) await assert.rejects(operation, /capture refused/);
    else await operation();
    assert.equal(style.fontSize, '');
  }
  const source = read('tools/design-v6/capture-account-layout.mjs');
  assert.match(source, /rootFontSizePx: parseFloat\(getComputedStyle\(document.documentElement\).fontSize\)/);
  assert.match(source, /'account-admin-shell-large-font'/);
});

test('enlarged-font stress rejects unexpected baselines and an override that was not actually applied', async () => {
  for (const pixels of [NaN, 0, 11, 25, 100]) {
    await assert.rejects(() => withDoubledRootFont({ evaluate: async () => ({ inline: '', pixels }), capture: async () => assert.fail('must not capture') }), /Unexpected root font/);
  }
  const expressions = [];
  await assert.rejects(() => withDoubledRootFont({ evaluate: async expression => {
    expressions.push(expression);
    return expression.startsWith('({') ? { inline: '', pixels: 16 } : 16;
  }, capture: async () => assert.fail('must not capture') }), /did not take effect/);
  assert.ok(expressions.at(-1).endsWith('= ""'));
  assert.equal(expressions.filter(expression => expression.startsWith('new Promise')).length, 30);
});

test('enlarged-font capture waits for computed size without weakening the exact doubled-size requirement', async () => {
  let frames = 0;
  let captured = false;
  await withDoubledRootFont({ evaluate: async expression => {
    if (expression.startsWith('({')) return { inline: '', pixels: 16 };
    if (expression.startsWith('new Promise')) return ++frames < 3 ? 24 : 32;
  }, capture: async ({ rootFontSizePx }) => { captured = true; assert.equal(rootFontSizePx, 32); } });
  assert.equal(frames, 3);
  assert.equal(captured, true);
});

test('account navigation waits for actual fonts and current intercepted responses instead of ignoring a canceled font', async () => {
  const events = [], pending = new Set();
  let finish;
  const response = new Promise(resolveResponse => { finish = resolveResponse; }).finally(() => { events.push('response'); pending.delete(response); });
  pending.add(response);
  await settleAccountDocument({ pending, evaluate: async expression => {
    assert.equal(expression, 'document.fonts.ready.then(() => document.fonts.status)');
    events.push('fonts'); finish(); return 'loaded';
  } });
  assert.deepEqual(events, ['fonts', 'response']);
  assert.equal(pending.size, 0);
  const source = read('tools/design-v6/capture-account-layout.mjs');
  const navigate = source.split('const navigateDocument = async path => {')[1].split('};')[0];
  assert.ok(navigate.indexOf('await settleAccountDocument') < navigate.indexOf("await cdp.send('Page.navigate'"));
  assert.match(source, /nativeCancellationObserved: canceled.has\(request.networkId\), nativeRetirementObserved: retired.has\(request.networkId\)/);
});

test('document settling refuses unresolved fonts, failed responses and a non-draining pending set', async () => {
  await assert.rejects(() => settleAccountDocument({ evaluate: async () => 'loading', pending: new Set() }), /fonts did not settle/);
  await assert.rejects(() => settleAccountDocument({ evaluate: async () => 'loaded', pending: new Set([Promise.reject(new Error('protocol failure'))]) }), /intercepted response failed/);
  await assert.rejects(() => settleAccountDocument({ evaluate: async () => 'loaded', pending: new Set([Promise.resolve()]) }), /bounded drain/);
});

test('public Park Fit recipe covers local participant and validation states without searching or requesting location', async () => {
  const navigations = [], captures = [], checks = [], expressions = [];
  const result = { apiReads: [] };
  await exercisePublicFitForm({ fixture: { language: 'pl' }, result,
    navigateDocument: async path => navigations.push(path), evaluate: async expression => expressions.push(expression),
    waitUntil: async (expression, label) => checks.push({ expression, label }),
    captureLayouts: async (selector, page) => captures.push({ selector, page }) });
  assert.deepEqual(navigations, ['/pl/park-fit', '/pl/park-fit/results', '/pl/park-fit/compare']);
  assert.equal(captures.length, 7);
  assert.ok(checks.some(check => check.expression.includes("classList.contains('ng-invalid')")));
  assert.ok(checks.some(check => check.label.includes('eighth participant')));
  assert.deepEqual(result.publicFitForm, { language: 'pl', maximumParticipants: 8, invalidSubmitOnly: true,
    searchRequested: false, locationRequested: false, scoredResultsExercised: false });
  for (const expression of expressions) {
    new Script(expression);
    assert.doesNotMatch(expression, /fetch\(|XMLHttpRequest|geolocation|requestLocation|\.submit\(|requestSubmit\(/);
  }
  assert.ok(checks.findIndex(check => check.label.includes('deliberately empty date')) >= 0);
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  assert.ok(capture.includes('...anonymousDocuments, ...publicFitDocuments'));
  assert.ok(read('tools/design-v6/capture-public-ssr.mjs').includes("'/fr/park-fit', '/fr/park-fit/results', '/fr/park-fit/compare'"));
});

test('public Park Fit local-only recipe refuses any unexpected API request', async () => {
  const result = { apiReads: [] };
  await assert.rejects(() => exercisePublicFitForm({ fixture: { language: 'fr' }, result,
    navigateDocument: async () => {}, evaluate: async () => {}, waitUntil: async () => {},
    captureLayouts: async (_, page) => { if (page === 'public-fit-comparison-missing') result.apiReads.push({ path: '/api/public/park-fit/search' }); } }),
  /unexpectedly requested an API/);
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  assert.equal(accountLayoutFixtureResponse({ method: 'POST', url: accountLayoutFixtureOrigin + '/api/public/park-fit/search', postData: '{}' }, fixture), null);
});

test('admin shell measurement reads the real links and toolbar without navigation or mutation', () => {
  const icon = {};
  const rect = (x, y, width, height) => ({ x, y, width, height, right: x + width, bottom: y + height });
  const link = (mobile, box) => ({ getAttribute: () => '/fr/admin', closest: () => mobile,
    getBoundingClientRect: () => box, contains: node => node === icon });
  const links = [link(false, rect(0, 0, 0, 0)), link(true, rect(330, 1128, 53, 72))];
  const sandbox = { innerWidth: 390, innerHeight: 1200, location: { pathname: '/fr/profile' }, document: {
    querySelectorAll: selector => selector === '.app-mobile-bottom-nav__item' ? Array(7).fill({}) : links,
    querySelector: selector => ({ getBoundingClientRect: () => selector === '.admin-public-view-toolbar'
      ? rect(8, 960, 374, 150) : rect(0, 1128, 390, 72) }), elementFromPoint: () => icon } };
  const layout = JSON.parse(JSON.stringify(runInNewContext(`(${collectAdminShellLayout.toString()})()`, sandbox)));
  assert.deepEqual(adminShellLayoutIssues(layout), []);
  assert.equal(layout.publicLayout, true);
  assert.deepEqual(layout.entries.map(entry => entry.scope), ['header', 'mobile']);
  assert.equal(layout.entries[1].unobscured, true);
  assert.equal(layout.adminPath, '/fr/admin');
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  const branch = capture.split("} else if (fixture.kind === 'admin-shell') {")[1].split('} else {')[0];
  assert.match(branch, /await captureLayouts\('\.profile-hero', 'account-admin-shell'\)/);
  assert.match(branch, /serverAuthorizationExercised: false, adminWorkflowsExercised: false/);
  assert.match(branch, /await navigateDocument\(publicAdminPath\)/);
  assert.ok(capture.includes('const publicAdminPath = `/${language}/privacy`;'));
  assert.doesNotMatch(branch, /\.click\(/);
});

test('admin shell acceptance refuses missing entries, obscured controls and toolbar overlap', () => {
  const layout = { adminPath: '/pl/admin', width: 390, height: 1200, publicLayout: true, bottomItemCount: 7,
    entries: [
      { scope: 'header', href: '/pl/admin', visible: false },
      { scope: 'mobile', href: '/pl/admin', visible: true, unobscured: true, x: 330, y: 1128,
        right: 383, bottom: 1200, width: 53, height: 72 },
    ], toolbar: { x: 8, y: 960, right: 382, bottom: 1110, width: 374, height: 150 },
    bottomNavigation: { x: 0, y: 1128, right: 390, bottom: 1200, width: 390, height: 72 } };
  assert.deepEqual(adminShellLayoutIssues(layout), []);
  assert.deepEqual(adminShellLayoutIssues({ ...layout, publicLayout: false, toolbar: null }), []);
  assert.ok(adminShellLayoutIssues({ ...layout, publicLayout: false }).includes('unexpected public admin toolbar in the account layout'));
  assert.ok(adminShellLayoutIssues({ ...layout, publicLayout: undefined }).includes('admin shell layout scope not measured'));
  for (const change of [
    value => value.entries.pop(), value => value.entries.push({ ...value.entries[1] }),
    value => { value.bottomItemCount = 6; }, value => { value.entries[1].unobscured = false; },
    value => { value.entries[1].right = 392; }, value => { value.entries[1].width = 10; },
    value => { value.entries[1].height = 20; }, value => { value.toolbar = null; },
    value => { value.toolbar.bottom = 1140; }, value => { value.toolbar.right = 400; },
  ]) {
    const invalid = structuredClone(layout);
    change(invalid);
    assert.ok(adminShellLayoutIssues(invalid).length, 'every invalid layout must be refused');
  }
});

test('the unencapsulated public admin toolbar targets its real host, including the mobile safe-area rule', () => {
  const root = 'src/app/features/admin/contextual-editing/ui/admin-public-view-toolbar/admin-public-view-toolbar.component';
  assert.match(read(root + '.ts'), /encapsulation:\s*ViewEncapsulation.None/);
  const source = read(root + '.scss');
  assert.doesNotMatch(source, /:host\b/);
  assert.match(source, /app-admin-public-view-toolbar\s*\{\s*position:\s*fixed/);
  assert.match(source, /@media \(max-width: 680px\)\s*\{\s*app-admin-public-view-toolbar\s*\{[^}]*bottom:\s*calc\(var\(--bottom-nav-height\) \+ max\(1rem, env\(safe-area-inset-bottom\)\)\)/);
});

test('admin shell recipe preserves the actual distinct account and public layout composition', () => {
  const account = read('src/app/ui/layouts/account-layout/account-layout.component.html');
  const publicLayout = read('src/app/ui/layouts/public-app-layout/public-app-layout.component.html');
  assert.doesNotMatch(account, /adminToolbarComponent|ngComponentOutlet|admin-public-view-toolbar/);
  assert.match(publicLayout, /@if \(adminToolbarComponent\(\); as toolbarComponent\)/);
  assert.match(publicLayout, /\*ngComponentOutlet="toolbarComponent"/);
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  assert.match(capture, /await captureLargeFont\('\.profile-hero', 'account-admin-shell-large-font'\)/);
  assert.match(capture, /await captureLargeFont\('\.app-public-layout__content', 'public-admin-shell-large-font'\)/);
  assert.match(capture, /account exposes its two existing navigation entries without a public-layout toolbar/);
});

test('history compare keeps the unchanged trail missing-article fallback exact and read-only', () => {
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const request = { method: 'GET', url: accountLayoutFixtureOrigin + '/api/history/articles/compare' };
  assert.equal(accountLayoutFixtureResponse(request, fixture).status, 404);
  for (const changed of [{ method: 'POST' }, { method: 'DELETE' }, { url: request.url + '?unknown=true' }, { url: request.url.replace('/compare', '/unknown') }]) {
    assert.equal(accountLayoutFixtureResponse({ ...request, ...changed }, fixture), null);
  }
});

test('account document ownership stays bounded after retirement without losing exact cancellation evidence', () => {
  const documents = new Map();
  const retired = new Set();
  let current;
  for (let page = 0; page < 80; page++) {
    current = advanceMainDocument(current, { id: 'main', loaderId: `loader-${page}` }, documents, retired);
    forgetRetiredDocumentOwners(documents, retired);
    for (let index = 0; index < 100; index++) documents.set(`${page}-${index}`, { frameId: 'main', loaderId: `loader-${page}` });
    assert.equal(documents.size, 100);
  }
  assert.equal(retired.size, 7900);
  assert.equal(isRetiredDocumentInterceptionError(new Error('Invalid InterceptionId.'), '0-0', retired), true);
  assert.equal(isRetiredDocumentInterceptionError(new Error('Invalid InterceptionId.'), '79-0', retired), false);
  documents.set('future-cached-request', { frameId: 'main', loaderId: 'loader-0' });
  forgetRetiredDocumentOwners(documents, retired);
  assert.equal(documents.has('future-cached-request'), true);
  assert.ok(read('tools/design-v6/capture-account-layout.mjs').includes('documents.size >= 4096'));
});

test('the participant header does not stretch a compact count chip to paragraph height on mobile', () => {
  const source = read('src/app/features/profile/trips/components/trip-participant-panel/trip-participant-panel.component.scss');
  assert.match(source, /\.participant-panel__heading\s*\{[^}]*align-items:\s*flex-start/);
  assert.match(source, /\.participant-panel__heading > div\s*\{[^}]*min-width:\s*0;[^}]*flex:\s*1/);
  const tablet = source.split('@media (max-width: 42rem)')[1].split('@media')[0];
  assert.doesNotMatch(tablet, /\.participant-panel__heading/);
  assert.match(source, /\.participant-panel__message--error\s*\{[^}]*color:\s*var\(--app-danger\)/);
});

test('anonymous continue selects the unlocked card regardless of IndexedDB list ordering, never deletion', () => {
  const clicked = [];
  const card = (id, locked) => ({ querySelector: selector => selector.endsWith('last-child')
    ? { disabled: locked, click: () => { throw new Error('Never delete a draft.'); } }
    : { disabled: false, click: () => clicked.push(id) } });
  const normal = card('normal', false);
  const locked = card('locked', true);
  for (const cards of [[normal, locked], [locked, normal]]) {
    runInNewContext(`(${openUnlockedAnonymousDraft.toString()})()`, { document: { querySelectorAll: () => cards } });
  }
  assert.deepEqual(clicked, ['normal', 'normal']);
  for (const cards of [[], [locked], [normal, card('ambiguous', false)]]) {
    assert.throws(() => runInNewContext(`(${openUnlockedAnonymousDraft.toString()})()`, { document: { querySelectorAll: () => cards } }), /exactly one unlocked/);
  }
});

test('account captures re-anchor the measured target after reflow and clear sticky navigation', () => {
  let top = -300;
  let scrolls = 0;
  const target = { scrollIntoView: () => { scrolls++; top = 0; },
    getBoundingClientRect: () => ({ x: 20, y: top, top, right: 355, bottom: top + 400, width: 335, height: 400 }) };
  const sandbox = { innerWidth: 390, innerHeight: 1200,
    document: { querySelector: selector => selector === '.target' ? target : selector === '.app-public-nav'
      ? { getBoundingClientRect: () => ({ bottom: 80 }) } : { getBoundingClientRect: () => ({ top: 1120, height: 80 }) } },
    window: { scrollBy: options => { top -= options.top; } } };
  const measure = () => runInNewContext(`(${positionAccountCaptureTarget.toString()})('.target')`, sandbox);
  assert.equal(measure().y, 92);
  top = 1500; // A different viewport has moved the target again.
  assert.equal(measure().visible, true);
  assert.equal(top, 92);
  assert.equal(scrolls, 2);
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  assert.match(capture, /layout\.target = target/);
  assert.match(capture, /layout\.theme === theme && layout\.target\.visible/);
  assert.match(capture, /layout\.target\.right > width \+ 1/);
});

test('an absent or hidden capture target cannot be accepted as a screenshot of its page', () => {
  const hidden = { scrollIntoView: () => {}, getBoundingClientRect: () => ({ x: 0, y: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0 }) };
  const sandbox = { innerWidth: 390, innerHeight: 1200, document: { querySelector: () => null }, window: { scrollBy: () => {} } };
  assert.throws(() => runInNewContext(`(${positionAccountCaptureTarget.toString()})('.missing')`, sandbox), /target is missing/);
  sandbox.document.querySelector = selector => selector === '.hidden' ? hidden : null;
  assert.equal(runInNewContext(`(${positionAccountCaptureTarget.toString()})('.hidden')`, sandbox).visible, false);
});

test('diagnostic contrast collection cannot accept a failed fixture or defer geometry and missing coverage', () => {
  assert.equal(accountLayoutMustStop('v6', [accountContrastIssue]), true);
  assert.equal(accountLayoutMustStop('v6', [accountContrastIssue], true), false);
  for (const issue of ['document horizontal overflow', 'expired login dialog bounds or close action', 'no measurable visible flat text for the account contrast probe']) {
    assert.equal(accountLayoutMustStop('v6', [accountContrastIssue, issue], true), true);
  }
  assert.equal(accountLayoutMustStop('v6', [], true), false);
  assert.equal(accountLayoutMustStop('master', [accountContrastIssue]), false);
  assert.equal(accountLayoutsHaveFailure('v6', [{ issues: [] }, { issues: [accountContrastIssue] }]), true);
  assert.equal(accountLayoutsHaveFailure('v6', [{ issues: [] }]), false);
  assert.equal(accountLayoutsHaveFailure('master', [{ issues: [accountContrastIssue] }]), false);
  const source = read('tools/design-v6/capture-account-layout.mjs');
  assert.match(source, /deferContrastFailure = false/);
  assert.match(source, /deferContrastFailure && process\.env\.V6_ISOLATED_PREFLIGHT !== 'true'/);
  assert.ok(source.indexOf('if (accountLayoutsHaveFailure(label, result.layouts)) throw') < source.indexOf('result.passed = true'));
  const fullCampaign = read('tools/design-v6/capture-public-ssr.mjs').split('entry = await import(pathToFileURL')[1];
  assert.doesNotMatch(fullCampaign, /deferContrastFailure/);
});

test('isolated preflight is an explicit Linux CI subset, never a replacement for the full matrix', () => {
  const environment = { CI: 'true', V6_ISOLATED_PREFLIGHT: 'true', V6_BASELINE_DIST_DIR: '/ci/master-dist', V6_BASELINE_SHA: 'a'.repeat(40) };
  assert.equal(isolatedLayoutPreflightPlan({}, 'win32'), null);
  assert.deepEqual(isolatedLayoutPreflightPlan(environment, 'linux'), {
    fullCampaignRequired: true,
    cases: [{ kind: 'admin-shell', language: 'fr', widths: [320, 390, 1366] },
      ...['standard', 'incomplete'].map(kind => ({ kind, language: 'fr', widths: [390, 1366] }))],
  });
  for (const patch of [{ CI: undefined }, { CI: 'false' }, { V6_BASELINE_DIST_DIR: undefined }, { V6_BASELINE_SHA: 'master' }, { V6_ISOLATED_PREFLIGHT: 'false' }]) {
    assert.throws(() => isolatedLayoutPreflightPlan({ ...environment, ...patch }, 'linux'));
  }
  assert.throws(() => isolatedLayoutPreflightPlan(environment, 'win32'), /Linux CI/);
  assert.throws(() => isolatedLayoutPreflightPlan(environment, 'darwin'), /Linux CI/);
});

test('preflight rejects local execution before Chromium and does not load public fixtures or SSR', () => {
  const source = read('tools/design-v6/capture-public-ssr.mjs');
  assert.ok(source.indexOf('isolatedLayoutPreflightPlan(process.env, process.platform)') < source.indexOf('const chromeCandidates'));
  assert.match(source, /const homeFixtureDocument = !preflightPlan && process\.env\.V6_BASELINE_DIST_DIR/);
  assert.match(source, /report\.preflightOnly = true;/);
  assert.match(source, /report\.fullCampaignRequired = preflightPlan\.fullCampaignRequired;/);
  assert.match(source, /\} else \{\s+entry = await import\(pathToFileURL/);
  assert.match(source, /accountLayoutCases\(widths\)/);
  assert.match(source, /finally \{\s+entry\?\.ɵdestroyAngularServerApp\(\);/);
  assert.match(source, /globalThis\.fetch = nativeFetch;/);
  assert.match(source, /await rm\(tempRoot, \{ recursive: true, force: true/);
});

test('CI keeps full visual capture mandatory after preflight and retains failure artifacts', () => {
  const workflow = read('../../.github/workflows/production.yml');
  const preflight = workflow.indexOf('id: v6-preflight');
  const full = workflow.indexOf('id: v6-review');
  const upload = workflow.indexOf('name: Upload initial V6 public SSR evidence');
  assert.ok(preflight > 0 && full > preflight && upload > full);
  const diagnosticStep = workflow.slice(preflight, full);
  const fullStep = workflow.slice(full, upload);
  assert.match(diagnosticStep, /V6_ISOLATED_PREFLIGHT: 'true'/);
  assert.doesNotMatch(diagnosticStep + fullStep, /continue-on-error:/);
  assert.doesNotMatch(fullStep, /V6_ISOLATED_PREFLIGHT|steps\.v6-preflight/);
  assert.match(fullStep, /node --max-old-space-size=768 tools\/design-v6\/capture-public-ssr\.mjs/);
  assert.match(workflow.slice(upload, upload + 480), /always\(\).*steps\.v6-preflight\.outcome != 'skipped'.*steps\.v6-review\.outcome != 'skipped'/);
});

test('isolated historical capabilities keep exact paged DTOs, snapshot dates, ranges and lineage without publication', () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    const prefix = `/api/public/parks/${historyLayoutParkId}/history`;
    const get = path => accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + path }, fixture);
    const timeline = get(prefix + '/timeline?page=1&pageSize=25');
    assert.equal(timeline.body.events.length, kind === 'standard' ? 25 : 0);
    assert.equal(timeline.body.pagination.totalPages, kind === 'standard' ? 2 : 1);
    assert.equal(get(prefix + '/timeline?page=2&pageSize=25').body.events.length, kind === 'standard' ? 1 : 0);
    const snapshot = get(prefix + '/snapshot?year=2000&month=1&day=1');
    assert.deepEqual(snapshot.body.requestedInstant, { year: 2000, month: 1, day: 1, precision: 'Day' });
    assert.equal(snapshot.body.isIndexableKeyYear, false);
    snapshot.body.subjects.push({ subjectId: 'changed' });
    assert.equal(get(prefix + '/snapshot?year=2000').body.subjects.length, kind === 'standard' ? 5 : 0);
    assert.equal(get(prefix + '/compare?fromYear=2001&toYear=2002').body.fromInstant.year, 2001);
    const lineage = get(`/api/public/history/subjects/ParkItem/${historyLayoutSubjectId}/lineage?contextParkId=${historyLayoutParkId}`);
    assert.equal(lineage.body.relations.length, kind === 'standard' ? 1 : 0);
    for (const suffix of ['/timeline?page=3&pageSize=25', '/snapshot?year=2026', '/snapshot?year=2000&unknown=true', '/compare?fromYear=2002&toYear=2000']) assert.equal(get(prefix + suffix), null);
    for (const method of ['POST', 'PATCH', 'DELETE']) assert.equal(accountLayoutFixtureResponse({ method, url: accountLayoutFixtureOrigin + prefix + '/snapshot?year=2000' }, fixture), null);
    const documents = historyLayoutDocuments('pl');
    assert.equal(documents.length, 7);
    assert.ok(documents.every(path => path.startsWith('/pl/')));
  }
});

test('isolated history expressions compile date/range/pagination/source interactions without data publication', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const result = {};
    const captures = [];
    await exerciseIsolatedHistoryPages({ fixture: createAccountLayoutFixture(kind, 1792144800, 'de'), result, language: 'de',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page), navigateDocument: async () => {} });
    assert.equal(captures.length, kind === 'standard' ? 9 : 6);
    assert.equal(result.isolatedHistory.historyPublished, false);
    assert.equal(result.isolatedHistory.publicFleuryCapabilitiesNotReclassified, true);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-isolated-history-pages.mjs'), /fetch\(|\.publish\(|\.save\(|admin\/history/);
});

test('anonymous draft seed is bounded to the isolated browser origin and never touches user storage', async () => {
  const drafts = createAnonymousDraftFixtures('2026-10-16T12:00:00Z');
  assert.equal(drafts.length, 2);
  assert.equal(drafts[0].schemaVersion, 1);
  assert.equal(drafts[0].rides[0].count, 2);
  assert.equal(drafts[0].pendingImport, null);
  assert.equal(drafts[1].pendingImport.choice, 'Separate');
  for (const origin of ['https://amusement-parks.fun', 'null', 'https://unrelated.invalid']) {
    await assert.rejects(runInNewContext(`(${seedAnonymousDraftBrowser.toString()})(${JSON.stringify(accountLayoutFixtureOrigin)}, [])`, {
      location: { origin }, indexedDB: { open: () => { throw new Error('Must not touch storage.'); } },
    }), /not isolated/);
  }
  await assert.rejects(runInNewContext(`(${seedAnonymousDraftBrowser.toString()})(${JSON.stringify(accountLayoutFixtureOrigin)}, [{},{},{}])`, {
    location: { origin: accountLayoutFixtureOrigin }, indexedDB: { open: () => { throw new Error('Must not touch storage.'); } },
  }), /fixture bound/);
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const request = { method: 'GET', url: `${accountLayoutFixtureOrigin}/api/park-items/park/${anonymousDraftParkId}?page=1&size=20&closedFilter=all&category=Attraction` };
  assert.equal(accountLayoutFixtureResponse(request, fixture).body.items.length, 1);
  assert.equal(accountLayoutFixtureResponse({ ...request, method: 'POST' }, fixture), null);
  assert.equal(accountLayoutFixtureResponse({ ...request, url: request.url + '&unknown=true' }, fixture), null);
  assert.equal(accountLayoutFixtureResponse({ ...request, url: request.url.replace(accountLayoutFixtureOrigin, 'https://amusement-parks.fun') }, fixture), null);
});

test('anonymous draft recipe compiles local list/editor/lock states without import, remote write or export', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800, 'pl');
    const result = { apiReads: [] };
    const captures = [];
    await exerciseAnonymousDraftPages({ fixture, result, language: 'pl',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page), navigateDocument: async () => {} });
    assert.equal(captures.length, kind === 'standard' ? 7 : 2);
    assert.equal(result.anonymousDrafts.indexedDbExercised, true);
    assert.equal(result.anonymousDrafts.importExportAndConfirmedDeletionNotExercised, true);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-anonymous-draft-pages.mjs'), /fetch\(|requestSubmit|\.submit\(|\.export\(|\.addRide\(|\.deleteDraft\(/);
  const harness = read('tools/design-v6/capture-account-layout.mjs');
  assert.ok(harness.includes('unauthenticatedDocuments)}.includes(location.pathname)'));
  assert.ok(harness.includes('const unauthenticatedDocuments = [...anonymousDocuments,'));
  assert.ok(harness.includes('localStorage.removeItem('));
});

test('visit editor input keeps exact draft/completed/archived states, cursor and destructive-write denial', () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    const get = path => accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + path }, fixture);
    assert.deepEqual(fixture.visitEditorData.map(item => item.visit.status), kind === 'standard' ? ['Draft', 'Completed'] : ['Archived']);
    for (const data of fixture.visitEditorData) {
      const prefix = `/api/me/passport/visits/${data.visit.id}`;
      assert.equal(get(prefix).body.status, data.visit.status);
      assert.equal(get(`${prefix}/occurrences?limit=50`).body.items.length, kind === 'standard' ? 2 : 0);
      const next = get(`${prefix}/occurrences?limit=50&cursor=apf-editor-next`);
      if (kind === 'standard') {
        assert.equal(next.body.items.length, 1);
        assert.equal(next.body.items[0].id, data.occurrences[2].id);
        assert.ok(!get(`${prefix}/occurrences?limit=50`).body.items.some(item => item.id === next.body.items[0].id));
        assert.equal(next.body.nextCursor, null);
      } else assert.equal(next, null);
      assert.equal(get(`${prefix}/historical-ride-targets?page=1&pageSize=24&scope=KnownOpen`).body.items.length, data.targets.length);
      assert.equal(get(`${prefix}/deletion-preview`).body.occurrenceCount, data.occurrences.length);
      assert.equal(get(`${prefix}/historical-existence-reports`).body.length, kind === 'standard' ? 1 : 0);
      assert.equal(get(`${prefix}/historical-existence-reports?unknown=true`), null);
      assert.equal(get(`${prefix}/occurrences?limit=100`), null);
      assert.equal(get(`${prefix}/historical-ride-targets?page=2&pageSize=24&scope=KnownOpen`), null);
      for (const method of ['POST', 'PUT', 'DELETE']) for (const suffix of ['', '/complete', '/archive', '/assessment', '/occurrences:batch', '/historical-existence-reports']) {
        assert.equal(accountLayoutFixtureResponse({ method, url: accountLayoutFixtureOrigin + prefix + suffix }, fixture), null);
      }
      const first = get(prefix);
      first.body.title = 'changed';
      assert.notEqual(get(prefix).body.title, 'changed');
    }
    assert.equal(get(`/api/parks/${fixture.visitEditorData[0].visit.parkId}`).status, kind === 'standard' ? 200 : 404);
  }
});

test('visit editor expressions compile without saved metadata, assessments or confirmed deletion', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800, 'de');
    const result = {};
    const captures = [];
    await exerciseAccountVisitEditor({ fixture, result, language: 'de',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page), navigateDocument: async () => {} });
    assert.equal(captures.length, kind === 'standard' ? 17 : 6);
    assert.equal(result.visitEditor.writesAssessmentsRideLogsStatusChangesDeletionAndPublicationNotExercised, true);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-account-visit-editor.mjs'), /fetch\(|requestSubmit|\.submit\(|saveVisitMetadata\(|confirmVisitDeletion\(|\.complete\(|\.archive\(/);
});

test('private passport statistics fixtures preserve exact scopes, local filters and unpublished comparison/share states', () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    const data = fixture.passportStatisticsData;
    const get = path => accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + path }, fixture);
    for (const path of ['/api/me/passport/stats', '/api/me/passport/stats/history', `/api/me/passport/items/${data.itemId}/stats`,
      `/api/me/passport/parks/${data.parkId}/stats`, '/api/me/passport/years/2026/stats', '/api/me/passport/share', '/api/me/passport/share/selection',
      '/api/me/passport/years/2026/share', '/api/me/passport/years/2026/share/selection', '/api/me/profile-comparisons',
      `/api/me/comparisons/invitations/${accountComparisonFixtureToken}/preview`]) assert.equal(get(path).status, 200, path);
    assert.equal(get('/api/me/passport/stats').body.summary.visitCount, kind === 'standard' ? 2 : 0);
    assert.equal(get('/api/me/passport/stats?year=2026').body.selectedYear, 2026);
    assert.equal(get(`/api/me/passport/stats?year=2026&parkId=${data.parkId}`).body.selectedParkId, data.parkId);
    const first = get('/api/me/passport/stats');
    first.body.availableYears.push(1900);
    assert.equal(get('/api/me/passport/stats').body.availableYears.includes(1900), false);
    assert.equal(get('/api/me/passport/share').body.isPublic, false);
    assert.equal(get(`/api/me/comparisons/invitations/${accountComparisonFixtureToken}/preview`).body.status, kind === 'standard' ? 'Ready' : 'Expired');
    for (const path of ['/api/me/passport/stats?year=1900', '/api/me/passport/stats?unknown=true', '/api/me/passport/items/unknown/stats',
      '/api/me/comparisons/invitations/unknown/preview', '/api/me/passport/years/2025/share']) assert.equal(get(path), null);
    for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) for (const path of ['/api/me/shares/preview', '/api/me/shares/publish',
      '/api/me/passport/share', '/api/me/profile-comparisons/ci-comparison-only', `/api/me/comparisons/invitations/${accountComparisonFixtureToken}/accept`]) {
      assert.equal(accountLayoutFixtureResponse({ method, url: accountLayoutFixtureOrigin + path }, fixture), null);
    }
  }
});

test('seven private passport statistics/share recipes compile without publication or comparison consent', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800, 'pl');
    const result = {};
    const documents = [];
    const captures = [];
    await exerciseAccountPassportStatistics({ fixture, result, language: 'pl',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page), navigateDocument: async path => documents.push(path) });
    assert.equal(documents.length, 8);
    assert.equal(captures.length, kind === 'standard' ? 16 : 15);
    assert.equal(documents.at(-1), '/pl/profile');
    assert.equal(result.passportStatisticsPages.sharingComparisonAndPublicationNotExercised, true);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-account-passport-statistics.mjs'), /fetch\(|requestSubmit|\.submit\(|\.publish\(|previewPublication\(|\.accept\(|\.revoke\(/);
});

test('private trip UI input preserves exact read-only endpoints, local role variants and activity cursor deduplication', () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    const prefix = `/api/me/trips/${fixture.tripData.plan.tripPlanId}`;
    const get = path => accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + path }, fixture);
    for (const suffix of ['', '/program', '/preferences', '/preference-summary', '/coherence', '/activity', '/participants', '/invitations', '/notifications', '/export', '/passport-transition']) assert.equal(get(prefix + suffix).status, 200, suffix);
    assert.equal(get(prefix).body.effectiveRole, kind === 'standard' ? 'Owner' : 'Viewer');
    assert.equal(get(prefix + '/preferences').body.canVote, kind === 'standard');
    const first = get(prefix + '/program');
    first.body.candidates.push({ candidateId: 'changed' });
    assert.equal(get(prefix + '/program').body.candidates.length, kind === 'standard' ? 1 : 0);
    if (kind === 'standard') {
      const activity = get(prefix + '/activity');
      const older = get(prefix + '/activity?beforeSequence=3');
      assert.equal(older.body.entries[0].sequence, activity.body.entries[0].sequence);
      assert.deepEqual(older.body.entries.map(item => item.sequence), [3, 2]);
      assert.equal(older.body.nextBeforeSequence, null);
    } else assert.equal(get(prefix + '/activity?beforeSequence=3'), null);
    assert.equal(get('/api/me/collections?targetType=Park').body.every(item => item.targetType === 'Park'), true);
    for (const path of [prefix + '/unknown', prefix + '?unknown=true', prefix + '/activity?beforeSequence=2', '/api/me/trips/unknown/program']) assert.equal(get(path), null);
    for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) for (const suffix of ['', '/dates', '/program', '/invitations', '/participants', '/preferences', '/passport-transition/days/2026-12-20/confirm']) {
      assert.equal(accountLayoutFixtureResponse({ method, url: accountLayoutFixtureOrigin + prefix + suffix }, fixture), null);
    }
  }
});

test('seven private trip deep-link recipes compile exact browser expressions without saved mutations or downloads', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const fixture = createAccountLayoutFixture(kind, 1792144800, 'de');
    const result = {};
    const documents = [];
    const captures = [];
    await exerciseAccountTripPages({ fixture, result, language: 'de',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page), navigateDocument: async path => documents.push(path) });
    assert.equal(documents.length, 8);
    assert.equal(captures.length, kind === 'standard' ? 18 : 16);
    assert.equal(result.tripPages.pages, 7);
    assert.equal(documents.at(-1), '/de/profile');
    assert.equal(result.tripPages.persistenceInvitationsDownloadsPrintAndPassportConfirmationNotExercised, true);
  }
  const source = read('tools/design-v6/exercise-account-trip-pages.mjs');
  assert.doesNotMatch(source, /fetch\(|requestSubmit|\.submit\(|\.save\(|\.print\(|downloadJson\(|\.confirm\(/);
  assert.ok(source.includes('JSON.stringify(`${hero} h1`)'));
  assert.ok(!source.includes("document.querySelector('h1')"), 'hidden account-shell heading must not replace the exact trip page title');
});

test('notification layout fixtures retain exact pagination, filters, immutable rows and intentional isolated errors', () => {
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const response = query => accountLayoutFixtureResponse({ method: 'GET', url: `${accountLayoutFixtureOrigin}/api/me/notifications${query}` }, fixture);
  const first = response('?page=1&size=12&unreadOnly=false');
  assert.equal(first.body.items.length, 12);
  assert.equal(first.body.totalItems, 13);
  assert.equal(first.body.totalPages, 2);
  const second = response('?page=2&size=12&unreadOnly=false');
  assert.equal(second.body.items.length, 1);
  assert.equal(second.body.items[0].notificationId, fixture.notificationData.notifications[12].notificationId);
  assert.equal(response('?page=1&size=12&unreadOnly=true').body.items.length, 7);
  assert.equal(response('?page=1&size=12&unreadOnly=false&parkId=00000000-0000-4000-8000-000000000050').body.items.length, 7);
  assert.equal(response('?page=1&size=12&unreadOnly=false&eventType=ParkNameChanged').body.items.length, 6);
  assert.equal(response('?page=1&size=12&unreadOnly=false&eventType=VerifiedSourceAdded').status, 503);
  first.body.items[0].target.name = 'changed';
  assert.notEqual(response('?page=1&size=12&unreadOnly=false').body.items[0].target.name, 'changed');
  for (const query of ['?page=3&size=12&unreadOnly=false', '?page=1&size=100&unreadOnly=false', '?page=1&size=12&unreadOnly=false&unknown=true']) assert.equal(response(query), null);
  const empty = createAccountLayoutFixture('incomplete', 1792144800);
  assert.equal(accountLayoutFixtureResponse({ method: 'GET', url: `${accountLayoutFixtureOrigin}/api/me/notifications?page=1&size=12&unreadOnly=false` }, empty).body.totalItems, 0);
});

test('notification stubs allow only exact automatic page-open telemetry and reject private mutations', () => {
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const request = { method: 'POST', url: `${accountLayoutFixtureOrigin}/api/me/notifications/pilot-interactions`,
    postData: JSON.stringify({ interactionKind: 'NotificationCenterOpened', notificationId: null }) };
  assert.equal(accountLayoutFixtureResponse(request, fixture).status, 200);
  for (const postData of ['', 'null', '{', JSON.stringify({ interactionKind: 'SourceOpened', notificationId: null }),
    JSON.stringify({ interactionKind: 'NotificationCenterOpened', notificationId: 'unknown' }),
    JSON.stringify({ interactionKind: 'NotificationCenterOpened', notificationId: null, extra: true })]) {
    assert.equal(accountLayoutFixtureResponse({ ...request, postData }, fixture), null);
  }
  for (const [method, path] of [['POST', '/api/me/notifications/read-all'], ['POST', '/api/me/notifications/unknown/read'],
    ['DELETE', '/api/me/notifications/unknown/subscription'], ['PUT', '/api/me/notification-preferences'], ['DELETE', '/api/me/live-alerts/unknown']]) {
    assert.equal(accountLayoutFixtureResponse({ method, url: accountLayoutFixtureOrigin + path }, fixture), null);
  }
  assert.equal(accountLayoutFixtureResponse({ ...request, url: request.url.replace(accountLayoutFixtureOrigin, 'https://amusement-parks.fun') }, fixture), null);
  assert.equal(accountLayoutFixtureResponse(request, createAccountLayoutFixture('expired', 1792144800)), null);
  for (const kind of ['standard', 'incomplete']) {
    const variant = createAccountLayoutFixture(kind, 1792144800);
    const preference = accountLayoutFixtureResponse({ method: 'GET', url: `${accountLayoutFixtureOrigin}/api/me/notification-preferences` }, variant);
    assert.equal(preference.body.emailAvailable, kind === 'standard');
    const live = accountLayoutFixtureResponse({ method: 'GET', url: `${accountLayoutFixtureOrigin}/api/me/live-alerts` }, variant);
    assert.equal(live.body.notifications.length, kind === 'standard' ? 1 : 0);
  }
});

test('notification recipe compiles exact browser expressions without activating private write controls', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const captures = [];
    const result = { apiReads: [] };
    await exerciseAccountNotifications({ fixture: createAccountLayoutFixture(kind, 1792144800, 'pl'), result, language: 'pl',
      evaluate: async expression => { assert.doesNotThrow(() => new Script(expression)); return { index: 3, key: 'profile-entry' }; },
      waitUntil: async predicate => { if (typeof predicate === 'string') assert.doesNotThrow(() => new Script(predicate)); },
      captureLayouts: async (_selector, page) => captures.push(page) });
    assert.equal(captures.length, kind === 'standard' ? 7 : 3);
    assert.equal(result.notifications.mutationsExercised, false);
    assert.equal(result.notifications.emailConsentChanged, false);
  }
  const source = read('tools/design-v6/exercise-account-notifications.mjs');
  assert.doesNotMatch(source, /fetch\(|requestSubmit|\.submit\(|\.markRead\(|\.dismiss\(|\.unsubscribe\(|\.enable\(/);
});

test('account layout fixtures distinguish complete, incomplete and expired UI sessions without real credentials', () => {
  const now = 1792144800;
  const standard = createAccountLayoutFixture('standard', now);
  const incomplete = createAccountLayoutFixture('incomplete', now);
  const expired = createAccountLayoutFixture('expired', now);
  const request = { method: 'POST', url: `${accountLayoutFixtureOrigin}/api/auth/refresh-token` };
  assert.equal(accountLayoutFixtureResponse(request, standard).status, 200);
  assert.equal(accountLayoutFixtureResponse(request, expired).status, 401);
  const claims = JSON.parse(Buffer.from(standard.accessToken.split('.')[1], 'base64url').toString());
  assert.deepEqual(claims.roles, ['USER']);
  assert.equal(claims.exp, now + 6 * 60 * 60);
  assert.equal(claims.aud, 'apf-ci-layout-only');
  assert.equal(incomplete.user.firstName, '');
  assert.equal(incomplete.user.isActivated, true);
  assert.equal(standard.user.preferredMeasurementSystem, 'Metric');
  assert.throws(() => createAccountLayoutFixture('admin', now), /Unknown/);
});

test('account acceptance retains all French widths and explicit two-theme mobile/desktop cases in the other seven languages', () => {
  const widths = [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920];
  const cases = accountLayoutCases(widths);
  assert.equal(cases.length, 18);
  assert.deepEqual(cases.slice(0, 3).map(item => item.kind), ['standard', 'incomplete', 'expired']);
  assert.deepEqual(cases.slice(0, 3).map(item => item.widths), [widths, widths, widths]);
  assert.deepEqual(cases.slice(3, 10).map(item => item.language), ['en', 'de', 'nl', 'it', 'es', 'pt', 'pl']);
  for (const item of cases.slice(3, 10)) {
    assert.deepEqual(item.widths, [390, 1366]);
    assert.equal(createAccountLayoutFixture(item.kind, 1792144800, item.language).user.preferredLanguage, item.language.toUpperCase());
  }
  assert.deepEqual(cases.slice(10).map(item => item.language), ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl']);
  assert.deepEqual(cases[10].widths, widths);
  for (const item of cases.slice(10)) {
    assert.equal(item.kind, 'admin-shell');
    const fixture = createAccountLayoutFixture(item.kind, 1792144800, item.language);
    assert.deepEqual(fixture.user.roles, ['USER', 'ADMIN']);
    const claims = JSON.parse(Buffer.from(fixture.accessToken.split('.')[1], 'base64url'));
    assert.deepEqual(claims.roles, fixture.user.roles);
    assert.deepEqual(createAccountLayoutFixture('standard', 1792144800, item.language).user.roles, ['USER']);
    assert.equal(accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + '/api/admin/users' }, fixture), null);
  }
  assert.throws(() => accountLayoutCases([320]), /390 and 1366/);
  assert.throws(() => accountLayoutCases([...widths, 390]), /bounded widths/);
  assert.throws(() => createAccountLayoutFixture('standard', 1792144800, 'unknown'), /Unknown/);
});

test('account introduction and feature group have localized accessible names in all eight dictionaries', () => {
  const html = read('src/app/ui/layouts/account-layout/account-layout.component.html');
  assert.ok(html.includes("[attr.aria-label]=\"'accountLayout.introductionLabel' | translate\""));
  assert.ok(html.includes("role=\"group\" [attr.aria-label]=\"'accountLayout.featuresLabel' | translate\""));
  assert.doesNotMatch(html, /aria-label="Account (experience introduction|feature highlights)"/);
  for (const language of ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl']) {
    const labels = JSON.parse(read(`src/assets/i18n/source/${language}/account/layout.json`)).accountLayout;
    assert.ok(labels.introductionLabel.trim());
    assert.ok(labels.featuresLabel.trim());
  }
});

test('auth fallback copy is localized without replacing existing dictionary values or server messages', () => {
  for (const language of ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl']) {
    const labels = JSON.parse(read(`src/assets/i18n/source/${language}/auth/auth.json`)).auth;
    for (const value of [labels.confirm.invalidLinkMessage, labels.confirm.fallbackError, labels.forgot_password.fallbackError, labels.reset_password.fallbackError]) assert.ok(value.trim());
  }
  const french = JSON.parse(read('src/assets/i18n/source/fr/auth/auth.json')).auth;
  assert.equal(french.confirm.invalidLinkMessage, 'Le lien de confirmation est invalide.');
  assert.equal(french.confirm.fallbackError, 'La confirmation du compte a échoué.');
  assert.equal(french.forgot_password.fallbackError, 'La demande a échoué.');
  assert.equal(french.reset_password.fallbackError, 'La réinitialisation du mot de passe a échoué.');
  for (const name of ['confirm-account', 'forgot-password', 'reset-password']) {
    const source = read(`src/app/features/auth/state/${name}-page-state.facade.ts`);
    assert.ok(source.includes('private readonly translateService: TranslateService'));
    assert.ok(source.includes("'Message' in nestedError"));
    assert.ok(source.includes("'message' in nestedError"));
  }
});

test('auth deep-link rendering uses a local noncredential token and never submits confirmation, reset or recovery', async () => {
  for (const language of ['fr', 'de', 'pl']) {
    const result = {};
    const navigations = [];
    const captures = [];
    await exerciseAccountAuthPages({ result, fixture: createAccountLayoutFixture('standard', 1792144800, language), expectedConfirmationMessage: 'Localized test message',
      evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page),
      navigateDocument: async path => navigations.push(path),
    });
    assert.equal(navigations.length, 5);
    assert.ok(navigations.every(path => path.startsWith(`/${language}/`)));
    assert.ok(navigations[2].endsWith('?token=APF-CI-UI-NOT-A-REAL-RESET-TOKEN'));
    assert.equal(captures.length, 5);
    assert.equal(result.authPages.confirmationNotRequested, true);
    assert.equal(result.authPages.resetNotSubmitted, true);
    assert.equal(result.authPages.recoveryEmailNotSent, true);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-account-auth-pages.mjs'), /fetch\(|requestSubmit\(|\.submit\(|\.click\(|\/api\//);
});

test('account layout fixtures reject production origins, unknown endpoints and all user-data mutations', () => {
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const profile = `${accountLayoutFixtureOrigin}/api/users/${fixture.user.id}`;
  const response = accountLayoutFixtureResponse({ method: 'GET', url: profile }, fixture);
  assert.equal(response.body.id, fixture.user.id);
  assert.equal(accountLayoutFixtureResponse({ method: 'GET', url: profile.replace(accountLayoutFixtureOrigin, 'https://amusement-parks.fun') }, fixture), null);
  for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) assert.equal(accountLayoutFixtureResponse({ method, url: profile }, fixture), null);
  for (const path of ['/api/admin/users', '/api/users', '/api/users/forgot-password', `/api/users/${fixture.user.id}?includePrivate=true`]) {
    assert.equal(accountLayoutFixtureResponse({ method: 'GET', url: accountLayoutFixtureOrigin + path }, fixture), null);
  }
  response.body.roles.push('ADMIN');
  assert.deepEqual(fixture.user.roles, ['USER']);
  for (const path of ['/api/me/collections', '/api/me/trips', '/api/me/park-fit/group-profiles']) {
    const request = { method: 'GET', url: accountLayoutFixtureOrigin + path };
    const list = accountLayoutFixtureResponse(request, fixture);
    assert.ok(list.body.length > 0);
    list.body.splice(0, list.body.length);
    assert.ok(accountLayoutFixtureResponse(request, fixture).body.length > 0);
    for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) assert.equal(accountLayoutFixtureResponse({ ...request, method }, fixture), null);
    assert.deepEqual(accountLayoutFixtureResponse(request, createAccountLayoutFixture('incomplete', 1792144800)).body, []);
    assert.equal(accountLayoutFixtureResponse(request, createAccountLayoutFixture('expired', 1792144800)), null);
  }
  assert.doesNotMatch(read('tools/design-v6/account-layout-fixtures.mjs'), /fetch\(|https:\/\/amusement-parks\.fun|password\s*:/);
});

test('private passport cursor fixture is exact, immutable and rejects arbitrary query or mutation', () => {
  const fixture = createAccountLayoutFixture('standard', 1792144800);
  const request = { method: 'GET', url: accountLayoutFixtureOrigin + '/api/me/passport/visits?limit=20' };
  const first = accountLayoutFixtureResponse(request, fixture);
  assert.equal(first.body.items.length, 2);
  assert.equal(first.body.nextCursor, 'apf-layout-next-page');
  const second = accountLayoutFixtureResponse({ ...request, url: request.url + '&cursor=apf-layout-next-page' }, fixture);
  assert.equal(second.body.items[0].id, first.body.items[0].id);
  assert.equal(second.body.nextCursor, null);
  second.body.items[0].title = 'Modified fixture copy';
  assert.notEqual(fixture.visits[0].title, 'Modified fixture copy');
  for (const query of ['', '?limit=100', '?limit=20&cursor=unknown', '?limit=20&includeHidden=true']) {
    assert.equal(accountLayoutFixtureResponse({ ...request, url: accountLayoutFixtureOrigin + '/api/me/passport/visits' + query }, fixture), null);
  }
  for (const method of ['POST', 'PATCH', 'DELETE']) assert.equal(accountLayoutFixtureResponse({ ...request, method }, fixture), null);
  assert.deepEqual(accountLayoutFixtureResponse(request, createAccountLayoutFixture('incomplete', 1792144800)).body, { items: [], nextCursor: null });
  assert.equal(accountLayoutFixtureResponse(request, createAccountLayoutFixture('expired', 1792144800)), null);
});

test('passport pagination and private Park Fit drafts compile without sharing or confirmed mutations', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const captures = [];
    const navigations = [];
    const result = {};
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    await exerciseAccountPassportAndFit({ fixture, result,
      evaluate: async expression => { assert.doesNotThrow(() => new Script(expression)); return expression.includes('index: navigation.currentEntry.index') ? { index: 1, key: 'original-profile' } : undefined; },
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page),
      navigateDocument: async path => navigations.push(path),
    });
    assert.deepEqual(navigations, ['/fr/profile/park-fit/profiles', '/fr/profile']);
    assert.ok(captures.includes('account-passport'));
    assert.ok(captures.includes('account-park-fit-draft'));
    assert.equal(captures.includes('account-passport-final-page'), kind === 'standard');
    assert.equal(captures.includes('account-park-fit-delete-warning'), kind === 'standard');
    assert.equal(result.passportAndFit.persistedWritesExercised, false);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-account-passport-and-fit.mjs'), /fetch\(|requestSubmit\(|\.submit\(|\/api\//);
});

test('compiled collection filters and trip draft recipe retain exact fixtures and never submit a persisted write', async () => {
  for (const kind of ['standard', 'incomplete']) {
    const captures = [];
    const result = {};
    const fixture = createAccountLayoutFixture(kind, 1792144800);
    await exerciseAccountLists({ fixture, result,
      evaluate: async expression => { assert.doesNotThrow(() => new Script(expression)); return expression.includes('path: location.pathname') ? { path: '/fr/profile', index: 2, key: 'exact-profile' } : undefined; },
      waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
      captureLayouts: async (_selector, page) => captures.push(page),
    });
    assert.deepEqual(captures, ['account-collections', 'account-trips', 'account-trip-draft']);
    assert.equal(result.lists.collections, fixture.collections.length);
    assert.equal(result.lists.trips, fixture.trips.length);
    assert.equal(result.lists.persistedWritesExercised, false);
  }
  assert.doesNotMatch(read('tools/design-v6/exercise-account-lists.mjs'), /fetch\(|requestSubmit\(|\.submit\(|\/api\//);
});

test('profile editor, export presence and deletion disclosure compile without persisted or destructive actions', async () => {
  const result = {};
  const captures = [];
  await exerciseAccountProfileControls({ result, fixture: createAccountLayoutFixture('standard', 1792144800),
    evaluate: async expression => assert.doesNotThrow(() => new Script(expression)),
    waitUntil: async expression => assert.doesNotThrow(() => new Script(expression)),
    captureLayouts: async (_selector, page) => captures.push(page),
  });
  assert.deepEqual(captures, ['account-identity-editor', 'account-deletion-warning']);
  assert.deepEqual(result.profileControls, { identityNotSaved: true, exportNotRequested: true, deletionNotSubmitted: true });
  assert.doesNotMatch(read('tools/design-v6/exercise-account-profile-controls.mjs'), /fetch\(|requestSubmit\(|\.submit\(|\/api\//);
});

test('compiled account capture is Linux-CI-only, never forwards API traffic and unloads the app before removing interception', () => {
  assert.equal(accountLayoutCaptureAllowed('true', 'linux'), true);
  assert.equal(accountLayoutCaptureAllowed(undefined, 'linux'), false);
  assert.equal(accountLayoutCaptureAllowed('true', 'win32'), false);
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  assert.ok(capture.includes('url.origin !== accountLayoutFixtureOrigin'));
  assert.ok(capture.includes('accountLayoutFixtureResponse(request, fixture, homeFixtures)'));
  assert.ok(capture.includes('no exact local fixture; never forwarded'));
  assert.ok(capture.includes('isCanceledInterceptionError(error, request.networkId, canceled)'));
  assert.ok(capture.includes('isRetiredDocumentInterceptionError(error, request.networkId, retired)'));
  assert.doesNotMatch(capture, /fetch\(|nativeFetch|spawn\(|request\.headers|request\.postData/);
  assert.ok(capture.indexOf("await cdp.send('Page.navigate', { url: 'about:blank' })") < capture.indexOf("await cdp.send('Fetch.disable')"));
  const script = read('tools/design-v6/capture-public-ssr.mjs');
  const fullCampaign = script.slice(script.indexOf('entry = await import(pathToFileURL'));
  assert.ok(fullCampaign.indexOf("'initial-theme-report.json'") >= 0);
  assert.ok(fullCampaign.indexOf('await captureAccountLayout(') > fullCampaign.indexOf("'initial-theme-report.json'"));
});

test('expired account UI can replay only exact bounded anonymous home data on its local origin', () => {
  const fixture = createAccountLayoutFixture('expired', 1792144800);
  const key = '/api/public-stats/home';
  const record = { status: 200, contentType: 'application/json', bytes: Buffer.from('{"parksCount":0}') };
  const fixtures = new Map([[key, record], ['/api/me/trips', record]]);
  const request = { method: 'GET', url: accountLayoutFixtureOrigin + key };
  assert.deepEqual(accountLayoutFixtureResponse(request, fixture, fixtures).body, { parksCount: 0 });
  for (const method of ['POST', 'PUT', 'DELETE']) assert.equal(accountLayoutFixtureResponse({ ...request, method }, fixture, fixtures), null);
  assert.equal(accountLayoutFixtureResponse({ ...request, url: request.url + '?unexpected=true' }, fixture, fixtures), null);
  assert.equal(accountLayoutFixtureResponse({ ...request, url: accountLayoutFixtureOrigin + '/api/me/trips' }, fixture, fixtures), null);
  assert.equal(accountLayoutFixtureResponse(request, createAccountLayoutFixture('standard', 1792144800), fixtures), null);
  assert.equal(accountLayoutFixtureResponse(request, fixture, new Map([[key, { ...record, bytes: Buffer.alloc(2097153) }]])), null);
  const capture = read('tools/design-v6/capture-account-layout.mjs');
  assert.ok(capture.includes("read.path.startsWith('/api/users/') || read.path.startsWith('/api/me/')"));
  assert.ok(capture.includes('closeUnobscured'));
  assert.ok(capture.includes('result.fixtureFailures.length || result.exceptions.length'));
});

test('anonymous auth recipe compiles every exact browser expression and never submits credentials or sends recovery email', async () => {
  const result = {};
  const captures = [];
  await exerciseAnonymousAuth({ result,
    evaluate: async expression => {
      assert.doesNotThrow(() => new Script(expression));
      if (expression.includes('path: location.pathname')) return { path: '/fr/home', index: 0, key: 'home-entry' };
      if (expression.includes('loginEmail:')) return { loginEmail: 'email', loginPassword: 'password', loginAutocomplete: 'current-password', registerAutocomplete: 'new-password', providerHost: true };
      if (expression === 'navigation.currentEntry.key') return 'recovery-entry';
    },
    waitUntil: async (expression, label) => assert.doesNotThrow(() => new Script(expression), label),
    capturePage: async label => captures.push(label),
  });
  assert.deepEqual(captures, ['forgot-password-empty']);
  assert.equal(result.anonymousAuth.recovery.emailNotSent, true);
  assert.equal(result.anonymousAuth.authenticatedAccountNotExercised, true);
  const source = read('tools/design-v6/exercise-anonymous-auth.mjs');
  assert.doesNotMatch(source, /fetch\(|requestSubmit\(|\.submit\(|localStorage\.|sessionStorage\.|\/api\//);
  assert.doesNotMatch(source, /button\[type=submit\].*\.click\(/);
});

const collectScss = (directory) => readdirSync(directory, { withFileTypes: true })
  .flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) {
      return collectScss(path);
    }
    return entry.name.endsWith('.scss') ? [readFileSync(path, 'utf8')] : [];
  });

const appRoutes = read('src/app/app.routes.ts');
const profileRoutes = read('src/app/features/profile/profile-routing.module.ts');
const authModal = read('src/app/features/auth/ui/auth-modal/auth-modal.component.html');
const login = read('src/app/features/auth/ui/login-form/login-form.component.html');
const register = read('src/app/features/auth/ui/register-form/register-form.component.html');
const confirmAccount = read('src/app/features/auth/pages/confirm-account-page/confirm-account-page.component.html');
const forgotPassword = read('src/app/features/auth/pages/forgot-password-page/forgot-password-page.component.html');
const resetPassword = read('src/app/features/auth/pages/reset-password-page/reset-password-page.component.html');
const profile = read('src/app/features/profile/pages/profile-page/profile-page.component.html');
const deletion = read('src/app/features/profile/components/account-deletion-panel/account-deletion-panel.component.html');
const parkFitProfiles = read('src/app/features/profile/park-fit/pages/park-fit-group-profiles-page.component.html');
const passportShare = read('src/app/features/profile/passport/pages/passport-profile-share-page/passport-profile-share-page.component.html');
const globalStatistics = read('src/app/features/profile/passport/pages/passport-global-statistics-page/passport-global-statistics-page.component.html');
const visitEditor = read('src/app/features/profile/passport/pages/passport-visit-editor-page/passport-visit-editor-page.component.html');
const tripList = read('src/app/features/profile/trips/pages/trip-list-page/trip-list-page.component.html');
const tripOverview = read('src/app/features/profile/trips/pages/trip-overview-page/trip-overview-page.component.html');
const tripPreferences = read('src/app/features/profile/trips/pages/trip-preferences-page/trip-preferences-page.component.html');
const tripExport = read('src/app/features/profile/trips/pages/trip-export-page/trip-export-page.component.html');

const accountStyles = [
  read('src/styles/_forms.scss'),
  ...collectScss(resolve(projectRoot, 'src/app/features/auth')),
  ...collectScss(resolve(projectRoot, 'src/app/features/profile')),
].join('\n');

test('all profile routes remain lazy and protected by the authentication guard', () => {
  const paths = [
    'notifications',
    'collections',
    'park-fit/profiles',
    'trips/:tripId/activity',
    'trips/:tripId/export',
    'trips/:tripId/passport',
    'trips/:tripId/coherence',
    'trips/:tripId/preference-summary',
    'trips/:tripId/preferences',
    'trips/:tripId',
    'trips',
    'passport/comparisons/invitations/:token',
    'passport/share',
    'passport/statistics/history',
    'passport/statistics',
    'passport/items/:parkItemId',
    'passport/parks/:parkId',
    'passport/years/:year',
    'passport',
    'visits/:visitId',
    '',
  ];
  for (const path of paths) {
    assert.ok(profileRoutes.includes(`path: '${path}'`), `${path || '(profile root)'} must remain registered`);
  }
  assert.equal(profileRoutes.match(/canActivate:\s*\[authGuard\]/g)?.length, paths.length);
  assert.equal(profileRoutes.match(/loadComponent:/g)?.length, paths.length);
});

test('account recovery routes keep the isolated account layout and original paths', () => {
  assert.ok(appRoutes.includes("import('@ui/layouts/account-layout/account-layout.component')"));
  for (const path of ['confirm-account', 'forgot-password', 'reset-password']) {
    assert.ok(appRoutes.includes(`path: '${path}'`), `${path} must remain registered`);
  }
});

test('authentication keeps Google, login, registration, validation and recovery contracts', () => {
  assert.ok(authModal.includes('#googleButtonContainer'));
  assert.ok(authModal.includes('<app-register-form>'));
  assert.ok(authModal.includes('(loginSuccess)="onLoginSuccess()"'));
  for (const contract of [
    '(ngSubmit)="onSubmit()"',
    'autocomplete="email"',
    'autocomplete="current-password"',
    '(click)="navigateToForgotPassword()"',
  ]) {
    assert.ok(login.includes(contract), `${contract} must remain in login`);
  }
  assert.equal(register.match(/autocomplete="new-password"/g)?.length, 2);
  assert.ok(register.includes('[disabled]="!isValidEmail() || !isValidPassword() || !passwordsMatch()"'));
  assert.ok(register.includes('(click)="resendConfirmation()"'));
  assert.ok(confirmAccount.includes('[state]="state().kind"'));
  assert.ok(forgotPassword.includes('(ngSubmit)="onSubmit()"'));
  assert.ok(resetPassword.includes('(ngSubmit)="onSubmit()"'));
  assert.ok(resetPassword.includes('new-password'));
});

test('profile identity and destructive account actions keep every explicit confirmation', () => {
  for (const contract of [
    '[identityDraft]="identityDraft"',
    '(saveIdentityClicked)="saveIdentity()"',
    '(logoutClicked)="logout()"',
    '(avatarUploaded)="onAvatarUploaded($event)"',
    '(accountDeletionAccepted)="onAccountDeletionAccepted()"',
  ]) {
    assert.ok(profile.includes(contract), `${contract} must remain in profile`);
  }
  for (const contract of [
    '(click)="open()"',
    'autocomplete="email"',
    'autocomplete="current-password"',
    '(click)="submit()"',
    '(click)="cancel()"',
  ]) {
    assert.ok(deletion.includes(contract), `${contract} must remain in account deletion`);
  }
});

test('Park Fit and passport keep editing, sharing, statistics and deletion workflows', () => {
  assert.ok(parkFitProfiles.includes('[formGroup]="form"'));
  assert.ok(parkFitProfiles.includes('(ngSubmit)="submit()"'));
  assert.ok(parkFitProfiles.includes('(click)="edit(profile)"'));
  assert.ok(parkFitProfiles.includes('(click)="requestDelete(profile)"'));
  for (const contract of ['preview()', 'publish()', 'revoke()', 'visibility']) {
    assert.ok(passportShare.includes(contract), `${contract} must remain in passport sharing`);
  }
  assert.ok(globalStatistics.includes('(click)="facade.retry()"'));
  assert.ok(globalStatistics.includes('(click)="openHistoricalStatistics()"'));
  for (const contract of [
    '(click)="facade.saveVisitMetadata()"',
    '(click)="facade.completeVisit()"',
    '(click)="facade.reopenVisit()"',
    '(click)="requestVisitDeletion()"',
    '(click)="saveAssessment()"',
  ]) {
    assert.ok(visitEditor.includes(contract), `${contract} must remain in visit editing`);
  }
});

test('trip planning keeps creation, dates, preferences, participants and exports', () => {
  assert.ok(tripList.includes('(click)="create()"'));
  assert.ok(tripList.includes('(click)="open(trip)"'));
  for (const contract of [
    '(click)="saveDates()"',
    '(click)="saveDay(date)"',
    '(click)="importSelectedWishlist()"',
    '<app-trip-participant-panel',
    "'export'",
    "'passport'",
  ]) {
    assert.ok(tripOverview.includes(contract), `${contract} must remain in trip overview`);
  }
  assert.ok(tripPreferences.includes('(click)="facade.discardChanges()"'));
  assert.ok(tripPreferences.includes('(click)="facade.save()"'));
  assert.ok(tripExport.includes('(click)="print()"'));
  assert.ok(tripExport.includes('(click)="downloadJson()"'));
});

test('account and profile surfaces use the flat Playground visual language', () => {
  assert.match(accountStyles, /\.auth-form-card--login\s*\{[^}]*border-top:\s*4px solid var\(--c-sky\)/s);
  assert.match(accountStyles, /\.profile-hero\s*\{[^}]*border-top:\s*4px solid var\(--app-primary\)/s);
  assert.match(accountStyles, /\.passport-overview__hero\s*\{[^}]*border-top:\s*4px solid var\(--c-gold\)/s);
  assert.doesNotMatch(accountStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(accountStyles, /backdrop-filter\s*:/);
});
