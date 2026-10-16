import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { mapViewportSettled } from '../design-v6/exercise-public-map.mjs';
import { canRenderPublicDocument } from '../design-v6/public-document-evidence.mjs';
import { mapPopupLayoutIssues, mapReviewScrollDelta } from '../design-v6/map-popup-layout.mjs';

test('map review recentres a real page after responsive reflow, allowing for the fixed bottom navigation', () => {
  assert.equal(mapReviewScrollDelta({ mapTop: -50, mapHeight: 352, viewportHeight: 640, bottomNavigationHeight: 76 }), -156);
  assert.equal(mapReviewScrollDelta({ mapTop: 106, mapHeight: 352, viewportHeight: 640, bottomNavigationHeight: 76 }), 0);
  assert.equal(mapReviewScrollDelta({ mapTop: 30, mapHeight: 500, viewportHeight: 480, bottomNavigationHeight: 76 }), 30);
});

test('map popup geometry rejects clipped headings, covered actions and compressed touch controls', () => {
  const layout = { map: { x: 10, y: 20, right: 310, bottom: 440 }, popup: { x: 50, y: 40, right: 270, bottom: 200 }, heading: { unobscured: true }, close: { x: 238, y: 40, right: 270, bottom: 72, unobscured: true }, actions: [{ x: 62, y: 136, right: 180, bottom: 180, unobscured: true }], zoomControls: [{ unobscured: true }, { unobscured: true }] };
  assert.deepEqual(mapPopupLayoutIssues(layout), []);
  assert.ok(mapPopupLayoutIssues({ ...layout, popup: { ...layout.popup, y: 10 } }).includes('popup clipped by map bounds'));
  assert.ok(mapPopupLayoutIssues({ ...layout, close: { ...layout.close, unobscured: false } }).includes('popup control covered or outside the visible viewport'));
  assert.ok(mapPopupLayoutIssues({ ...layout, close: { ...layout.close, right: 255 } }).includes('compressed popup target'));
  assert.ok(mapPopupLayoutIssues({ ...layout, heading: { unobscured: false } }).includes('popup heading covered by map controls'));
  assert.ok(mapPopupLayoutIssues({ ...layout, heading: null }).includes('missing actual map popup content'));
  assert.ok(mapPopupLayoutIssues({ ...layout, zoomControls: [{ unobscured: false }, { unobscured: true }] }).includes('map zoom control covered by popup'));
});

test('native public detail navigation renders the same compiled SSR build, never a production V5 or private document', () => {
  const path = '/fr/park/00000000-0000-0000-0000-000000000001/park/item/00000000-0000-0000-0000-000000000002/item';
  assert.equal(canRenderPublicDocument(path), true);
  assert.equal(canRenderPublicDocument('/pl/attraction/00000000-0000-0000-0000-000000000001/ride/history?page=2'), true);
  for (const forbidden of ['/fr/admin/parks', '/fr/account/profile', '/api/parks', 'https://amusement-parks.fun' + path, '//evil.invalid' + path, path + '\\escape', path + 'a'.repeat(2049), path + '/%2e%2e/%2e%2e/%2e%2e/admin']) assert.equal(canRenderPublicDocument(forbidden), false);
  const capture = read('tools/design-v6/capture-public-ssr.mjs');
  assert.ok(capture.includes('!options.networkProbe && options.renderDocument'));
  assert.ok(capture.includes('result.documentNavigations'));
  assert.ok(capture.includes('const previousReads = reads'));
  assert.ok(capture.includes('reads = previousReads'));
  assert.ok(capture.includes('await captureHydratedHome(homeRawHtml, { renderDocument })'));
});

test('the separate map zoom recipe requires stable initial public DOM without weakening the actual zoom assertion', () => {
  const stable = { transform: 'translate3d(10px, 20px, 0) scale(32768)', animating: false };
  assert.equal(mapViewportSettled([stable, stable, stable]), true);
  assert.equal(mapViewportSettled([stable, stable]), false);
  assert.equal(mapViewportSettled([stable, { ...stable, transform: 'different' }, stable]), false);
  assert.equal(mapViewportSettled([stable, stable, { ...stable, animating: true }]), false);
  assert.equal(mapViewportSettled([{ ...stable, paneTransform: 'first' }, { ...stable, paneTransform: 'second' }, { ...stable, paneTransform: 'second' }]), false);
  assert.equal(mapViewportSettled([{ transform: '' }, { transform: '' }, { transform: '' }]), false);
});

test('normal popup captures wait for actual category bounds and auto-pan without weakening rapid navigation tests', () => {
  const recipe = readFileSync(resolve(import.meta.dirname, '../design-v6/exercise-public-map.mjs'), 'utf8');
  assert.ok(recipe.includes('paneTransform: document.querySelector'));
  assert.ok(recipe.includes('leaflet-pan-anim'));
  assert.ok(recipe.indexOf('category reset finishes native bounds') < recipe.indexOf("document.querySelector('.park-items-map .leaflet-marker-icon').click()"));
  assert.ok(recipe.includes('normal popup auto-pan finishes before responsive geometry capture'));
});

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

test('real popup surfaces/actions are themed and open popup reflow uses public Leaflet update without changing zoom or data', () => {
  assert.match(leafletStyles, /\.leaflet-popup-content-wrapper\s*\{[^}]*border-radius:\s*var\(--app-radius-sm\);[^}]*background:\s*var\(--app-field\)/);
  assert.doesNotMatch(leafletStyles, /#(?:ff5a1f|c4ff27|1a140f|3a3026)/i);
  assert.ok(leafletStyles.includes('min-height: 2.75rem'));
  assert.ok(leafletStyles.includes('max-height: min(14rem, 224px)'));
  const popupLayout = read('src/app/ui/maps/leaflet/leaflet-popup-layout.factory.ts');
  assert.ok(popupLayout.includes('autoPanPaddingTopLeft: [5, 80]'));
  assert.doesNotMatch(popupLayout, /(?:autoPan|keepInView|zoom|center)\s*:|(?:setView|panBy)\s*\(/);
  assert.match(interactiveStyles, /\.leaflet-map-popup__lines span:first-child\s*\{[^}]*color:\s*var\(--app-primary-strong\)/);
  assert.doesNotMatch(interactiveStyles, /#6a4424/);
  const source = read('src/app/shared/components/leaflet-map/leaflet-map.component.ts');
  const reflow = source.slice(source.indexOf('  private refreshOpenPopupLayout()'), source.indexOf('  private bindViewportChangeListeners()'));
  assert.ok(reflow.includes('popup?.isOpen()'));
  assert.ok(reflow.includes('popup.update()'));
  assert.ok(reflow.includes('this.bindInternalPopupNavigationLinks()'));
  assert.doesNotMatch(reflow, /openPopup\(|setView\(|setZoom\(|fitBounds\(|_popup|\.options\./);
});

test('actual Leaflet pins and clusters use flat themed Playground markers, not residual V5 gradients or the separate pictogram', () => {
  const styles = read('src/styles/_maps.scss').split('/* Centralized Leaflet marker design. */')[1].split('/* M10 responsive hardening')[0];
  assert.match(styles, /\.app-leaflet-marker__head\s*\{[^}]*background:\s*var\(--app-field\)/);
  assert.match(styles, /\.app-leaflet-marker__symbol\s*\{[^}]*color:\s*var\(--app-heading\)/);
  assert.doesNotMatch(styles, /gradient\(|drop-shadow\(|text-shadow:|border-radius:\s*(?:50%|1[468]px)/);
  const factory = read('src/app/ui/maps/leaflet/leaflet-marker-icon.factory.ts');
  assert.equal((factory.match(/color: 'var\(/g) ?? []).length, 36);
  assert.doesNotMatch(factory, /color: '#|logo-amusementpark-marker|<img/);
  assert.ok(factory.includes('iconSize: isItemMarker ? ItemMarkerSize : LocationMarkerSize'));
  assert.ok(factory.includes('popupAnchor: isItemMarker ? ItemPopupAnchor : LocationPopupAnchor'));
});

test('map browser acceptance uses real zoom controls, category counts, popup detail routes and available official years', () => {
  const recipe = read('tools/design-v6/exercise-public-map.mjs');
  for (const token of ['leaflet-control-zoom-in', 'beforeZoom', 'category.count', 'category.total', 'leaflet-map-popup__action--detail', 'mapKey', 'official.multiYearNotExercised = false', 'externalTilesAndDirectionsNotValidated: true', 'pointerDragNotExercised: true']) assert.ok(recipe.includes(token));
  assert.doesNotMatch(recipe, /fetch\(|localStorage\.setItem|setView\(|setZoom\(|\.fire\(|_leaflet_id|new Date\(/);
  assert.ok(read('tools/design-v6/capture-public-ssr.mjs').includes('await exercisePublicMap('));
});

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
  assert.match(viewStyles, /\.park-subpage-hero[\s\S]*?@include m\.public-editorial-header/);
  assert.match(interactiveStyles, /\.park-items-map[\s\S]*?background:\s*var\(--app-surface\)[\s\S]*?box-shadow:\s*none/);
  assert.match(leafletStyles, /\.leaflet-map-container[\s\S]*?border-radius:\s*var\(--app-radius-md\)[\s\S]*?box-shadow:\s*none/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /mask-image\s*:/);
  assert.doesNotMatch(styles, /!important/);
});
