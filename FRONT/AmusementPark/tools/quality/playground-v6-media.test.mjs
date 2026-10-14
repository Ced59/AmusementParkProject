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

test('media surfaces are flat and responsive without decorative filters', () => {
  const styles = [carouselStyles, parkImagesStyles, itemImagesStyles, videoListStyles, videoWatchStyles].join('\n');
  assert.match(carouselStyles, /\.photo-carousel-panel[\s\S]*?border-radius:\s*var\(--app-radius-md\)[\s\S]*?box-shadow:\s*none/);
  assert.match(parkImagesStyles, /\.park-subpage-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(itemImagesStyles, /\.park-subpage-hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(videoListStyles, /\.public-video-list__hero[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.match(videoWatchStyles, /\.public-video-watch__header[\s\S]*?border-top:\s*4px solid var\(--app-primary\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(styles, /backdrop-filter\s*:/);
  assert.doesNotMatch(styles, /!important/);
});
