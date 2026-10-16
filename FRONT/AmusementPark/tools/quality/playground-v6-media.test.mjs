import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { Script, runInNewContext } from 'node:vm';
import { publicParkGalleryPages, publicPhotoReadEvidence } from '../design-v6/public-photo-evidence.mjs';
import { photoLightboxLayoutIssues, photoLightboxLayoutExpression } from '../design-v6/photo-lightbox-layout.mjs';
import { browserModuleEvidence } from '../design-v6/browser-module-evidence.mjs';
import { publicVideoReadEvidence } from '../design-v6/public-video-evidence.mjs';
import { publicVideoCardIdsExpression } from '../design-v6/exercise-public-media-history.mjs';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const carouselTemplate = read('src/app/ui/media/photo-carousel/ui-photo-carousel.component.html');
const carouselStyles = read('src/app/ui/media/photo-carousel/ui-photo-carousel.component.scss');
const parkImagesTemplate = read('src/app/features/public/parks/ui/park-images-view.component.html');
const parkImagesStyles = read('src/app/features/public/parks/ui/park-images-view.component.scss');
const itemImagesTemplate = read('src/app/features/public/park-items/ui/park-item-images-view.component.html');
const itemImagesStyles = read('src/app/features/public/park-items/ui/park-item-images-view.component.scss');
const videoListTemplate = read('src/app/features/public/videos/ui/public-video-list-view.component.html');
const videoListStyles = read('src/app/features/public/videos/ui/public-video-list-view.component.scss');
const videoWatchTemplate = read('src/app/features/public/videos/ui/public-video-watch-view.component.html');
const videoWatchStyles = read('src/app/features/public/videos/ui/public-video-watch-view.component.scss');

test('the exact generated browser expression parses and extracts park/item video IDs without template-literal slash escaping', () => {
  assert.doesNotThrow(() => new Script(`JSON.stringify(${publicVideoCardIdsExpression}) === '[]'`));
  const paths = ['/fr/park/park-id/name/videos/first-id/title', '/fr/park/park-id/name/item/item-id/name/videos/second-id/title'];
  const actual = runInNewContext(publicVideoCardIdsExpression, { document: { querySelectorAll: () => paths.map(path => ({ getAttribute: () => path })) } });
  assert.equal(JSON.stringify(actual), JSON.stringify(['first-id', 'second-id']));
});

test('lightbox review waits for stable real viewport-unit layout while keeping all geometry and hit-test requirements', () => {
  assert.doesNotThrow(() => new Script(photoLightboxLayoutExpression));
  const capture = read('tools/design-v6/capture-public-ssr.mjs');
  assert.ok(capture.includes('previousLayout === serialized'));
  assert.ok(capture.includes('layout.width === width && layout.height === height && photoLightboxLayoutIssues(layout).length === 0'));
  assert.ok(capture.includes('result.lastLightboxLayout = layout'));
  assert.ok(photoLightboxLayoutExpression.includes('document.elementFromPoint'));
});

test('video acceptance retains only published IDs and bounded pagination for the exact public owner endpoints', () => {
  const id = '00000000-0000-0000-0000-000000000001';
  const pagination = { currentPage: 1, itemsPerPage: 24, totalItems: 1 };
  const video = { id, isPublished: true, externalMetadata: { channel: 'not retained' }, creatorName: 'not retained' };
  const path = `/api/videos?ownerType=PARK&ownerId=${id}`;
  const bytes = Buffer.from(JSON.stringify({ data: [video], pagination }));
  assert.deepEqual(publicVideoReadEvidence(path, 200, bytes), { kind: 'park', ids: [id], pagination });
  assert.equal(publicVideoReadEvidence(path.replace('ownerType=PARK', 'ownerType=Park'), 200, bytes), undefined);
  assert.equal(publicVideoReadEvidence(path.replace('ownerType=PARK', 'ownerType=PARK_ITEM'), 200, bytes), undefined);
  const itemPath = `/api/videos/parks/${id}/park-items`;
  const items = Buffer.from(JSON.stringify({ data: [{ video, item: { name: 'Public item' } }], pagination }));
  assert.deepEqual(publicVideoReadEvidence(itemPath, 200, items), { kind: 'items', ids: [id], pagination });
  assert.equal(publicVideoReadEvidence('/api/videos', 200, Buffer.from('not parsed')), undefined);
  assert.equal(publicVideoReadEvidence('/api/admin/videos', 200, bytes), undefined);
  assert.equal(publicVideoReadEvidence(path, 503, bytes), undefined);
  assert.throws(() => publicVideoReadEvidence(`${path}&isPublished=false`, 200, bytes), /Unpublished/);
  assert.throws(() => publicVideoReadEvidence(path, 200, Buffer.from(JSON.stringify({ data: [{ ...video, isPublished: false }], pagination }))), /unpublished/);
  assert.throws(() => publicVideoReadEvidence(itemPath, 200, Buffer.from(JSON.stringify({ data: [{ video, item: { isVisible: false } }], pagination }))), /hidden/);
  assert.throws(() => publicVideoReadEvidence(path, 200, Buffer.alloc(2 * 1024 * 1024 + 1)), /response bound/);
  assert.throws(() => publicVideoReadEvidence(path, 200, Buffer.from(JSON.stringify({ data: [video], pagination: { ...pagination, itemsPerPage: 101 } }))), /pagination/);
});

test('video browser acceptance uses actual tabs, API IDs, query filters, empty results and complete watch pages', () => {
  const recipe = read('tools/design-v6/exercise-public-media-history.mjs');
  for (const contract of ['verifyVideos', 'pagination.itemsPerPage === 24', 'aria-selected', 'creatorName', 'video filters reset', 'videoWatch', 'externalPlaybackNotValidated: true', 'navigation.currentEntry.key']) assert.ok(recipe.includes(contract));
  assert.doesNotMatch(recipe, /fetch\(|\.submit\(|localStorage\.setItem|\.emit\(|\.ngOnInit\(/);
  assert.ok(read('tools/design-v6/capture-public-ssr.mjs').includes('await exercisePublicMediaHistory('));
});

test('lightbox geometry rejects offscreen controls, overlapping photographs and unreadable shrinking', () => {
  const layout = { width: 390, height: 640, documentWidth: 390,
    dialog: { x: 8, y: 8, right: 382, bottom: 632 }, close: { x: 324, y: 20, right: 368, bottom: 64 },
    image: { width: 320, height: 360, bottom: 450 }, footer: { y: 460 } };
  assert.deepEqual(photoLightboxLayoutIssues(layout), []);
  assert.ok(photoLightboxLayoutIssues({ ...layout, image: { ...layout.image, bottom: 510 } }).includes('image overlaps caption or thumbnails'));
  assert.ok(photoLightboxLayoutIssues({ ...layout, close: { ...layout.close, bottom: 700 } }).includes('close control outside viewport'));
  assert.ok(photoLightboxLayoutIssues({ ...layout, image: { ...layout.image, height: 20 } }).includes('image compressed into an unusable region'));
  assert.ok(photoLightboxLayoutIssues({ ...layout, closeUnobscured: false }).includes('close control covered by another layer'));
});

test('compiled-module diagnostics keep only import edges and bounded focus markers, never source content', () => {
  const source = 'import {a} from "./chunk-ABC123.js"; import "./chunk-DEF456.js"; const selector="cdk-focus-trap-anchor"; const hidden="cdk-visually-hidden"; const dialog="p-dialog-mask";';
  assert.deepEqual(browserModuleEvidence('chunk-test.js', source), { filename: 'chunk-test.js', bytes: Buffer.byteLength(source),
    staticImports: ['chunk-ABC123.js', 'chunk-DEF456.js'], behaviors: { focusTrap: true, visuallyHiddenStyles: true, sharedDialog: true } });
  assert.deepEqual(browserModuleEvidence('main.js', 'import("./chunk-LAZY.js")').staticImports, []);
});

test('photo evidence retains only published IDs and real bounded pagination', () => {
  const id = '00000000-0000-0000-0000-000000000001';
  const pagination = { currentPage: 1, itemsPerPage: 100, totalItems: 141 };
  const image = { id, isPublished: true, exifMetadata: { latitude: 'not evidence' }, credits: ['not evidence'] };
  const bytes = Buffer.from(JSON.stringify({ data: [image], pagination }));
  assert.deepEqual(publicPhotoReadEvidence(`/api/images/1/${id}/2?page=1&size=100`, 200, bytes), { kind: 'park', ids: [id], pagination });
  const items = Buffer.from(JSON.stringify({ data: [{ image, item: { isVisible: true, internalNote: 'not evidence' } }], pagination }));
  assert.deepEqual(publicPhotoReadEvidence(`/api/images/parks/${id}/park-items?page=1&size=100`, 200, items), { kind: 'items', ids: [id], pagination });
  const compactId = '1'.repeat(32);
  const compact = Buffer.from(JSON.stringify({ data: [{ image: { ...image, id: compactId }, item: { name: 'Public item' } }], pagination }));
  assert.deepEqual(publicPhotoReadEvidence(`/api/images/parks/${id}/park-items`, 200, compact), { kind: 'items', ids: [compactId], pagination });
  assert.equal(publicPhotoReadEvidence('/api/images', 200, Buffer.from('not JSON')), undefined);
  assert.equal(publicPhotoReadEvidence(`/api/images/1/${id}/2`, 503, Buffer.from('not JSON')), undefined);
  assert.throws(() => publicPhotoReadEvidence(`/api/images/1/${id}/2?includeUnpublished=true`, 200, bytes), /Unpublished/);
  assert.throws(() => publicPhotoReadEvidence(`/api/images/1/${id}/2`, 200, Buffer.from(JSON.stringify({ data: [{ ...image, isPublished: false }], pagination }))), /unpublished/);
  assert.throws(() => publicPhotoReadEvidence(`/api/images/parks/${id}/park-items`, 200, Buffer.from(JSON.stringify({ data: [{ image, item: { isVisible: false } }], pagination }))), /hidden/);
});

test('gallery evidence excludes late summary pages and identifies both complete media types', () => {
  const parkId = '00000000-0000-0000-0000-000000000001';
  const page = (type, size, id, owner = parkId) => ({ path: `/api/images/1/${owner}/${type}`,
    photos: { kind: 'park', ids: [id], pagination: { currentPage: 1, itemsPerPage: size, totalItems: 1 } } });
  const photo = page(2, 100, 'photo');
  const logo = page(1, 100, 'logo');
  const reads = [page(2, 24, 'summary'), page(1, 1, 'summary-logo'), photo,
    page(1, 100, 'other-park', '00000000-0000-0000-0000-000000000002'), photo];
  assert.deepEqual(publicParkGalleryPages(reads, parkId), [photo]);
  assert.deepEqual(publicParkGalleryPages([...reads, logo, page(2, 1, 'late-summary')], parkId), [logo, photo]);
});

test('the photo recipe exercises real controls and 100-plus server-paginated media without writes', () => {
  const recipe = read('tools/design-v6/exercise-public-photos.mjs');
  for (const contract of ['photo next control', 'photo previous control', 'photo year filter', 'photo tag filter', 'photo lightbox opens through the existing keyboard handler',
    'photo lightbox closes', 'item photo tab requests its real server page size', 'cumulative page', 'only after all server pages', 'gallery browser back restores the original full park entry']) {
    assert.ok(recipe.includes(contract), `${contract} must remain in the read-only photo recipe`);
  }
  assert.doesNotMatch(recipe, /nativeFetch|fetch\(|POST|PUT|PATCH|DELETE/);
  assert.ok(recipe.includes('pagination.itemsPerPage !== 100'));
  assert.ok(recipe.includes('photoScenarios'));
  assert.ok(recipe.includes('await pressTab(true)'));
  assert.ok(recipe.includes('await pressTab(false)'));
  assert.ok(recipe.includes('photoFocusBeforeTab'));
});

test('photo galleries preserve filters, lazy thumbnails, credits, maps and lightbox controls', () => {
  assert.ok(carouselTemplate.includes('cdkTrapFocus [cdkTrapFocusAutoCapture]="true"'));
  assert.ok(carouselTemplate.includes('cdkFocusInitial'));
  assert.match(carouselTemplate, /<button\s+class="photo-lightbox__backdrop"\s+type="button"\s+tabindex="-1"\s+\[attr\.aria-label\]="closeFullscreenLabelKey \| translate"\s+\(click\)="closeLightbox\(\)"/);
  assert.doesNotMatch(carouselTemplate, /\$event\.stopPropagation/);
  for (const contract of [
    'selectYear(',
    'selectTag(',
    '(click)="previousPhoto()"',
    '(click)="nextPhoto()"',
    '(click)="openLightbox(activePhotoIndex)"',
    '(keydown.enter)="openLightbox(activePhotoIndex)"',
    '[alt]="currentPhoto.alt"',
    'loading="eager"',
    'loading="lazy"',
    'currentPhoto.sourceTitle',
    'currentPhoto.externalSourceUrl',
    '[markers]="mapMarkers"',
    'aria-modal="true"',
    '(click)="closeLightbox()"',
  ]) {
    assert.ok(carouselTemplate.includes(contract), `${contract} must remain in the photo carousel`);
  }
});

test('park and ParkItem galleries preserve real tabs and incremental loading', () => {
  for (const contract of [
    '[tabs]="tabsWithCounts()"',
    '[activeTabId]="activeTab"',
    '[photos]="photos"',
    '[disabled]="loadingMore"',
    '(click)="loadMore()"',
  ]) {
    assert.ok(parkImagesTemplate.includes(contract), `${contract} must remain in the park gallery`);
  }

  for (const contract of [
    '[photos]="photos"',
    '[disabled]="loadingMore"',
    '(click)="loadMore()"',
  ]) {
    assert.ok(itemImagesTemplate.includes(contract), `${contract} must remain in the ParkItem gallery`);
  }
});

test('video catalogue and player preserve filtering, pagination, metadata and unavailable fallbacks', () => {
  for (const contract of [
    '[value]="filters.type ?? \'\'"',
    '[value]="filters.tagId ?? \'\'"',
    '(submit)="applyCreatorFilter($event)"',
    '[routerLink]="video.detailLink"',
    '[alt]="video.title"',
    '[disabled]="loadingMore"',
    '(click)="loadMore()"',
  ]) {
    assert.ok(videoListTemplate.includes(contract), `${contract} must remain in the video catalogue`);
  }

  for (const contract of [
    '[routerLink]="previousVideo?.routerLink"',
    '[routerLink]="nextVideo?.routerLink"',
    '@if (video.embedUrl)',
    '[src]="video.embedUrl"',
    '[href]="video.originalUrl | safeExternalUrl"',
    'video.creatorName',
    'video.tags',
    '<app-public-share-panel',
  ]) {
    assert.ok(videoWatchTemplate.includes(contract), `${contract} must remain in the video player`);
  }
});

test('public gallery, map and video headers share the editorial shell without duplicate gutters', () => {
  const mixins = read('src/styles/_mixins.scss');
  assert.match(mixins, /@mixin public-editorial-page\s*\{[^}]*width:\s*100%;[^}]*min-width:\s*0/);
  assert.match(mixins, /@mixin public-editorial-header\s*\{[^}]*padding:\s*18px 0;[^}]*border-top:\s*4px solid var\(--app-primary\);[^}]*border-radius:\s*0;[^}]*background:\s*transparent/);
  for (const source of [parkImagesStyles, itemImagesStyles, videoListStyles, read('src/app/features/public/parks/ui/park-map-view.component.scss')]) {
    assert.ok(source.startsWith('@use "../../../../../styles/mixins" as m;'), 'component styles must resolve the existing mixin without a custom include path');
    assert.match(source, /@include m\.public-editorial-page/);
    assert.match(source, /@include m\.public-editorial-header/);
    assert.doesNotMatch(source, /width:\s*min\(100% -/);
  }
  const surfaces = read('src/styles/_surfaces.scss');
  assert.match(surfaces, /\.public-section-header__title\s*\{[^}]*line-height:\s*1\.2;[^}]*letter-spacing:\s*0;[^}]*text-transform:\s*none/);
  assert.match(surfaces, /h1\.public-section-header__title\s*\{[^}]*font-size:\s*clamp\(34px, 4\.4vw, 57px\)/);
});

test('photo filters keep readable label columns and an explicit local scroll at every width', () => {
  for (const rule of carouselStyles.matchAll(/\.photo-carousel-years\s*\{([^}]+)\}/g)) {
    assert.match(rule[1], /grid-auto-columns:\s*minmax\(11rem, 13rem\)/);
  }
  assert.match(carouselStyles, /\.photo-carousel-axis span\s*\{[^}]*white-space:\s*normal/);
  assert.doesNotMatch(carouselStyles.match(/\.photo-carousel-axis span\s*\{([^}]+)\}/)[1], /overflow:\s*hidden|text-overflow:\s*ellipsis/);
  assert.match(carouselStyles, /\.photo-carousel-years\s*\{[^}]*overflow-x:\s*auto/);
  assert.match(carouselStyles, /\.photo-carousel-axis\s*\{[^}]*min-height:\s*44px/);
});

test('photo lightbox reserves bounded image space and stays above fixed navigation', () => {
  assert.match(carouselStyles, /\.photo-lightbox__dialog\s*\{[^}]*height:\s*min\(64rem, calc\(100dvh/);
  assert.match(carouselStyles, /\.photo-lightbox__image-frame\s*\{[^}]*grid-template-rows:\s*minmax\(0, 1fr\)/);
  assert.match(carouselStyles, /\.photo-lightbox__footer\s*\{[^}]*max-height:\s*min\(42dvh, 24rem\);[^}]*overflow:\s*auto/);
  assert.doesNotMatch(carouselStyles, /height:\s*min\((?:58|66)vh/);
  assert.match(read('src/styles/_layout.scss'), /\.app-layout-main:has\(\.photo-lightbox\)\s*\{[^}]*z-index:\s*2000/);
});

test('media surfaces are flat and responsive without decorative filters', () => {
  const styles = [carouselStyles, parkImagesStyles, itemImagesStyles, videoListStyles, videoWatchStyles].join('\n');
  assert.match(carouselStyles, /\.photo-carousel-panel[\s\S]*?border-radius:\s*0[\s\S]*?box-shadow:\s*none/);
  assert.match(parkImagesStyles, /\.park-subpage-hero[\s\S]*?@include m\.public-editorial-header/);
  assert.match(itemImagesStyles, /\.park-subpage-hero[\s\S]*?@include m\.public-editorial-header/);
  assert.match(videoListStyles, /\.public-video-list__hero[\s\S]*?@include m\.public-editorial-header/);
  assert.match(videoWatchStyles, /\.public-video-watch__header[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /backdrop-filter\s*:/);
  assert.doesNotMatch(styles, /!important/);
});
