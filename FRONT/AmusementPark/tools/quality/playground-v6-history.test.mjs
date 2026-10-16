import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { Script } from 'node:vm';
import { exercisePublicHistoryExplorer } from '../design-v6/exercise-public-history-explorer.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

test('historical comparison aligns desktop year fields without forcing shared rows on stacked mobile fields', () => {
  const source = read('src/app/features/public/history/pages/park-history-comparison-page.component.scss');
  assert.match(source, /@media \(min-width: 521px\)\s*\{\s*\.comparison-picker__fields label\s*\{\s*grid-row: 1;\s*\}\s*\.comparison-picker__fields input,\s*\.comparison-picker__fields > span\s*\{\s*grid-row: 2;/);
  const browser = read('tools/design-v6/capture-account-layout.mjs');
  assert.ok(browser.includes("page === 'isolated-history-comparison' && width > 520"));
  assert.ok(browser.includes('Math.abs(layout.yearFields[0].top - layout.yearFields[1].top) > 1'));
  assert.ok(browser.includes('historical comparison year fields not aligned'));
});

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
  for (const token of ['history-narrative-link', 'history-sources', 'history-article__content p', 'history-article-body', 'timeline.key', 'exercisePublicHistoryExplorer']) assert.ok(recipe.includes(token));
  assert.doesNotMatch(recipe, /fetch\(|localStorage\.setItem|new Date\(/);
});

test('published history explorer uses native links/forms, exact back entries and records unavailable data without manufacturing it', () => {
  const recipe = read('tools/design-v6/exercise-public-history-explorer.mjs');
  for (const token of ['history-comparison-callout', 'canonical-history-event__header a[href]', 'history-lineage-link', 'park-history-pagination',
    'navigation.currentEntry.key', 'snapshotMonth', 'snapshotDay', 'requestSubmit()', 'comparison-picker__error', 'comparison-stable', 'evidence.unavailable.push']) assert.ok(recipe.includes(token), token);
  assert.doesNotMatch(recipe, /fetch\(|localStorage\.|sessionStorage\.|new Date\(|\/api\/|history\.pushState|__ngContext__/);
  assert.doesNotMatch(recipe, /[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}/i);
});

test('a real timeline without decision snapshots records missing coverage and does not invent navigation', async () => {
  const result = {};
  let evaluations = 0;
  await exercisePublicHistoryExplorer({
    result,
    evaluate: async expression => {
      evaluations++;
      assert.doesNotThrow(() => new Script(expression));
      return { path: '/fr/park/test/name/history', key: 'native-entry', index: 2, events: 3,
        next: null, snapshot: null, comparison: null, lineage: null };
    },
    waitUntil: async () => assert.fail('Missing public links must not trigger navigation'),
    capturePage: async () => assert.fail('Unavailable content must not be captured as covered'),
  });
  assert.equal(evaluations, 1);
  assert.equal(result.historyExplorer.unavailable.length, 4);
  assert.deepEqual(result.historyExplorer.checks, []);
});

test('every generated history browser expression parses, including dates, invalid ranges and restored entries', async () => {
  const result = {};
  const captures = [];
  const base = '/fr/park/test/name/history';
  let expressions = 0;
  await exercisePublicHistoryExplorer({
    result,
    evaluate: async expression => {
      expressions++;
      assert.doesNotThrow(() => new Script(expression));
      if (expression.includes('next: document.querySelector')) return { path: base, key: 'timeline-entry', index: 2, events: 50,
        next: `${base}/page/2`, snapshot: `${base}/2016`, comparison: `${base}/compare/2016/2026`, lineage: '/fr/history/subject/test' };
      if (expression.includes('summary: document.querySelector')) return { path: `${base}/page/2`, events: 3, summary: '2 / 2' };
      if (expression.includes('coverageFields:')) return { path: `${base}/2016`, year: 2016, coverageFields: 4 };
      if (expression.includes('return { year:')) return { year: 2016, month: 1 };
      if (expression.includes('return Number(option.value')) return 1;
      if (expression.includes('counts: [...document.querySelectorAll')) return { from: 2016, to: 2026, counts: [1, 2, 3, 4] };
      if (expression === 'document.querySelectorAll(\'.comparison-stable li\').length') return 5;
      if (expression.includes('summaryFields:')) return { path: '/fr/history/subject/test', title: 'Published subject', summaryFields: 2 };
    },
    waitUntil: async (expression, label) => {
      expressions++;
      assert.doesNotThrow(() => new Script(expression), label);
    },
    capturePage: async label => captures.push(label),
  });
  assert.ok(expressions > 30);
  assert.deepEqual(captures, ['history-snapshot', 'history-comparison', 'history-lineage']);
  assert.deepEqual(result.historyExplorer.unavailable, []);
  assert.deepEqual(result.historyExplorer.snapshot.selectedDate, { year: 2016, month: 1, day: 1 });
  assert.equal(result.historyExplorer.comparison.selectedPath, `${base}/compare/2025/2026`);
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
