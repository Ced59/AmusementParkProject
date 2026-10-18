import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { readHomeEvidenceFixtures, summarizeHomeResources, compareHomeNetworkSamples, homeNetworkAcceptance, homeBrandImageRequestSaving } from '../design-v6/home-network-evidence.mjs';

test('small CI JSON reports precede the complete image archive without replacing evidence or gates', () => {
  const workflow = readFileSync(resolve(import.meta.dirname, '../../../../.github/workflows/production.yml'), 'utf8');
  const compact = workflow.match(/- name: Upload compact V6 diagnostic reports[\s\S]*?(?=\n      - name: Upload initial V6)/)?.[0];
  assert.ok(compact);
  assert.ok(compact.includes("if: always() && (steps.v6-preflight.outcome != 'skipped' || steps.v6-review.outcome != 'skipped')"));
  assert.ok(compact.includes("github.event_name == 'pull_request' && github.head_ref == 'feat/ui-v6-playground-redesign'"));
  assert.ok(compact.includes('name: v6-diagnostic-reports'));
  assert.ok(compact.includes('path: ${{ runner.temp }}/v6-public-ssr/**/*.json'));
  assert.ok(compact.includes('if-no-files-found: error'));
  assert.doesNotMatch(compact, /continue-on-error/);
  const complete = workflow.match(/- name: Upload initial V6 public SSR evidence[\s\S]*?(?=\n      - name: Upload frontend coverage)/)?.[0];
  assert.ok(complete?.includes('name: v6-public-ssr-initial'));
  assert.ok(complete?.includes('path: ${{ runner.temp }}/v6-public-ssr'));
  const capture = readFileSync(resolve(import.meta.dirname, '../design-v6/capture-public-ssr.mjs'), 'utf8');
  assert.ok(capture.includes("throw new Error('P04.6: initial requests exceed the exact owner-approved focus exception; inspect acceptance.failures.')"));
});

test('owner exception accepts only the measured two focus scripts and never other resource types or a new baseline', () => {
  const approval = JSON.parse(readFileSync(resolve(import.meta.dirname, '../../../../docs/design-v6/V6-PERFORMANCE-EXCEPTION.json'), 'utf8'));
  const before = { requests: 105, byType: { Script: { requests: 84, decodedBytes: 936317 }, XHR: { requests: 2 }, Image: { requests: 12 } } };
  const after = { requests: 107, byType: { Script: { requests: 86, decodedBytes: 936317 + 4565 }, XHR: { requests: 2 }, Image: { requests: 12 } } };
  const report = { baseSha: approval.baselineSha, comparisons: [{ width: 390, sampleNumber: 1, before, after, additionalRequests: 2 }] };
  assert.equal(homeNetworkAcceptance(report).status, 'FAIL');
  assert.equal(homeNetworkAcceptance(report, approval).status, 'PASS_WITH_OWNER_EXCEPTION');
  assert.equal(homeNetworkAcceptance({ ...report, baseSha: 'another-master' }, approval).status, 'FAIL');
  for (const changed of [
    { ...after, byType: { ...after.byType, Script: { requests: 86, decodedBytes: 936317 + 4566 } } },
    { ...after, byType: { ...after.byType, XHR: { requests: 3 } } },
    { ...after, byType: { ...after.byType, Image: { requests: 13 } } },
    { ...after, byType: { ...after.byType, Font: { requests: 1 } } },
    { ...after, requests: 108 },
  ]) assert.equal(homeNetworkAcceptance({ ...report, comparisons: [{ ...report.comparisons[0], after: changed }] }, approval).status, 'FAIL');
  assert.equal(homeNetworkAcceptance({ ...report, comparisons: [{ ...report.comparisons[0], after: before, additionalRequests: 0 }] }).status, 'PASS');
  assert.equal(homeNetworkAcceptance(report, { ...approval, maximumAdditionalScriptRequests: 3 }).status, 'FAIL');
  assert.equal(homeNetworkAcceptance(report, { ...approval, maximumScriptDecodedBytesDelta: 4566 }).status, 'FAIL');
  assert.equal(homeNetworkAcceptance(report, { ...approval, approval: 'general approval' }).status, 'FAIL');
  assert.equal(homeNetworkAcceptance({ ...report, comparisons: [] }, approval).status, 'FAIL');
});

test('the earlier approval stays capped at 3302 bytes; the extension cannot be inferred from it', () => {
  const current = JSON.parse(readFileSync(resolve(import.meta.dirname, '../../../../docs/design-v6/V6-PERFORMANCE-EXCEPTION.json'), 'utf8'));
  const legacy = { ...current, ...current.previousApproval };
  const before = { requests: 10, byType: { Script: { requests: 10, decodedBytes: 10000 } } };
  for (const [delta, legacyStatus] of [[3302, 'PASS_WITH_OWNER_EXCEPTION'], [3303, 'FAIL'], [3827, 'FAIL'], [4565, 'FAIL']]) {
    const after = { requests: 12, byType: { Script: { requests: 12, decodedBytes: 10000 + delta } } };
    const report = { baseSha: current.baselineSha, comparisons: [{ width: 390, sampleNumber: 1, before, after, additionalRequests: 2 }] };
    assert.equal(homeNetworkAcceptance(report, legacy).status, legacyStatus);
    assert.equal(homeNetworkAcceptance(report, current).status, 'PASS_WITH_OWNER_EXCEPTION');
  }
});

test('removing only the approved separate brand pictogram is a proven saving, not an additional resource or a missing hero allowance', () => {
  const approval = JSON.parse(readFileSync(resolve(import.meta.dirname, '../../../../docs/design-v6/V6-PERFORMANCE-EXCEPTION.json'), 'utf8'));
  const brand = { type: 'Image', path: '/assets/general-icon/logo-amusementpark-ui-36.webp' };
  const otherImages = Array.from({ length: 11 }, (_, index) => ({ type: 'Image', path: `/test-image-${index}.webp` }));
  const before = { requests: 105, byType: { Script: { requests: 84, decodedBytes: 936317 }, XHR: { requests: 2 }, Image: { requests: 12 } } };
  const after = { requests: 106, byType: { Script: { requests: 86, decodedBytes: 939597 }, XHR: { requests: 2 }, Image: { requests: 11 } } };
  const beforeSample = { summary: before, resources: [brand, ...otherImages] };
  const afterSample = { summary: after, resources: otherImages };
  const saving = homeBrandImageRequestSaving(beforeSample, afterSample);
  assert.equal(saving.requestsSaved, 1);
  const comparison = { width: 390, sampleNumber: 1, before, after, additionalRequests: 1, brandImageSaving: saving };
  const report = { baseSha: approval.baselineSha, comparisons: [comparison] };
  assert.equal(homeNetworkAcceptance(report, approval).status, 'PASS_WITH_OWNER_EXCEPTION');
  assert.equal(homeNetworkAcceptance({ ...report, comparisons: [{ ...comparison, brandImageSaving: null }] }, approval).status, 'FAIL');
  assert.equal(homeBrandImageRequestSaving(beforeSample, { ...afterSample, resources: [brand, ...otherImages.slice(1)] }), null);
  assert.equal(homeBrandImageRequestSaving(beforeSample, { ...afterSample, resources: otherImages.slice(1) }), null);
  assert.equal(homeBrandImageRequestSaving(beforeSample, { ...afterSample, resources: [...otherImages.slice(1), { type: 'Image', path: '/different-hero.webp' }] }), null);
  assert.equal(homeBrandImageRequestSaving(beforeSample, { ...afterSample, resources: otherImages.map(resource => ({ type: resource.type })) }), null);
  assert.equal(homeBrandImageRequestSaving(beforeSample, { ...afterSample, summary: { byType: { Image: { requests: 10 } } } }), null);
  const apiRegression = { ...after, requests: 107, byType: { ...after.byType, XHR: { requests: 3 } } };
  assert.equal(homeNetworkAcceptance({ ...report, comparisons: [{ ...comparison, after: apiRegression, additionalRequests: 2 }] }, approval).status, 'FAIL');
});

test('the hero logo advertises its actual 72-percent / 18-rem cap instead of the entire card', () => {
  const read = path => readFileSync(resolve(import.meta.dirname, '../..', path), 'utf8');
  const template = read('src/app/features/public/home/pages/home-view.component.html');
  const cards = read('src/styles/_cards.scss');
  assert.match(cards, /\.home-hero-card__image\s*\{[^}]*width:\s*min\(72%, 18rem\)/);
  assert.ok(template.includes('sizes="(max-width: 428px) calc(72vw - 20.16px), 18rem"'));
  assert.doesNotMatch(template, /sizes="\(max-width: 960px\) 78vw, 36vw"/);
  for (const width of [320, 360, 390, 414]) {
    const advertised = width * 0.72 - 20.16;
    assert.ok(Math.abs(advertised - (width - 28) * 0.72) < 1);
  }
  assert.ok(template.includes('[responsiveWidths]="[320, 480, 640, 800]"'), 'retina candidates remain available');
});

test('home comparison fixtures accept only bounded successful public home responses', () => {
  const record = { key: '/api/parks/random-visible?limit=4', status: 200, contentType: 'application/json; charset=utf-8', bodyBase64: Buffer.from('[{"name":"Parc été"}]').toString('base64') };
  const fixtures = readHomeEvidenceFixtures([record]);
  assert.equal(fixtures.get(record.key).bytes.toString('utf8'), '[{"name":"Parc été"}]');
  for (const invalid of [
    { ...record, key: '/api/admin/parks' }, { ...record, key: '/api/profile' },
    { ...record, key: 'https://untrusted.invalid/api/parks/random-visible' },
    { ...record, status: 503 }, { ...record, contentType: 'text/html' },
    { ...record, bodyBase64: 'a'.repeat(2800001) },
  ]) assert.throws(() => readHomeEvidenceFixtures([invalid]), /Unexpected home fixture/);
  assert.throws(() => readHomeEvidenceFixtures([record, record]), /Unexpected home fixture/);
  assert.throws(() => readHomeEvidenceFixtures(Array(17).fill(record)), /Invalid bounded/);
});

test('network evidence counts issued requests separately from decoded body bytes', () => {
  assert.deepEqual(summarizeHomeResources([
    { type: 'Image', decodedBytes: 30 }, { type: 'Image', decodedBytes: 0 },
    { type: 'Script', decodedBytes: 100 },
  ]), { requests: 3, decodedBytes: 130, byType: { Image: { requests: 2, decodedBytes: 30 }, Script: { requests: 1, decodedBytes: 100 } } });
});

test('home comparison builds the exact master frontend and preserves distinct SSR and refreshed client data', () => {
  const read = path => readFileSync(resolve(import.meta.dirname, '../..', path), 'utf8');
  const baseline = read('tools/design-v6/render-home-baseline.mjs');
  const capture = read('tools/design-v6/capture-public-ssr.mjs');
  const workflow = readFileSync(resolve(import.meta.dirname, '../../../../.github/workflows/production.yml'), 'utf8');
  assert.ok(baseline.includes("process.env.CI !== 'true' || process.platform !== 'linux'"));
  assert.ok(baseline.includes("request.method !== 'GET'"));
  assert.ok(baseline.includes('clientRecords.push({ key: record.key'));
  assert.ok(baseline.includes('ssr: [...records.values()], client: clientRecords'));
  assert.ok(workflow.includes('archive "${V6_BASELINE_SHA}" FRONT/AmusementPark'));
  assert.ok(workflow.includes('cmp package-lock.json'));
  assert.ok(capture.includes('for (const width of [390, 1366])'));
  assert.ok(capture.includes('compareHomeNetworkSamples(report.homeNetworkSamples)'));
  assert.ok(capture.includes("sampleNumber % 2 ? ['master', 'v6'] : ['v6', 'master']"));
  assert.ok(capture.includes('homeNetworkAcceptance(report.homeNetworkComparison, ownerApproval)'));
  assert.ok(capture.includes("report.homeNetworkComparison.acceptance.status === 'FAIL'"));
  assert.ok(capture.includes('P04.6: initial requests exceed the exact owner-approved focus exception'));
  assert.ok(capture.includes('decoded bytes, not wire compression'));
  assert.ok(capture.includes('LCP/CLS proxies, not field CWV or INP'));
});

function pairedHomeSamples() {
  return [1, 2, 3].flatMap(sampleNumber => ['master', 'v6'].map(label => ({
    width: 390, sampleNumber, label,
    summary: { requests: 100, decodedBytes: label === 'master' ? 2000 : 1900 },
    vitals: { heroNames: ['Public park'], lcpMs: sampleNumber * (label === 'master' ? 100 : 90), cls: 0, domCompleteMs: sampleNumber * 200 },
  })));
}

test('repeated home evidence keeps every paired sample and reports median and range, not a field metric', () => {
  const result = compareHomeNetworkSamples(pairedHomeSamples(), [390]);
  assert.equal(result.comparisons.length, 3);
  assert.deepEqual(result.aggregates[0].before.lcpMs, { min: 100, median: 200, max: 300 });
  assert.deepEqual(result.aggregates[0].after.lcpMs, { min: 90, median: 180, max: 270 });
  assert.deepEqual(result.aggregates[0].additionalRequests, { min: 0, median: 0, max: 0 });
  assert.deepEqual(result.aggregates[0].decodedBytesDelta, { min: -100, median: -100, max: -100 });
});

test('home comparison rejects missing, duplicate, mismatched and incomplete samples', () => {
  const samples = pairedHomeSamples();
  assert.throws(() => compareHomeNetworkSamples(samples.slice(1), [390]), /Missing or duplicate/);
  assert.throws(() => compareHomeNetworkSamples([...samples, samples[0]], [390]), /Missing or duplicate/);
  const mismatched = structuredClone(samples);
  mismatched[1].vitals.heroNames = ['Different population'];
  assert.throws(() => compareHomeNetworkSamples(mismatched, [390]), /populations differ/);
  const incomplete = structuredClone(samples);
  incomplete[1].vitals.lcpMs = null;
  assert.throws(() => compareHomeNetworkSamples(incomplete, [390]), /Incomplete home timing/);
});

test('one regressing request sample cannot be hidden by a non-positive median', () => {
  const samples = pairedHomeSamples();
  samples[1].summary.requests = 101;
  const result = compareHomeNetworkSamples(samples, [390]);
  assert.equal(result.aggregates[0].additionalRequests.median, 0);
  assert.equal(result.aggregates[0].additionalRequests.max, 1);
  assert.equal(result.comparisons.some(pair => pair.additionalRequests > 0), true);
});

const projectRoot = resolve(import.meta.dirname, '../..');
const template = readFileSync(
  resolve(projectRoot, 'src/app/features/public/home/pages/home-view.component.html'),
  'utf8',
);
const styles = readFileSync(
  resolve(projectRoot, 'src/app/features/public/home/pages/home.component.scss'),
  'utf8',
);
const accessibilityStyles = readFileSync(resolve(projectRoot, 'src/styles/_accessibility-polish.scss'), 'utf8');
const formStyles = readFileSync(resolve(projectRoot, 'src/styles/_forms.scss'), 'utf8');

test('the home search bar uses the supplied desktop and mobile control composition without changing other search panels', () => {
  assert.match(formStyles, /\.home-command-center \.app-search-panel\s*\{[^}]*border:\s*0;[^}]*background:\s*transparent/);
  assert.match(formStyles, /\.home-command-center \.app-search-panel__grid\s*\{[^}]*grid-template-columns:\s*minmax\(0, 1fr\) minmax\(190px, 0\.42fr\) auto/);
  assert.match(formStyles, /@media \(max-width:\s*680px\)\s*\{[\s\S]*?\.home-command-center \.app-search-panel__grid\s*\{[^}]*grid-template-columns:\s*minmax\(0, 1fr\) auto/);
  assert.match(formStyles, /\.home-command-center \.app-search-panel__clear\s*\{[^}]*grid-column:\s*1 \/ -1;[^}]*min-height:\s*44px/);
});

test('hero CTA typography follows the V6 reference while retaining 44px targets and both real destinations', () => {
  assert.match(styles, /\.home-hero__actions \.app-button\s*\{[^}]*min-height:\s*44px;[^}]*padding:\s*10px 14px;[^}]*border-width:\s*2px;[^}]*font-size:\s*0\.75rem;[^}]*font-weight:\s*850;[^}]*letter-spacing:\s*0\.015em;[^}]*text-transform:\s*uppercase/);
  assert.match(styles, /\.home-hero__actions \.btn\s*\{[^}]*font-size:\s*0\.6875rem/);
  assert.ok(template.includes("[routerLink]=\"['/', currentLang(), 'parks']\""));
  assert.ok(template.includes("[routerLink]=\"['/', currentLang(), 'about']\""));
});

test('the home hero keeps API statistics, two CTAs and four dynamic park slots', () => {
  for (const count of ['parksCount', 'attractionsCount', 'countriesCount']) {
    assert.match(template, new RegExp(`formatStatValue\\(homeStats\\(\\)\\?\\.${count}\\)`));
  }

  assert.match(template, /home\.ctaBrowseParks/);
  assert.match(template, /home\.ctaLearnMore/);
  assert.match(template, /@for \(park of heroFeaturedParks\(\)/);
  assert.match(template, /getHeroCardClass\(index\)/);
  assert.match(template, /\[imageId\]="park\.logoImageId"/);
  assert.match(template, /\[loading\]="index === 0 \? 'eager' : 'lazy'"/);
  assert.match(template, /\[fetchPriority\]="index === 0 \? 'high' : 'low'"/);
  assert.doesNotMatch(template, /Le Fleury|Pendolino|38 lieux/i);
});

test('the complete production search contract remains in the home template', () => {
  for (const contract of [
    '[searchTerm]="searchTerm()"',
    '[filters]="searchFilters()"',
    '(searchTermChange)="onSearchInput($event)"',
    '(searchKeyDown)="onSearchKeyDown($event)"',
    '(searchClicked)="onSearchSubmit()"',
    '(filterChanged)="onFilterChanged($event)"',
    '(clearClicked)="onClearSearch()"',
    '(click)="onAutocompleteSelected(autocompleteSuggestion)"',
    '(click)="onSuggestionSelected(suggestionCard.title)"',
    '[rowsPerPageOptions]="[5, 10, 20]"',
    '(pageChanged)="onPageChange($event)"',
  ]) {
    assert.ok(template.includes(contract), `${contract} must remain wired`);
  }

  assert.match(template, /<app-ui-search-result-card/);
  assert.match(template, /<app-pagination/);
});

test('featured, latest and sharing blocks keep their deferred production composition', () => {
  assert.match(template, /@for \(park of featuredParks\(\)/);
  assert.match(template, /@defer \(on viewport; hydrate on viewport; prefetch on idle\)/);
  assert.match(template, /<app-home-latest-content>/);
  assert.match(template, /<app-public-share-panel/);
});

test('the Playground home uses the editorial split without decorative effects', () => {
  assert.match(styles, /grid-template-columns:\s*minmax\(0, 1\.08fr\) minmax\(0, 0\.92fr\)/);
  assert.match(styles, /\.home-hero__spotlight[\s\S]*?border-left:\s*3px solid var\(--app-heading\)/);
  assert.match(styles, /\.home-hero-card--1[\s\S]*?border-top:\s*5px solid var\(--c-orange\)/);
  assert.match(styles, /@media \(max-width:\s*960px\)[\s\S]*?grid-template-columns:\s*1fr/);
  assert.match(styles, /@media \(max-width:\s*680px\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(styles, /animation\s*:/);
  assert.doesNotMatch(styles, /backdrop-filter\s*:/);
});

test('the home title and primary image use the supplied V6 dimensions rather than the previous oversized envelope', () => {
  assert.match(styles, /font-size:\s*clamp\(39px, 5\.3vw, 74px\)/);
  assert.match(styles, /line-height:\s*1\.065/);
  assert.match(styles, /gap:\s*clamp\(18px, 3vw, 38px\)/);
  assert.match(styles, /\.home-hero-card--1 \.home-hero-card__media\s*\{[^}]*height:\s*205px/);
  assert.match(styles, /font-size:\s*clamp\(38px, 9vw, 53px\)/);
  assert.match(styles, /\.home-hero__title-line--outline\s*\{[^}]*color:\s*var\(--app-heading\);[^}]*-webkit-text-stroke:\s*0/);
  assert.doesNotMatch(accessibilityStyles, /\.home-hero__title-line--outline/,
    'a higher-specificity light theme override must not restore the V5 orange outline on mobile');
});
