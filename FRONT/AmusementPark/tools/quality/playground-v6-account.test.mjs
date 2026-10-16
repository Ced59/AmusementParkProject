import assert from 'node:assert/strict';
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import test from 'node:test';
import { Script } from 'node:vm';
import { exerciseAnonymousAuth } from '../design-v6/exercise-anonymous-auth.mjs';
import { accountLayoutFixtureOrigin, createAccountLayoutFixture, accountLayoutFixtureResponse } from '../design-v6/account-layout-fixtures.mjs';

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
  assert.throws(() => createAccountLayoutFixture('admin', now), /Unknown/);
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
  assert.doesNotMatch(read('tools/design-v6/account-layout-fixtures.mjs'), /fetch\(|https:\/\/amusement-parks\.fun|password\s*:/);
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
