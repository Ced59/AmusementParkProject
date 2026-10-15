import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { publicDirectoryReadEvidence, publicRouteEvidenceName } from '../design-v6/public-directory-evidence.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');
const page = read('src/app/features/public/parks/pages/park-list-page.component.html');
const view = read('src/app/features/public/parks/ui/park-list-view.component.html');
const map = read('src/app/features/public/parks/ui/park-list-map.component.html');
const styles = read('src/styles/_public-experience.scss');
const mapStyles = read('src/app/features/public/parks/ui/park-list-map.component.scss');
const browserReview = read('tools/design-v6/capture-public-ssr.mjs');

test('directory evidence contains only public names and pagination, not full DTOs', () => {
  const payload = { data: [{ name: ' Parc été ', isVisible: true, id: 'public-id', internalNote: 'not evidence' }],
    pagination: { currentPage: 2, itemsPerPage: 9, totalItems: 25, totalPages: 3 } };
  assert.deepEqual(publicDirectoryReadEvidence('/api/parks?page=2&size=9&visibleOnly=true', 200, Buffer.from(JSON.stringify(payload))), {
    names: ['Parc été'], pagination: { currentPage: 2, itemsPerPage: 9, totalItems: 25 },
  });
});

test('directory evidence ignores private lists, unrelated endpoints and unsuccessful reads', () => {
  for (const [url, status] of [['/api/parks', 200], ['/api/admin/parks?visibleOnly=true', 200],
    ['/api/parks/map-visible?visibleOnly=true', 200], ['/api/parks?visibleOnly=true', 503]]) {
    assert.equal(publicDirectoryReadEvidence(url, status, Buffer.from('not JSON')), undefined);
  }
});

test('directory evidence rejects hidden entities, broken pagination and unbounded fixtures', () => {
  const valid = { data: [{ name: 'Parc', isVisible: true }], pagination: { currentPage: 1, itemsPerPage: 9, totalItems: 1 } };
  for (const payload of [
    { ...valid, data: [{ name: 'Hidden', isVisible: false }] },
    { ...valid, data: [{ name: ' ', isVisible: true }] },
    { ...valid, data: Array(101).fill(valid.data[0]) },
    { ...valid, pagination: { currentPage: 0, itemsPerPage: 9, totalItems: 1 } },
    { ...valid, pagination: { currentPage: 1, itemsPerPage: 9, totalItems: 0 } },
    { ...valid, pagination: null },
  ]) assert.throws(() => publicDirectoryReadEvidence('/api/parks?visibleOnly=true', 200, Buffer.from(JSON.stringify(payload))), /Invalid or non-public/);
  assert.deepEqual(publicDirectoryReadEvidence('/api/parks?visibleOnly=true', 200, Buffer.from(JSON.stringify({ ...valid, data: [], pagination: { ...valid.pagination, totalItems: 0 } }))), {
    names: [], pagination: { currentPage: 1, itemsPerPage: 9, totalItems: 0 },
  });
});

test('public route evidence names support deep links and query strings on Windows', () => {
  assert.equal(publicRouteEvidenceName('/'), 'language-entry');
  assert.equal(publicRouteEvidenceName('/fr/parks'), 'fr_parks');
  assert.equal(publicRouteEvidenceName('/fr/parks?page=2'), 'fr_parks_page_2');
  assert.doesNotMatch(publicRouteEvidenceName('/fr/parks?page=2&name=%22é%22'), /[<>:"/\\|?*]/);
  assert.notEqual(publicRouteEvidenceName('/fr/parks'), publicRouteEvidenceName('/fr/parks?page=2'));
});

test('the hydrated catalogue recipe compares real API pages and preserves map selection versus detail navigation', () => {
  for (const contract of [
    "'/fr/parks?page=2'", 'publicDirectoryReadEvidence(target, response.status, bytes)',
    'catalogue page two retains its real URL and canonical', 'catalogue browser back restores the first-page URL',
    'catalogue audience filter', 'catalogue audience reset', 'for (const rows of [6, 18, 36, 9])',
    'API names and pagination match the rendered cards',
    'catalogue card body selects the real map park without opening a detail',
    'catalogue clear selection restores the real result list', 'catalogue detail action opens its full real park route',
  ]) assert.ok(browserReview.includes(contract), `${contract} must remain in the read-only browser recipe`);
  assert.ok(browserReview.includes('if (new Set(paths.map(publicRouteEvidenceName)).size !== paths.length)'));
});

test('the park directory page preserves every filter, selection and pagination output', () => {
  for (const binding of [
    '[selectedRegion]="selectedRegion"',
    '[selectedStatus]="selectedStatus"',
    '[selectedAudienceClassificationFilter]="selectedAudienceClassificationFilter"',
    '[discoveryScope]="discoveryScope"',
    '(searchInputChanged)="onSearchInput($event)"',
    '(searchSubmitted)="onSearchSubmit()"',
    '(mapParkSelected)="onMapParkSelected($event)"',
    '(regionFilterChanged)="onRegionFilterChanged($event)"',
    '(statusFilterChanged)="onStatusFilterChanged($event)"',
    '(audienceClassificationFilterChanged)="onAudienceClassificationFilterChanged($event)"',
    '(discoveryScopeChanged)="onDiscoveryScopeChanged($event)"',
    '(pageChanged)="onPageChange($event)"',
  ]) {
    assert.ok(page.includes(binding), `${binding} must remain wired`);
  }
});

test('the directory view keeps URL pagination, real cards, mixed results and the synchronized map', () => {
  for (const contract of [
    '<app-ui-search-panel',
    '<app-park-list-map',
    '[pageHref]="pageHref()"',
    '[rowsPerPageOptions]="[6, 9, 18, 36]"',
    '<app-ui-park-card',
    '<app-ui-search-result-card',
    '(click)="onResultCardClick($event, park)"',
    '(keydown)="onResultCardKeydown($event, park)"',
    '<app-public-share-panel',
  ]) {
    assert.ok(view.includes(contract), `${contract} must remain in the catalogue`);
  }

  assert.match(view, /data-pagination-scroll-target/);
  assert.match(view, /parks-pagination-trail/);
});

test('the map remains a real lazy-compatible Leaflet interaction surface', () => {
  for (const contract of [
    '<app-leaflet-map',
    '[markers]="mapMarkers()"',
    '[selectedMarkerId]="selectedParkId()"',
    '[fitBounds]="true"',
    '(markerClick)="onMarkerClick($event)"',
    '(click)="onRegionFilterClick(filter.value)"',
  ]) {
    assert.ok(map.includes(contract), `${contract} must remain in the map`);
  }
});

test('catalogue surfaces follow the flat Playground visual language', () => {
  assert.match(styles, /\.parks-hero[\s\S]*?border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(styles, /\.parks-results[\s\S]*?border-top:\s*3px solid var\(--app-heading\)/);
  assert.match(styles, /\.public-filter-panel[\s\S]*?background:\s*var\(--app-surface-2\)/);
  assert.match(mapStyles, /border-radius:\s*var\(--app-radius-sm\)/);
  assert.doesNotMatch(`${styles}\n${mapStyles}`, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(`${styles}\n${mapStyles}`, /backdrop-filter\s*:/);
});
