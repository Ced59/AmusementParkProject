import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { publicParkItemsReadEvidence } from '../design-v6/public-directory-evidence.mjs';

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
const imageLayout = read('src/app/features/public/parks/models/park-detail-image-layout.ts');
const itemImageLayout = read('src/app/features/public/park-items/models/park-item-detail-image-layout.ts');

test('the item header uses a distinct photo, full-flow siblings and a readable editorial identity', () => {
  assert.ok(itemTemplate.includes('[class.item-detail-hero--has-photo]="!!currentDetail.heroPhoto"'));
  assert.doesNotMatch(itemTemplate, /item-detail-hero__bg|item-detail-hero__accent/);
  assert.match(itemStyles, /grid-template-columns:\s*minmax\(0, 1\.55fr\) minmax\(0, 0\.85fr\)/);
  assert.match(itemStyles, /\.item-detail-hero__photo\s*\{[^}]*position:\s*relative;[^}]*aspect-ratio:\s*16 \/ 10/);
  assert.doesNotMatch(itemStyles, /opacity:\s*0\.(?:24|12|2);/);
  assert.match(itemStyles, /\.item-detail-hero__siblings\s*\{[^}]*display:\s*grid/);
  assert.match(itemStyles, /\.item-detail-hero__sibling strong\s*\{[^}]*overflow-wrap:\s*anywhere/);
  assert.doesNotMatch(itemStyles, /-webkit-line-clamp/);
  assert.ok(itemTemplate.indexOf('item-detail-hero__name') < itemTemplate.indexOf('item-detail-hero__tags'));
  assert.ok(itemTemplate.includes('loading="eager"') && itemTemplate.includes('fetchPriority="high"'));
});

test('item photo sizes and its existing preload follow the same V6 envelope at all QA widths', () => {
  const sizes = itemImageLayout.match(/PARK_ITEM_DETAIL_HERO_IMAGE_SIZES: string = '([^']+)'/)[1];
  for (const width of [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920]) {
    let advertised;
    for (const clause of sizes.split(',')) {
      const condition = clause.trim().match(/^\(max-width: (\d+)px\) (.+)$/);
      if (condition && width > Number(condition[1])) continue;
      const value = (condition ? condition[2] : clause.trim()).replace(/^calc\(|\)$/g, '');
      advertised = value.split(' - ').map(part => part.endsWith('vw') ? Number.parseFloat(part) * width / 100 : Number.parseFloat(part)).reduce((total, part) => total - part);
      break;
    }
    const shell = Math.min(1160, width - (width <= 480 ? 28 : width <= 680 ? 30 : 48));
    const expected = (width <= 960 ? shell : (shell - 22) * 1.55 / 2.4) - 2;
    assert.ok(Math.abs(advertised - expected) < 1, `item hero at ${width}px`);
  }
  for (const path of ['pages/park-item-detail-page.component.ts', 'ui/park-item-detail-view.component.ts']) {
    assert.ok(read(`src/app/features/public/park-items/${path}`).includes('heroImageSizes: string = PARK_ITEM_DETAIL_HERO_IMAGE_SIZES'));
  }
});

test('item evidence records only visible names and server pagination for the exact public parent endpoint', () => {
  const url = '/api/park-items/park/00000000-0000-0000-0000-000000000001?page=2&size=12';
  const body = Buffer.from(JSON.stringify({ data: [{ name: 'Attraction été', isVisible: true, internalNote: 'not evidence' }], pagination: { currentPage: 2, itemsPerPage: 12, totalItems: 38 } }));
  assert.deepEqual(publicParkItemsReadEvidence(url, 200, body), { names: ['Attraction été'], pagination: { currentPage: 2, itemsPerPage: 12, totalItems: 38 } });
  assert.equal(publicParkItemsReadEvidence('/api/admin/park-items', 200, Buffer.from('not JSON')), undefined);
  assert.equal(publicParkItemsReadEvidence(url, 503, Buffer.from('not JSON')), undefined);
  assert.throws(() => publicParkItemsReadEvidence(`${url}&includeHidden=true`, 200, body), /Hidden item evidence is prohibited/);
  assert.throws(() => publicParkItemsReadEvidence(url, 200, Buffer.from(JSON.stringify({ data: [{ name: 'Hidden', isVisible: false }], pagination: { currentPage: 2, itemsPerPage: 12, totalItems: 38 } }))), /Invalid or non-public/);
});

test('the item browser recipe preserves paging, filters, empty state, full detail and exact browser-history entry', () => {
  const recipe = read('tools/design-v6/exercise-public-park-items.mjs');
  for (const contract of [
    'item inventory page two', 'item inventory browser forward', 'for (const rows of [6, 24, 12, 48])',
    'item inventory restaurant category', 'item inventory category reset', 'item inventory includes visible closed items',
    'item inventory renders its real empty filtered state', 'real item card opens its full detail page, never a popup',
    'item detail back restores filtered inventory', 'item recipe preserves the original park navigation history',
    'API names and page match the item cards',
  ]) assert.ok(recipe.includes(contract), `${contract} must remain in the read-only recipe`);
  assert.ok(recipe.includes('navigation.currentEntry.index'));
  assert.doesNotMatch(recipe, /nativeFetch|fetch\(|POST|PUT|PATCH|DELETE/);
});

test('the editorial park header separates real photography and puts every action below the identity row', () => {
  assert.ok(parkTemplate.includes('[class.park-banner--has-photo]="currentPark.heroImageId && currentPark.heroImageId !== currentPark.logoImageId"'));
  assert.match(parkStyles, /\.park-banner--has-photo \.park-banner__content\s*\{[^}]*grid-template-columns:\s*72px minmax\(0, 1fr\) 52%/);
  assert.match(parkStyles, /\.park-banner__actions\s*\{[^}]*grid-column:\s*1 \/ -1/);
  assert.match(parkStyles, /font-size:\s*clamp\(34px, 4\.4vw, 57px\)/);
  assert.match(parkStyles, /\.park-banner__actions span\s*\{[^}]*white-space:\s*normal;[^}]*overflow-wrap:\s*anywhere/);
  assert.doesNotMatch(parkStyles, /opacity:\s*0\.(?:24|1);/);
  assert.doesNotMatch(parkStyles, /\.park-banner__actions\s*\{[^}]*flex-direction:\s*column/);
  assert.doesNotMatch(parkTemplate, /park-banner__bg|park-banner__pattern/);
  assert.match(parkStyles, /\.park-banner__info\s*\{[^}]*display:\s*contents/);
  assert.match(parkStyles, /\.park-banner__meta\s*\{[^}]*grid-column:\s*1 \/ -1;[^}]*grid-row:\s*2/);
  assert.match(parkStyles, /app-user-collection-actions:empty/);
});

test('park image sizes follow the V6 shell gutters and existing detail columns at every QA width', () => {
  const hero = imageLayout.match(/PARK_DETAIL_HERO_IMAGE_SIZES: string = '([^']+)'/)[1];
  const photo = imageLayout.match(/PARK_DETAIL_MAIN_PHOTO_SIZES: string = '([^']+)'/)[1];
  function advertisedWidth(sizes, width) {
    for (const clause of sizes.split(',')) {
      const condition = clause.trim().match(/^\(max-width: (\d+)px\) (.+)$/);
      if (condition && width > Number(condition[1])) continue;
      const value = (condition ? condition[2] : clause.trim()).replace(/^calc\(|\)$/g, '');
      return value.split(' - ').map(part => part.endsWith('vw') ? Number.parseFloat(part) * width / 100 : Number.parseFloat(part))
        .reduce((total, part) => total - part);
    }
    throw new Error('Missing image size fallback');
  }
  for (const width of [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920]) {
    const shell = Math.min(1160, width - (width <= 480 ? 28 : width <= 680 ? 30 : 48));
    const heroWidth = width <= 680 ? shell : shell * 0.52;
    const photoWidth = width <= 680 ? shell - 32 : width <= 959 ? shell - 24 : width <= 1119 ? shell - 40 : shell - 40 - 22.4 - 410;
    assert.ok(Math.abs(advertisedWidth(hero, width) - heroWidth) <= 1, `hero at ${width}px`);
    assert.ok(Math.abs(advertisedWidth(photo, width) - photoWidth) <= 1, `photo at ${width}px`);
  }
});

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
  assert.match(itemListStyles, /\.items-map-panel__chip--active\s*\{[^}]*color:\s*var\(--app-heading\)/);
  assert.doesNotMatch(itemListStyles, /\.items-map-panel__chip--all\s*\{[^}]*color:\s*var\(--app-primary\)/);
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

test('the standalone identity precedes a distinct real photograph and dense technical sidebar', () => {
  assert.doesNotMatch(standaloneTemplate, /standalone-hero__backdrop|standalone-hero__pattern|standalone-hero__media/);
  assert.ok(standaloneTemplate.indexOf('<h1>') < standaloneTemplate.indexOf('standalone-hero__chips'));
  assert.ok(standaloneTemplate.indexOf('standalone-hero__actions') < standaloneTemplate.indexOf('standalone-main__media'));
  assert.ok(standaloneTemplate.indexOf('<aside') < standaloneTemplate.indexOf('detailsRows().length'));
  assert.match(standaloneStyles, /\.standalone-main__media\s*\{[^}]*position:\s*relative;[^}]*height:\s*340px/);
  assert.match(standaloneStyles, /grid-template-columns:\s*minmax\(0, 1\.45fr\) minmax\(0, 0\.8fr\)/);
  assert.doesNotMatch(standaloneStyles, /opacity:\s*0\.(?:24|12);/);
  assert.match(standaloneStyles, /\.standalone-hero__actions a\s*\{[^}]*min-height:\s*44px;[^}]*white-space:\s*normal/);
  assert.equal((standaloneTemplate.match(/fetchPriority="high"/g) ?? []).length, 1);
  assert.doesNotMatch(standaloneTemplate, /Pendolino|1 900|2007|isVisible\s*=|isPublished\s*=/);
});

test('the standalone photograph sizes match its actual shell and technical column', () => {
  const sizes = standaloneTemplate.match(/sizes="([^"]+)"/)[1];
  for (const width of [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920]) {
    let advertised;
    for (const clause of sizes.split(',')) {
      const condition = clause.trim().match(/^\(max-width: (\d+)px\) (.+)$/);
      if (condition && width > Number(condition[1])) continue;
      const value = (condition ? condition[2] : clause.trim()).replace(/^calc\(|\)$/g, '');
      advertised = value.split(' - ').map(part => part.endsWith('vw') ? Number.parseFloat(part) * width / 100 : Number.parseFloat(part)).reduce((total, part) => total - part);
      break;
    }
    const shell = Math.min(1160, width - (width <= 480 ? 28 : width <= 680 ? 30 : 48));
    const expected = (width < 960 ? shell : (shell - 26) * 1.45 / 2.25) - 2;
    assert.ok(Math.abs(advertised - expected) < 1, `standalone photograph at ${width}px`);
  }
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
