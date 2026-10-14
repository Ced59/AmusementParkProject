import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const rankingsTemplate = read('src/app/features/public/ratings/pages/rankings-page.component.html');
const rankingsStyles = read('src/app/features/public/ratings/pages/rankings-page.component.scss');
const methodologyTemplate = read('src/app/features/public/ratings/pages/rating-methodology-page.component.html');
const methodologyStyles = read('src/app/features/public/ratings/pages/rating-methodology-page.component.scss');
const sharedRankingsTemplate = read('src/app/features/public/ratings/pages/shared-user-rankings-page.component.html');
const sharedRankingsStyles = read('src/app/features/public/ratings/pages/shared-user-rankings-page.component.scss');
const commentsTemplate = read('src/app/features/public/comments/pages/comments-page.component.html');
const commentsStyles = read('src/app/features/public/comments/pages/comments-page.component.scss');
const collectionsTemplate = read('src/app/features/collections/pages/user-collections-page.component.html');
const collectionsStyles = read('src/app/features/collections/pages/user-collections-page.component.scss');
const notificationsTemplate = read('src/app/features/watchlists/pages/user-notifications-page.component.html');
const notificationsStyles = read('src/app/features/watchlists/pages/user-notifications-page.component.scss');
const watchTemplate = read('src/app/features/watchlists/ui/watch-subscription-action.component.html');
const shareTemplate = read('src/app/ui/sharing/public-share-panel/public-share-panel.component.html');
const shareReportTemplate = read('src/app/ui/sharing/public-share-report/public-share-report.component.html');
const shareStyles = read('src/app/ui/sharing/public-share-panel/public-share-panel.component.scss');
const ratingInputStyles = read('src/app/shared/components/rating-input/rating-input.component.scss');

test('rankings preserve filters, searches, methodology evidence and incremental loading', () => {
  for (const contract of [
    '(click)="selectFilter(filter)"',
    '(change)="selectAttractionType($any($event.target).value)"',
    '(keyup.enter)="applySearch()"',
    '(click)="clearSearch()"',
    '<app-rating-ranking-list',
    '<app-rating-tree',
    '(click)="loadMore()"',
    'method.evidenceThresholds.eligible',
    "['/', currentLang(), 'rankings', 'methodology', method.version]",
  ]) {
    assert.ok(rankingsTemplate.includes(contract), `${contract} must remain in rankings`);
  }

  for (const contract of ['method.evidenceThresholds', 'method.parkComposition', 'history(); track entry.version']) {
    assert.ok(methodologyTemplate.includes(contract), `${contract} must remain in methodology`);
  }

  for (const contract of [
    '(click)="selectFilter(filter)"',
    '(click)="selectAttractionQuickFilter(filter)"',
    '(click)="applySearch()"',
    '<app-rating-ranking-list',
    '<app-rating-tree',
    '<app-public-share-panel',
  ]) {
    assert.ok(sharedRankingsTemplate.includes(contract), `${contract} must remain in shared rankings`);
  }
});

test('comments preserve localized rich text, personal moderation, states and language filtering', () => {
  for (const contract of [
    '[formGroup]="editorForm"',
    '(ngSubmit)="submit()"',
    '<app-localized-rich-text-editor',
    '(click)="showCommentsFromCurrentLanguage()"',
    '(click)="showCommentsFromAllLanguages()"',
    '(click)="startEditing(commentView.comment)"',
    '(click)="deleteComment(commentView.comment)"',
    'safeCommentRichHtml',
  ]) {
    assert.ok(commentsTemplate.includes(contract), `${contract} must remain in comments`);
  }
});

test('collections and watch notifications preserve authenticated actions and every state', () => {
  for (const contract of [
    "facade.status() === 'loading'",
    "facade.status() === 'error'",
    '(click)="facade.load()"',
    '(click)="facade.remove(entry)"',
    '[routerLink]="route"',
  ]) {
    assert.ok(collectionsTemplate.includes(contract), `${contract} must remain in collections`);
  }

  for (const contract of [
    '(click)="facade.setUnreadOnly(false)"',
    '(change)="onParkChanged($event)"',
    '(change)="onEventTypeChanged($event)"',
    '(click)="facade.markAllRead()"',
    '(click)="facade.reportMisleading(notification.notificationId)"',
    '(click)="facade.unsubscribe(notification)"',
    '(pageChanged)="onPageChanged($event)"',
  ]) {
    assert.ok(notificationsTemplate.includes(contract), `${contract} must remain in notifications`);
  }

  for (const contract of [
    '@if (facade.authenticated())',
    '(click)="facade.toggleGroup(group.eventTypes)"',
    '(click)="facade.save()"',
    '(click)="facade.togglePaused()"',
    '(click)="facade.remove()"',
  ]) {
    assert.ok(watchTemplate.includes(contract), `${contract} must remain in watch actions`);
  }
});

test('sharing preserves native, copied, network, QR and moderation workflows', () => {
  for (const contract of [
    '(click)="shareNative()"',
    '(click)="copyLink()"',
    '(click)="trackLink(network.channel)"',
    '(click)="toggleQrCode()"',
    'qrCodeLoading()',
    'qrCodeDataUrl()',
    'qrCodeError()',
  ]) {
    assert.ok(shareTemplate.includes(contract), `${contract} must remain in share panel`);
  }
  assert.ok(shareReportTemplate.includes('[disabled]="!canSubmit()"'));
  assert.ok(shareReportTemplate.includes('(click)="submit()"'));
});

test('social surfaces use flat Playground panels without decorative gradients', () => {
  const styles = [
    rankingsStyles,
    methodologyStyles,
    sharedRankingsStyles,
    commentsStyles,
    collectionsStyles,
    notificationsStyles,
    shareStyles,
    ratingInputStyles,
  ].join('\n');

  assert.match(rankingsStyles, /\.rankings-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(commentsStyles, /\.comments-hero[\s\S]*?border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(collectionsStyles, /\.collections-page__hero[\s\S]*?border-top:\s*4px solid var\(--c-rose\)/);
  assert.match(notificationsStyles, /\.notifications-page__hero[\s\S]*?border-top:\s*4px solid var\(--c-sky\)/);
  assert.match(shareStyles, /\.public-share-panel[\s\S]*?border-top:\s*4px solid var\(--app-accent-cyan\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /backdrop-filter\s*:/);
});
