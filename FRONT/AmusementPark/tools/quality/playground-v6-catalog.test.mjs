import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');
const page = read('src/app/features/public/parks/pages/park-list-page.component.html');
const view = read('src/app/features/public/parks/ui/park-list-view.component.html');
const map = read('src/app/features/public/parks/ui/park-list-map.component.html');
const styles = read('src/styles/_public-experience.scss');
const mapStyles = read('src/app/features/public/parks/ui/park-list-map.component.scss');

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
