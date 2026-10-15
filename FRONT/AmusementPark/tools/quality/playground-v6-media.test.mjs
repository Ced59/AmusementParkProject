import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

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

test('photo galleries preserve filters, lazy thumbnails, credits, maps and lightbox controls', () => {
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
