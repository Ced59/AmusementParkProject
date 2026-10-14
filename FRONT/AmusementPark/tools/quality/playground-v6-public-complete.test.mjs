import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const routes = read('src/app/app.routes.ts');
const parkFitStart = read('src/app/features/public/park-fit/pages/park-fit-start-page.component.html');
const parkFitResults = read('src/app/features/public/park-fit/pages/park-fit-results-page.component.html');
const parkFitComparison = read('src/app/features/public/park-fit/pages/park-fit-comparison-page.component.html');
const manufacturers = read('src/app/features/public/manufacturers/pages/manufacturers-page.component.html');
const referenceDetail = read('src/app/features/public/parks/ui/park-reference-detail-view.component.html');
const sharedPassport = read('src/app/features/public/passport/pages/shared-passport-profile-page.component.html');
const sharedVisit = read('src/app/features/public/passport/pages/shared-visit-recap-page.component.html');
const sharedYear = read('src/app/features/public/passport/pages/shared-year-recap-page.component.html');
const sharedComparison = read('src/app/features/public/passport/pages/shared-profile-comparison-page.component.html');
const anonymousDrafts = read('src/app/features/profile/passport/anonymous-drafts/pages/passport-anonymous-drafts-page/passport-anonymous-drafts-page.component.html');
const anonymousEditor = read('src/app/features/profile/passport/anonymous-drafts/pages/passport-anonymous-draft-editor-page/passport-anonymous-draft-editor-page.component.html');
const anonymousImport = read('src/app/features/profile/passport/anonymous-drafts/components/passport-anonymous-import-panel/passport-anonymous-import-panel.component.html');
const contact = read('src/app/features/public/contact/pages/contact-page.component.html');
const privacy = read('src/app/features/public/legal/pages/privacy-policy-page.component.html');
const sitemap = read('src/app/features/public/sitemap/pages/public-sitemap-page.component.html');
const technicalDetail = read('src/app/features/public/technical-pages/pages/technical-page-detail-page.component.html');
const tripInvitation = read('src/app/features/public/trips/pages/trip-invitation-preview-page/trip-invitation-preview-page.component.html');
const notFound = read('src/app/features/public/not-found/pages/public-not-found-page.component.html');

const secondaryStyles = [
  'src/app/features/public/about/pages/about.component.scss',
  'src/app/features/public/contact/pages/contact-page.component.scss',
  'src/app/features/public/legal/pages/privacy-policy-page.component.scss',
  'src/app/features/public/manufacturers/pages/manufacturers-page.component.scss',
  'src/app/features/public/not-found/pages/public-not-found-page.component.scss',
  'src/app/features/public/park-fit/pages/park-fit-start-page.component.scss',
  'src/app/features/public/park-fit/pages/park-fit-results-page.component.scss',
  'src/app/features/public/park-fit/pages/park-fit-comparison-page.component.scss',
  'src/app/features/public/passport/pages/shared-passport-profile-page.component.scss',
  'src/app/features/public/passport/pages/shared-visit-recap-page.component.scss',
  'src/app/features/public/passport/pages/shared-year-recap-page.component.scss',
  'src/app/features/public/passport/pages/shared-profile-comparison-page.component.scss',
  'src/app/features/public/parks/ui/park-reference-detail-view.component.scss',
  'src/app/features/public/sitemap/pages/public-sitemap-page.component.scss',
  'src/app/features/public/technical-pages/pages/technical-pages-page.component.scss',
  'src/app/features/public/technical-pages/pages/technical-page-detail-page.component.scss',
  'src/app/features/public/trips/pages/trip-invitation-preview-page/trip-invitation-preview-page.component.scss',
  'src/app/features/public/version-history/pages/version-history-page.component.scss',
  'src/app/features/profile/passport/anonymous-drafts/pages/passport-anonymous-drafts-page/passport-anonymous-drafts-page.component.scss',
  'src/app/features/profile/passport/anonymous-drafts/pages/passport-anonymous-draft-editor-page/passport-anonymous-draft-editor-page.component.scss',
].map(read).join('\n');

test('every secondary public route remains registered with its original shape', () => {
  for (const path of [
    "path: 'park-fit/compare'",
    "path: 'park-fit/results'",
    "path: 'park-fit'",
    "path: 'trip-invitations/:token'",
    "path: 'sitemap'",
    "path: 'technical/:slug'",
    "path: 'manufacturers'",
    "path: 'passport/shared/visits/:shareId'",
    "path: 'passport/shared/years/:shareId'",
    "path: 'passport/shared/profiles/:shareId'",
    "path: 'passport/shared/comparisons/:shareId'",
    "path: 'passport/local/:draftId'",
    "path: 'about'",
    "path: 'contact'",
    "path: 'versions'",
    "path: 'privacy'",
    "path: 'not-found'",
    "path: 'park-operator/:id/:slug'",
    "path: 'park-founder/:id/:slug'",
    "path: 'park-manufacturer/:id/:slug'",
  ]) {
    assert.ok(routes.includes(path), `${path} must remain registered`);
  }
});

test('Park Fit preserves its complete conditional questionnaire, evidence and comparison', () => {
  for (const contract of [
    '[formGroup]="form"',
    '(ngSubmit)="submit()"',
    'formControlName="evaluationDate"',
    '(click)="requestLocation()"',
    'formArrayName="members"',
    'formControlName="heightCentimeters"',
    'formControlName="ageYears"',
    'formControlName="canBeAccompanied"',
    '(click)="togglePreference(option.value)"',
  ]) {
    assert.ok(parkFitStart.includes(contract), `${contract} must remain in Park Fit`);
  }
  for (const contract of [
    '<app-park-fit-trust-notice',
    '<app-park-fit-source-report',
    'park.memberSummaries',
    'park.dataQualityCoveragePercent',
    '(click)="toggleComparison(park.parkId)"',
    '[routerLink]="comparisonRoute()"',
  ]) {
    assert.ok(parkFitResults.includes(contract), `${contract} must remain in Park Fit results`);
  }
  assert.ok(parkFitComparison.includes('(click)="toggleDifferences()"'));
  assert.ok(parkFitComparison.includes('[style.--park-count]="parks().length"'));
});

test('business references preserve search, rich content and related entity routes', () => {
  assert.ok(manufacturers.includes('(click)="clearSearch()"'));
  assert.ok(manufacturers.includes('[routerLink]="route"'));
  for (const contract of [
    '(click)="goBack()"',
    'safeRichHtml',
    '[href]="fact.href"',
    '[routerLink]="attraction.routerLink"',
  ]) {
    assert.ok(referenceDetail.includes(contract), `${contract} must remain in reference details`);
  }
});

test('shared and anonymous passports preserve visits, years, comparisons and local import', () => {
  for (const template of [sharedPassport, sharedVisit, sharedYear, sharedComparison]) {
    assert.ok(template.includes('role="status"'));
    assert.ok(template.includes('role="alert"'));
    assert.ok(template.includes('<app-public-share-report'));
  }
  assert.ok(sharedPassport.includes('shared.passportProfile.personalRanking'));
  assert.ok(sharedVisit.includes('shared.visitRecap.items'));
  assert.ok(sharedYear.includes('shared.yearRecap'));
  assert.ok(sharedComparison.includes('comparison.categories.includes("PersonalRatings")'));
  assert.ok(sharedComparison.includes('comparison.categories.includes("YearlyActivity")'));
  assert.ok(anonymousDrafts.includes('(click)="goToParks()"'));
  assert.ok(anonymousDrafts.includes('(click)="openDraft(draft.id)"'));
  assert.ok(anonymousEditor.includes('(ngSubmit)="addRide()"'));
  assert.ok(anonymousImport.includes('(click)="importDrafts()"'));
  assert.ok(anonymousImport.includes('(change)="setChoice(preview.draft.id, \'Merge\')"'));
});

test('secondary pages preserve legal, contact, sitemap, rich content, invitation and recovery actions', () => {
  assert.ok(contact.includes('[formGroup]="contactForm"'));
  assert.ok(contact.includes('(ngSubmit)="submit()"'));
  assert.ok(privacy.includes('(click)="acceptOptionalCookies()"'));
  assert.ok(privacy.includes('(click)="refuseOptionalCookies()"'));
  assert.ok(privacy.includes('(click)="resetCookieChoice()"'));
  assert.ok(sitemap.includes('[queryParams]="nextQueryParams()"'));
  assert.ok(sitemap.includes('rel="prev"'));
  assert.ok(technicalDetail.includes('safeRichHtml'));
  assert.ok(tripInvitation.includes('(click)="continueWithAccount()"'));
  assert.ok(tripInvitation.includes('(click)="decline()"'));
  assert.ok(notFound.includes("['/', currentLang(), 'home']"));
  assert.ok(notFound.includes("['/', currentLang(), 'parks']"));
});

test('secondary public surfaces follow the flat Playground language', () => {
  assert.match(secondaryStyles, /\.park-fit-hero[\s\S]*?border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(secondaryStyles, /\.shared-passport__story[^}]*border-top:\s*4px solid var\(--c-gold\)/);
  assert.match(secondaryStyles, /\.technical-detail__hero[\s\S]*?border-top:\s*4px solid var\(--app-accent-cyan\)/);
  assert.match(secondaryStyles, /\.park-reference-hero__bg[\s\S]*?border-top:\s*4px solid var\(--reference-accent/);
  assert.doesNotMatch(secondaryStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(secondaryStyles, /backdrop-filter\s*:/);
});
