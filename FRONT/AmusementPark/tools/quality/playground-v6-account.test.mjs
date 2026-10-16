import assert from 'node:assert/strict';
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import test from 'node:test';
import { Script } from 'node:vm';
import { exerciseAnonymousAuth } from '../design-v6/exercise-anonymous-auth.mjs';
import { accountLayoutFixtureOrigin, createAccountLayoutFixture, accountLayoutFixtureResponse, accountLayoutCases } from '../design-v6/account-layout-fixtures.mjs';
import { accountLayoutCaptureAllowed } from '../design-v6/capture-account-layout.mjs';
import { exerciseAccountLists } from '../design-v6/exercise-account-lists.mjs';
import { exerciseAccountProfileControls } from '../design-v6/exercise-account-profile-controls.mjs';
import { exerciseAccountPassportAndFit } from '../design-v6/exercise-account-passport-and-fit.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

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
  assert.equal(claims.exp, now + 3600);
  assert.equal(claims.aud, 'apf-ci-layout-only');
  assert.equal(incomplete.user.firstName, '');
  assert.equal(incomplete.user.isActivated, true);
  assert.equal(standard.user.preferredMeasurementSystem, 'Metric');
  assert.throws(() => createAccountLayoutFixture('admin', now), /Unknown/);
});

test('account acceptance retains all French widths and explicit two-theme mobile/desktop cases in the other seven languages', () => {
  const widths = [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920];
  const cases = accountLayoutCases(widths);
  assert.equal(cases.length, 10);
  assert.deepEqual(cases.slice(0, 3).map(item => item.kind), ['standard', 'incomplete', 'expired']);
  assert.deepEqual(cases.slice(0, 3).map(item => item.widths), [widths, widths, widths]);
  assert.deepEqual(cases.slice(3).map(item => item.language), ['en', 'de', 'nl', 'it', 'es', 'pt', 'pl']);
  for (const item of cases.slice(3)) {
    assert.deepEqual(item.widths, [390, 1366]);
    assert.equal(createAccountLayoutFixture(item.kind, 1792144800, item.language).user.preferredLanguage, item.language.toUpperCase());
  }
  assert.throws(() => accountLayoutCases([320]), /390 and 1366/);
  assert.throws(() => accountLayoutCases([...widths, 390]), /bounded widths/);
  assert.throws(() => createAccountLayoutFixture('standard', 1792144800, 'unknown'), /Unknown/);
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
  assert.ok(script.indexOf('await captureAccountLayout(') > script.indexOf("'initial-theme-report.json'"));
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
