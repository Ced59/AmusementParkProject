import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

test('the authorized Leaflet teardown waits for the native zoom event without changing live interactions', () => {
  const component = readFileSync(resolve(import.meta.dirname, '../../src/app/shared/components/leaflet-map/leaflet-map.component.ts'), 'utf8');
  const teardown = component.slice(component.indexOf('  ngOnDestroy(): void {'), component.indexOf('  public refreshMapSize('));
  const disposer = readFileSync(resolve(import.meta.dirname, '../../src/app/shared/utils/maps/leaflet-map-disposer.ts'), 'utf8');
  assert.ok(teardown.includes('this.disposeMap();'));
  assert.ok(disposer.includes("map.on('zoomend'"));
  assert.ok(disposer.includes('queueMicrotask('));
  assert.doesNotMatch(teardown, /setTimeout|_onZoomTransitionEnd|_animatingZoom|animate:\s*false/);
  assert.ok(teardown.indexOf('this.map = null;') < teardown.indexOf('this.disposeMap();'));
});

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const pageTemplate = read('src/app/features/public/parks/pages/park-map-page.component.html');
const viewTemplate = read('src/app/features/public/parks/ui/park-map-view.component.html');
const viewStyles = read('src/app/features/public/parks/ui/park-map-view.component.scss');
const interactiveTemplate = read('src/app/features/public/parks/ui/park-items-map-section.component.html');
const interactiveStyles = read('src/app/features/public/parks/ui/park-items-map-section.component.scss');
const officialTemplate = read('src/app/features/public/parks/ui/park-official-maps-section.component.html');
const officialStyles = read('src/app/features/public/parks/ui/park-official-maps-section.component.scss');
const listTemplate = read('src/app/features/public/parks/ui/park-list-map.component.html');
const leafletStyles = read('src/app/shared/components/leaflet-map/leaflet-map.component.scss');

test('the park map page preserves interactive and official modes with their filters', () => {
  for (const contract of [
    '[map]="map()"',
    '[officialMaps]="visibleOfficialMaps()"',
    '[officialMapYears]="officialMapYears()"',
    '[selectedOfficialMapYear]="selectedOfficialMapYear()"',
    '[selectedClosedFilter]="selectedClosedFilter()"',
    '(closedFilterChanged)="onClosedFilterChanged($event)"',
    '(officialMapYearSelected)="onOfficialMapYearSelected($event)"',
  ]) {
    assert.ok(pageTemplate.includes(contract), `${contract} must remain wired on the map page`);
  }

  assert.ok(viewTemplate.includes("activeTab === 'official'"));
  assert.ok(viewTemplate.includes('<app-park-official-maps-section'));
  assert.ok(viewTemplate.includes('<app-park-items-map-section'));
});

test('Leaflet interactions and navigation markers remain wired', () => {
  for (const contract of [
    '<app-leaflet-map',
    '[fitBounds]="true"',
    '[fitBoundsMaxZoom]="17"',
    '[markers]="navigationMarkers"',
    'map?.zoneFilters',
    'map?.categoryFilters',
    'map?.unlocatedItems',
  ]) {
    assert.ok(interactiveTemplate.includes(contract), `${contract} must remain in the park items map`);
  }

  for (const contract of [
    '[fitBounds]="true"',
    '[selectedMarkerId]="selectedParkId()"',
    '(markerClick)="onMarkerClick($event)"',
  ]) {
    assert.ok(listTemplate.includes(contract), `${contract} must remain in the catalogue map`);
  }
});

test('official maps preserve year, original documents, metadata and sources', () => {
  for (const contract of [
    '@if (years.length > 1)',
    '[value]="selectedYear"',
    '(change)="onYearChanged($event)"',
    '[href]="map.displayDocumentUrl"',
    '[src]="map.previewImageUrl"',
    'map.originalFileName',
    'map.sourcePageUrl',
    'map.fileSizeLabel',
  ]) {
    assert.ok(officialTemplate.includes(contract), `${contract} must remain in official maps`);
  }
});

test('map containers use flat, stable Playground surfaces', () => {
  const styles = [viewStyles, interactiveStyles, officialStyles, leafletStyles].join('\n');
  assert.match(viewStyles, /\.park-subpage-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(interactiveStyles, /\.park-items-map[\s\S]*?background:\s*var\(--app-surface\)[\s\S]*?box-shadow:\s*none/);
  assert.match(leafletStyles, /\.leaflet-map-container[\s\S]*?border-radius:\s*var\(--app-radius-md\)[\s\S]*?box-shadow:\s*none/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /mask-image\s*:/);
  assert.doesNotMatch(styles, /!important/);
});
