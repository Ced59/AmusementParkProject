import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');
const parkTemplate = read('src/app/features/public/parks/ui/park-detail-view.component.html');
const parkStyles = read('src/app/features/public/parks/ui/park-detail-view.component.scss');
const itemListTemplate = read('src/app/features/public/park-items/ui/park-items-list-view.component.html');
const itemListStyles = read('src/app/features/public/park-items/ui/park-items-list-view.component.scss');
const itemTemplate = read('src/app/features/public/park-items/ui/park-item-detail-view.component.html');
const itemStyles = read('src/app/features/public/park-items/ui/park-item-detail-view.component.scss');
const standaloneTemplate = read('src/app/features/public/standalone-attractions/pages/standalone-attraction-detail-page.component.html');
const standaloneStyles = read('src/app/features/public/standalone-attractions/pages/standalone-attraction-detail-page.component.scss');
const standalonePricingTemplate = read('src/app/features/public/standalone-attractions/pages/standalone-attraction-pricing-card.component.html');
const standalonePricingStyles = read('src/app/features/public/standalone-attractions/pages/standalone-attraction-pricing-card.component.scss');
const zonesTemplate = read('src/app/features/public/parks/ui/park-zones-view.component.html');
const zonesStyles = read('src/app/features/public/parks/ui/park-zones-view.component.scss');
const zoneTemplate = read('src/app/features/public/parks/ui/park-zone-view.component.html');
const zoneStyles = read('src/app/features/public/parks/ui/park-zone-view.component.scss');
const zonesSectionStyles = read('src/app/features/public/parks/ui/park-zones-section.component.scss');

test('the Fleury SSR review inventories the same visible open and closed population as its list', () => {
  const capture = read('tools/design-v6/capture-public-ssr.mjs');
  const inventoryRequests = capture.match(/\/api\/park-items\/park\/[^`]+\?page=[12]&size=48&closedFilter=all/g);
  assert.equal(inventoryRequests?.length, 2, 'both bounded API pages must explicitly include closed items');
  assert.ok(capture.includes('/items?size=48&page=${page}&closed=all'));
  assert.ok(capture.includes("expected.some((item) => item.isVisible !== true)"));
  assert.ok(capture.includes("closedFilter: 'all'"));
});

test('the park detail preserves every public action and conditional destination', () => {
  for (const contract of [
    '<app-rating-stars',
    '<app-comment-summary-link',
    '<app-user-collection-actions',
    '<app-watch-subscription-action',
    '(click)="createVisit()"',
    '(click)="goBack()"',
    '(click)="goToExplore()"',
    '[routerLink]="currentPark.zonesLink"',
    '[routerLink]="currentPark.imagesLink"',
    '[routerLink]="currentPark.videosLink"',
    '[routerLink]="currentPark.historyLink"',
    '[routerLink]="currentPark.mapLink"',
    '[href]="currentPark.websiteUrl | safeExternalUrl"',
  ]) {
    assert.ok(parkTemplate.includes(contract), `${contract} must remain available`);
  }
});

test('the park detail preserves rich, practical and stateful sections', () => {
  for (const contract of [
    '<app-park-lifecycle-notice',
    '<app-public-live-panel',
    '<app-ui-stat-card',
    '[innerHTML]="currentPark.description | safeRichHtml"',
    '<app-park-content-summary',
    'currentPark.practicalRows',
    '<app-park-location-section',
    '[routerLink]="currentPark.pricingLink"',
    '<app-park-opening-hours-card',
    '<app-park-weather-card',
    '<app-public-share-panel',
    '<app-park-nearby-section',
  ]) {
    assert.ok(parkTemplate.includes(contract), `${contract} must remain rendered`);
  }
});

test('the park detail keeps a single eager hero and lazy secondary photography', () => {
  assert.match(parkTemplate, /currentPark\.heroImageId[\s\S]*?loading="eager"[\s\S]*?fetchPriority="high"/);
  assert.match(parkTemplate, /currentPark\.primaryPhoto[\s\S]*?loading="lazy"[\s\S]*?fetchPriority="low"/);
  assert.match(parkTemplate, /\[responsiveWidths\]="heroImageResponsiveWidths"/);
  assert.match(parkTemplate, /placeholderKind="photo"/);
});

test('the park detail uses the editorial Playground surface at every breakpoint', () => {
  assert.match(parkStyles, /--park-detail-content-max-width:\s*var\(--content-max-width, 1160px\)/);
  assert.match(parkStyles, /\.park-banner[\s\S]*?border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(parkStyles, /\.park-desc,[\s\S]*?border-radius:\s*var\(--radius-md\)[\s\S]*?background:\s*var\(--app-surface\)/);
  assert.match(parkStyles, /@media \(max-width:\s*680px\)/);
  assert.doesNotMatch(parkStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(parkStyles, /mask-image\s*:/);
  assert.doesNotMatch(parkStyles, /!important/);
});

test('the ParkItem inventory preserves all filters, zone map, unlocated items and server paging', () => {
  for (const contract of [
    '[selectedCategory]="selectedCategory"',
    '[selectedType]="selectedType"',
    '[selectedZoneId]="selectedZoneId"',
    '[selectedClosedFilter]="selectedClosedFilter"',
    '(searchChanged)="onSearchChanged($event)"',
    '(categoryChanged)="onCategoryChanged($event)"',
    '(typeChanged)="onTypeChanged($event)"',
    '(zoneChanged)="onZoneChanged($event)"',
    '(closedFilterChanged)="onClosedFilterChanged($event)"',
    '<app-leaflet-map',
    '<app-unlocated-items-panel',
    '<app-park-item-card-view',
    '[rowsPerPageOptions]="[6, 12, 24, 48]"',
    '(pageChanged)="onPageChanged($event)"',
    '<app-public-share-panel',
  ]) {
    assert.ok(itemListTemplate.includes(contract), `${contract} must remain in the ParkItem inventory`);
  }
});

test('the ParkItem detail preserves navigation, media, technical, access and social workflows', () => {
  for (const contract of [
    'siblingNavigation.previous',
    'siblingNavigation.next',
    '[routerLink]="categoryNavigation.routerLink"',
    '[routerLink]="typeNavigation.routerLink"',
    '[routerLink]="subtypeNavigation.routerLink"',
    '[queryParams]="zoneNavigation.queryParams ?? null"',
    '<app-rating-stars',
    '<app-comment-summary-link',
    '<app-user-collection-actions',
    '<app-watch-subscription-action',
    '<app-public-live-panel',
    '<app-park-item-passport-ride-panel',
    '[innerHTML]="currentDetail.description | safeRichHtml"',
    '<app-leaflet-static-map',
    'currentDetail.accessConditions',
    'currentDetail.relatedItems',
    '[routerLink]="currentDetail.videosLink"',
    '[routerLink]="currentDetail.historyLink"',
    '<app-public-share-panel',
  ]) {
    assert.ok(itemTemplate.includes(contract), `${contract} must remain in the ParkItem detail`);
  }
});

test('ParkItem inventory and detail use flat Playground surfaces', () => {
  assert.match(itemListStyles, /\.results-workbench \.app-search-panel[\s\S]*?border-radius:\s*var\(--radius-md\)/);
  assert.match(itemStyles, /\.item-detail-hero[\s\S]*?border-top:\s*4px solid var\(--item-tone\)/);
  assert.match(itemStyles, /\.detail-panel[\s\S]*?background:\s*var\(--app-surface\)[\s\S]*?border-radius:\s*var\(--app-radius-md\)/);
  assert.match(itemStyles, /@media \(max-width:\s*680px\)/);
  assert.doesNotMatch(`${itemListStyles}\n${itemStyles}`, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(itemStyles, /mask-image\s*:/);
  assert.doesNotMatch(itemStyles, /!important/);
});

test('the standalone attraction remains autonomous and preserves every practical section', () => {
  for (const contract of [
    "[routerLink]=\"['/', currentLanguage(), 'parks']\"",
    '[routerLink]="historyLink(current)"',
    '[href]="websiteUrl"',
    '[innerHTML]="description() | safeRichHtml"',
    '<app-standalone-attraction-pricing-card',
    '<app-park-opening-hours-card',
    '<app-park-weather-card',
    '*ngIf="hasCoordinates()"',
    '<app-leaflet-map',
  ]) {
    assert.ok(standaloneTemplate.includes(contract), `${contract} must remain in the standalone attraction detail`);
  }

  assert.doesNotMatch(standaloneTemplate, /park-item-detail|parentPark|parkParent/);
  assert.ok(standalonePricingTemplate.includes('pricing.purchaseUrl'));
  assert.ok(standalonePricingTemplate.includes('summary.purchaseUrl'));
});

test('zone list and detail preserve counts, deep links, filters, map and item cards', () => {
  for (const contract of [
    'currentPage.zoneCount',
    'currentPage.totalItems',
    'zone.description | safeRichHtml',
    '[routerLink]="zone.zoneLink"',
    '<app-public-share-panel',
  ]) {
    assert.ok(zonesTemplate.includes(contract), `${contract} must remain in the zone inventory`);
  }

  for (const contract of [
    'currentPage.zoneDescription | safeRichHtml',
    '[routerLink]="currentPage.allItemsLink"',
    '[queryParams]="currentPage.allItemsQueryParams"',
    '<app-leaflet-map',
    '[fitBounds]="true"',
    '<app-park-item-card-view',
    '<app-public-share-panel',
  ]) {
    assert.ok(zoneTemplate.includes(contract), `${contract} must remain in the zone detail`);
  }
});

test('standalone and zone surfaces use the flat Playground visual language', () => {
  const styles = [standaloneStyles, standalonePricingStyles, zonesStyles, zoneStyles, zonesSectionStyles].join('\n');
  assert.match(standaloneStyles, /--standalone-content-max-width:\s*var\(--content-max-width,\s*1160px\)/);
  assert.match(standaloneStyles, /\.standalone-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(zonesStyles, /\.zones-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(zoneStyles, /\.zone-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /mask-image\s*:/);
  assert.doesNotMatch(styles, /!important/);
});
