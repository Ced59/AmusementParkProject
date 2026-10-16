import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const routes = read('src/app/app.routes.ts');
const timelineTemplate = read('src/app/features/public/history/pages/history-timeline-page.component.html');
const parkTimelineTemplate = read('src/app/features/public/history/pages/park-history-timeline-page.component.html');
const articleTemplate = read('src/app/features/public/history/pages/history-article-page.component.html');
const snapshotTemplate = read('src/app/features/public/history/pages/park-historical-snapshot-page.component.html');
const comparisonTemplate = read('src/app/features/public/history/pages/park-history-comparison-page.component.html');
const lineageTemplate = read('src/app/features/public/history/pages/historical-lineage-page.component.html');

const historyStyles = [
  'history-timeline-page.component.scss',
  'history-article-page.component.scss',
  'historical-lineage-page.component.scss',
  'park-history-explorer.shared.scss',
  'park-history-timeline-page.component.scss',
  'park-historical-snapshot-page.component.scss',
  'park-history-comparison-page.component.scss',
].map((file) => read(`src/app/features/public/history/pages/${file}`)).join('\n');

test('history browser acceptance opens real source disclosures and complete narrative links without fabricating unavailable variants', () => {
  const recipe = read('tools/design-v6/exercise-public-media-history.mjs');
  for (const token of ['history-narrative-link', 'history-sources', 'history-article__content p', 'history-article-body', 'timeline.key', 'snapshotsAndComparisonNotExercised: true', 'serverPaginationNotExercised: true']) assert.ok(recipe.includes(token));
  assert.doesNotMatch(recipe, /fetch\(|localStorage\.setItem|new Date\(/);
});

test('small timeline labels and narrative links use readable semantic text accents', () => {
  for (const selector of ['canonical-history-event__type', 'history-narrative-link']) {
    assert.match(historyStyles, new RegExp(`\\.${selector}\\s*\\{[^}]*color:\\s*var\\(--app-primary-strong\\)`));
  }
});

test('historical routes keep resolvers, deep links, years and pagination', () => {
  for (const contract of [
    "path: 'attraction/:standaloneAttractionId/:slug/history/page/:page'",
    "path: 'park/:id/:slug/history/page/:page'",
    "path: 'park/:id/:slug/history/compare/:fromYear/:toYear'",
    "path: 'park/:id/:slug/history/:eventId/:eventSlug'",
    "path: 'park/:id/:slug/history/:year'",
    "path: 'park/:id/:slug/item/:itemId/:itemSlug/history/:eventId/:eventSlug'",
    'historyTimelineResolver',
    'historyArticleResolver',
    'parkHistoryTimelineResolver',
    'parkHistoricalSnapshotResolver',
    'parkHistoricalComparisonResolver',
  ]) {
    assert.ok(routes.includes(contract), `${contract} must remain in historical routing`);
  }
});

test('timelines preserve dates, sources, articles, lineages and server pagination', () => {
  for (const contract of [
    'currentTimeline.pagination?.totalItems',
    '[routerLink]="pageLink(currentTimeline, range.page)"',
    'event.dateLabel',
    'event.sourceCount',
    'event.articleLink',
  ]) {
    assert.ok(timelineTemplate.includes(contract), `${contract} must remain in the generic timeline`);
  }

  for (const contract of [
    'currentTimeline.pagination.totalItems',
    'snapshotLink(event, currentTimeline)',
    '[routerLink]="yearLink"',
    'narrativeLink(event, currentTimeline)',
    'lineageLink(event, currentTimeline)',
    'event.sources.length',
    '[routerLink]="previousPage(currentTimeline)"',
    '[routerLink]="nextPage(currentTimeline)"',
  ]) {
    assert.ok(parkTimelineTemplate.includes(contract), `${contract} must remain in the park timeline`);
  }
});

test('articles preserve complete narrative, media, sources and sharing', () => {
  for (const contract of [
    'currentArticle.dateLabel',
    '[routerLink]="currentArticle.timelineLink"',
    '[imageId]="currentArticle.mainImageId"',
    '@for (block of currentArticle.blocks',
    'currentArticle.sources.length',
    '[href]="source.url"',
    '<app-public-share-panel',
  ]) {
    assert.ok(articleTemplate.includes(contract), `${contract} must remain in the history article`);
  }
});

test('snapshots, comparisons and lineages preserve their evidence models', () => {
  for (const contract of [
    '(ngSubmit)="openSelectedDate(currentSnapshot)"',
    'currentSnapshot.requestedInstant',
    'comparisonLink(currentSnapshot)',
    '<app-public-share-panel',
  ]) {
    assert.ok(snapshotTemplate.includes(contract), `${contract} must remain in the historical snapshot`);
  }

  for (const contract of [
    '(ngSubmit)="compareSelectedYears(currentComparison)"',
    'currentComparison.fromInstant.year',
    'currentComparison.toInstant.year',
    'opened()',
    'closed()',
    'changed()',
    'uncertain()',
    'stable()',
  ]) {
    assert.ok(comparisonTemplate.includes(contract), `${contract} must remain in historical comparison`);
  }

  for (const contract of [
    'currentLineage.subjects.length',
    'currentLineage.relations.length',
    'relation.sources.length',
    '[href]="sourceReference.url"',
  ]) {
    assert.ok(lineageTemplate.includes(contract), `${contract} must remain in historical lineages`);
  }
});

test('historical surfaces follow the flat Playground system', () => {
  assert.match(historyStyles, /\.history-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(historyStyles, /\.history-article__hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(historyStyles, /\.historical-lineage-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(historyStyles, /\.park-history-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.doesNotMatch(historyStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(historyStyles, /mask-image\s*:/);
});
