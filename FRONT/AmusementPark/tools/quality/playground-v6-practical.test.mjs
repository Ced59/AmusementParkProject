import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { publicPracticalReadEvidence, openingCalendarLayoutIssues } from '../design-v6/public-practical-evidence.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const openingTemplate = read('src/app/features/public/parks/pages/park-opening-hours-page.component.html');
const weatherTemplate = read('src/app/features/public/parks/pages/park-weather-page.component.html');
const pricingTemplate = read('src/app/features/public/parks/pages/park-pricing-page.component.html');
const nearbyTemplate = read('src/app/features/public/parks/ui/park-nearby-section.component.html');
const liveTemplate = read('src/app/features/public/live/ui/public-live-panel.component.html');
const liveForecastTemplate = read('src/app/features/public/live/ui/public-live-forecast-card.component.html');
const liveHistoryTemplate = read('src/app/features/public/live/ui/public-live-history-panel.component.html');

const practicalStyles = [
  'src/app/features/public/parks/ui/park-weather-card.component.scss',
  'src/app/features/public/parks/ui/park-opening-hours-card.component.scss',
  'src/app/features/public/parks/pages/park-weather-page.component.scss',
  'src/app/features/public/parks/pages/park-opening-hours-page.component.scss',
  'src/app/features/public/parks/pages/park-pricing-page.component.scss',
  'src/app/features/public/live/ui/public-live-panel.component.scss',
  'src/app/features/public/live/ui/public-live-forecast-card.component.scss',
  'src/app/features/public/live/ui/public-live-history-panel.component.scss',
].map(read).join('\n');

test('practical evidence retains only bounded public comparison-year counts, not weather payloads', () => {
  const path = '/api/parks/00000000-0000-0000-0000-000000000001/weather/historical-comparisons?days=7&years=10';
  const bytes = Buffer.from(JSON.stringify({ years: [{ days: ['not retained'], internal: 'not retained' }] }));
  assert.deepEqual(publicPracticalReadEvidence(path, 200, bytes), { kind: 'weather-history', yearsCount: 1 });
  assert.deepEqual(publicPracticalReadEvidence(path, 200, Buffer.from('{"data":{"years":[]}}')), { kind: 'weather-history', yearsCount: 0 });
  assert.equal(publicPracticalReadEvidence('/api/admin/parks/example/weather', 200, bytes), undefined);
  assert.equal(publicPracticalReadEvidence(path, 503, bytes), undefined);
  assert.throws(() => publicPracticalReadEvidence(path, 200, Buffer.from('{}')), /Invalid bounded/);
  assert.throws(() => publicPracticalReadEvidence(path, 200, Buffer.alloc(2 * 1024 * 1024 + 1)), /response bound/);
});

test('practical browser recipe exercises actual dates, month controls, lazy history and exact back entry without mutations', () => {
  const recipe = read('tools/design-v6/exercise-public-practical.mjs');
  for (const token of ['aria-pressed', 'initialDayDetail', 'changedMonth', 'historyYears', 'cachedReopen', 'history.go', 'navigation.currentEntry.key']) assert.ok(recipe.includes(token));
  assert.doesNotMatch(recipe, /fetch\(|localStorage\.setItem|\.submit\(|\.emit\(|new Date\(/);
  assert.ok(recipe.includes('empty-state acceptance remains separate'));
  const capture = read('tools/design-v6/capture-public-ssr.mjs');
  assert.ok(capture.includes('await exercisePublicPractical('));
  assert.ok(capture.includes('Hydrated practical page overflows'));
});

test('the today badge uses the per-theme gold foreground rather than a fixed dark label', () => {
  assert.match(practicalStyles, /\.park-opening-hours-cell__today\s*\{[^}]*color:\s*var\(--app-on-gold\)/);
});

test('calendar labels reflow inside fixed grid tracks without shrinking the today label on mobile', () => {
  const styles = read('src/app/features/public/parks/pages/park-opening-hours-page.component.scss');
  assert.match(styles, /\.park-opening-hours-cell\s*\{[^}]*grid-template-columns:\s*minmax\(0, 1fr\)/);
  assert.match(styles, /\.park-opening-hours-cell__today\s*\{[^}]*overflow-wrap:\s*anywhere/);
  assert.doesNotMatch(styles, /\.park-opening-hours-cell__today\s*\{[^}]*(?:ellipsis|nowrap|overflow:\s*hidden|font-size:\s*0\.[45])/);
  assert.doesNotMatch(styles, /border-radius:\s*[67]px/);
  assert.match(styles, /\.park-opening-hours-month__table-wrap\s*\{[^}]*overflow-x:\s*auto/);
  assert.match(styles, /\.park-opening-hours-month__table\s*\{[^}]*min-width:\s*26rem/);
  assert.match(openingTemplate, /class="park-opening-hours-month__table-wrap" role="region" tabindex="0" \[attr\.aria-label\]="group\.label"/);
});

test('calendar geometry rejects internal overlap even when document width passes', () => {
  const bounds = { x: 20, y: 100, right: 72, bottom: 200 };
  const cell = { bounds, button: { ...bounds }, labels: [{ bounds: { x: 24, y: 120, right: 56, bottom: 160 }, clientWidth: 32, scrollWidth: 32, clientHeight: 40, scrollHeight: 40, today: true, textOverflow: 'clip', whiteSpace: 'normal' }] };
  assert.deepEqual(openingCalendarLayoutIssues([cell, cell]), []);
  const overlap = structuredClone(cell);
  overlap.labels[0].bounds.right = 75;
  assert.ok(openingCalendarLayoutIssues([cell, overlap]).some(issue => issue.includes('outside button')));
  const clipped = structuredClone(cell);
  clipped.labels[0].scrollWidth = 52;
  assert.ok(openingCalendarLayoutIssues([cell, clipped]).some(issue => issue.includes('clipped label')));
  const ellipsis = structuredClone(cell);
  ellipsis.labels[0].textOverflow = 'ellipsis';
  assert.ok(openingCalendarLayoutIssues([cell, ellipsis]).some(issue => issue.includes('cannot reflow')));
  const compressed = structuredClone(cell);
  compressed.button.right = 59;
  assert.ok(openingCalendarLayoutIssues([cell, compressed]).some(issue => issue.includes('compressed day target')));
  assert.ok(openingCalendarLayoutIssues([]).length > 0);
});

test('practical pages share the full editorial shell without nested responsive gutters', () => {
  for (const name of ['weather', 'opening-hours', 'pricing']) {
    const styles = read(`src/app/features/public/parks/pages/park-${name}-page.component.scss`);
    assert.ok(styles.startsWith('@use "../../../../../styles/mixins" as m;'));
    assert.ok(styles.includes('font-size: clamp(2.125rem, 5vw, 3.5625rem)'));
    assert.match(styles, new RegExp(`\\.park-${name}-page\\s*\\{[^}]*@include m\\.public-editorial-page`));
    assert.match(styles, new RegExp(`\\.park-${name}-hero\\s*\\{[^}]*@include m\\.public-editorial-header`));
    assert.doesNotMatch(styles, new RegExp(`\\.park-${name}-page\\s*\\{[^}]*[;{]\\s*width:`));
  }
});

test('pricing breadcrumbs, kickers and audience chips use semantic text accents', () => {
  for (const selector of ['park-pricing-breadcrumb a', 'park-pricing-hero__kicker', 'park-price-card__audience', 'park-price-card__buy', 'park-weather-board__legend-item--max']) {
    assert.match(practicalStyles, new RegExp(`\\.${selector}\\s*\\{[^}]*color:\\s*var\\(--app-primary-strong\\)`));
  }
  assert.match(practicalStyles, /\.park-weather-board__legend-item--max::before\s*\{[^}]*background:\s*var\(--weather-max\)/);
});

test('opening hours preserve month navigation, selected day, admissions and timezone', () => {
  for (const contract of [
    'previousMonthKey(data.calendar)',
    'nextMonthKey(data.calendar)',
    'monthLoading()',
    'monthLoadFailed()',
    'isSelectedCell(cell)',
    'selectedDayDetails(data.calendar)',
    'range.lastAdmissionAt',
    'data.calendar.timeZoneId',
    'data.calendar.sourceUrl',
  ]) {
    assert.ok(openingTemplate.includes(contract), `${contract} must remain in opening hours`);
  }
});

test('weather preserves real forecast series, rain, wind, history and unavailable states', () => {
  for (const contract of [
    'unavailablePark()',
    "temperatureLinePoints(data.weather.days, 'max')",
    "temperatureLinePoints(data.weather.days, 'min')",
    'precipitationProbabilityLabel(day)',
    'precipitationSumLabel(day)',
    'windSpeedLabel(day)',
    '(toggle)="onHistoricalComparisonToggle($event, data.park.id)"',
    "historicalComparisonState().kind === 'error'",
    'history.years',
  ]) {
    assert.ok(weatherTemplate.includes(contract), `${contract} must remain in weather`);
  }
});

test('pricing preserves every offer family, channel, condition, source and freshness', () => {
  for (const contract of [
    'pricing.admissionOffers',
    'pricing.creditOffers',
    'pricing.annualPasses',
    'pricing.parkingOffers',
    'offer.onlinePrice',
    'offer.gatePrice',
    'offer.conditions',
    'offer.purchaseUrl',
    'historySeries()',
    'pricing.lastVerifiedAtUtc',
    'pricing.sourceUrl',
    'pricing.purchaseUrl',
  ]) {
    assert.ok(pricingTemplate.includes(contract), `${contract} must remain in pricing`);
  }
});

test('nearby and live panels preserve distance, refresh, offline, filters, forecasts and evidence', () => {
  assert.ok(nearbyTemplate.includes('<app-ui-distance-panel'));
  assert.ok(nearbyTemplate.includes('[sourceLabel]="sourceName"'));

  for (const contract of [
    'state.isOnline',
    'state.isRefreshing',
    '(click)="refresh()"',
    'state.refreshFailed',
    'target.queues.length',
    '(click)="selectFilter(filter)"',
    '[routerLink]="route"',
    'freshnessReference(target)',
    'attributionSources(target)',
    '<app-public-live-forecast-card',
    '<app-public-live-history-panel',
  ]) {
    assert.ok(liveTemplate.includes(contract), `${contract} must remain in live operations`);
  }

  assert.ok(liveForecastTemplate.includes('forecast.expectedWaitMinutes'));
  assert.ok(liveForecastTemplate.includes('forecast.source.attributionUrl'));
  assert.ok(liveHistoryTemplate.includes('history.coveragePercent'));
  assert.ok(liveHistoryTemplate.includes('history.source.attributionUrl'));
});

test('practical data surfaces use flat Playground panels without decorative gradients', () => {
  assert.match(practicalStyles, /\.park-weather-card[\s\S]*?background:\s*var\(--app-surface/);
  assert.match(practicalStyles, /\.park-opening-hours-card[\s\S]*?background:\s*var\(--app-surface/);
  assert.match(practicalStyles, /\.park-pricing-hero\s*\{[^}]*@include m\.public-editorial-header/);
  assert.match(read('src/styles/_mixins.scss'), /@mixin public-editorial-header\s*\{[^}]*border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(practicalStyles, /\.live-panel[\s\S]*?border-top:\s*4px solid var\(--app-accent-cyan\)/);
  assert.doesNotMatch(practicalStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(practicalStyles, /mask-image\s*:/);
});
