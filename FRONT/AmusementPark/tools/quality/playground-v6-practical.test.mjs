import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

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

test('the today badge uses the per-theme gold foreground rather than a fixed dark label', () => {
  assert.match(practicalStyles, /\.park-opening-hours-cell__today\s*\{[^}]*color:\s*var\(--app-on-gold\)/);
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
  assert.match(practicalStyles, /\.park-pricing-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(practicalStyles, /\.live-panel[\s\S]*?border-top:\s*4px solid var\(--app-accent-cyan\)/);
  assert.doesNotMatch(practicalStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(practicalStyles, /mask-image\s*:/);
});
