// Only real Angular controls and browser history; no API writes or demo media.
import { publicParkGalleryPages } from './public-photo-evidence.mjs';

export async function exercisePublicPhotos({ evaluate, waitUntil, pressTab, result }) {
  const parent = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key })`);
  const parkId = parent.path.match(/\/park\/([0-9a-f-]{36})(?:\/|$)/i)?.[1];
  if (!parkId) throw new Error('The photo recipe requires its actual public park parent.');
  let cursor = result.apiReads.length;
  await evaluate(`document.querySelector('.park-banner__actions a[href$="/images"]').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`${parent.path}/images`)} && !!document.querySelector('.photo-carousel__stage img') && document.querySelectorAll('.photo-carousel-strip__item').length > 1`, 'real park gallery opens with published photos');
  await waitUntil(() => publicParkGalleryPages(result.apiReads.slice(cursor), parkId).length === 2, 'park gallery reads its real photo and logo pages');
  const parkReads = publicParkGalleryPages(result.apiReads.slice(cursor), parkId);
  if (parkReads.some(read => read.photos.ids.length !== read.photos.pagination.totalItems)) throw new Error('This park gallery recipe requires both complete bounded server pages.');
  const parkIds = new Set(parkReads.flatMap(read => read.photos.ids));
  const verifyLoaded = async (ids, label) => {
    await waitUntil(`Number(document.querySelector('.photo-carousel-years button:first-child strong')?.textContent) === ${ids.size}
      && document.querySelectorAll('.photo-carousel-strip__item').length === ${ids.size > 1 ? ids.size : 0}`, label);
    const renderedIds = await evaluate(`Array.from(document.querySelectorAll('.photo-carousel-strip__item img'), image => image.getAttribute('src')?.match(/[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}|[0-9a-f]{32}/i)?.[0]).filter(Boolean)`);
    if (renderedIds.some(id => !ids.has(id))) throw new Error('The gallery rendered an image outside its exact public API population.');
    (result.photoScenarios ??= []).push({ label, count: ids.size, renderedImageCount: renderedIds.length });
  };
  await verifyLoaded(parkIds, 'park gallery count and thumbnails match the public response population');
  const initial = await evaluate(`document.querySelector('.photo-carousel__stage img').getAttribute('src')`);
  await evaluate(`document.querySelector('.photo-carousel__nav--next').click()`);
  await waitUntil(`!!document.querySelector('.photo-carousel__stage img') && document.querySelector('.photo-carousel__stage img').getAttribute('src') !== ${JSON.stringify(initial)}`, 'real photo next control changes the active image');
  await evaluate(`document.querySelector('.photo-carousel__nav--prev').click()`);
  await waitUntil(`document.querySelector('.photo-carousel__stage img')?.getAttribute('src') === ${JSON.stringify(initial)}`, 'real photo previous control restores the active image');
  const year = await evaluate(`(() => { const button = document.querySelectorAll('.photo-carousel-years button')[1]; return { label: button.querySelector('span').textContent.trim(), count: Number(button.querySelector('strong').textContent) }; })()`);
  await evaluate(`document.querySelectorAll('.photo-carousel-years button')[1].click()`);
  await waitUntil(`document.querySelectorAll('.photo-carousel-strip__item').length === ${year.count > 1 ? year.count : 0}
    && document.querySelector('.photo-carousel-years .photo-carousel-axis--active span')?.textContent.trim() === ${JSON.stringify(year.label)}`, 'photo year filter preserves its actual dynamic count');
  await evaluate(`document.querySelector('.photo-carousel-years button:first-child').click()`);
  await verifyLoaded(parkIds, 'photo year reset restores the exact public population');
  const tag = await evaluate(`(() => { const button = document.querySelectorAll('.photo-carousel-tags button')[1]; return { label: button.querySelector('span').textContent.trim(), count: Number(button.querySelector('strong').textContent) }; })()`);
  await evaluate(`document.querySelectorAll('.photo-carousel-tags button')[1].click()`);
  await waitUntil(`document.querySelectorAll('.photo-carousel-strip__item').length === ${tag.count > 1 ? tag.count : 0}
    && document.querySelector('.photo-carousel-tags .photo-carousel-axis--active span')?.textContent.trim() === ${JSON.stringify(tag.label)}`, 'photo tag filter preserves its actual dynamic count');
  await evaluate(`document.querySelector('.photo-carousel-tags button').click()`);
  await verifyLoaded(parkIds, 'photo tag reset restores the exact public population');
  await evaluate(`(() => { const stage = document.querySelector('.photo-carousel__stage'); stage.focus(); stage.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })); })()`);
  await waitUntil(`!!document.querySelector('.photo-lightbox[role="dialog"][aria-modal="true"] img.photo-lightbox__image')`, 'photo lightbox opens through the existing keyboard handler');
  await waitUntil(`document.activeElement === document.querySelector('.photo-lightbox__close')`, 'photo lightbox captures keyboard focus on its close control');
  result.photoFocusBeforeTab = await evaluate(`(() => { const root = document.querySelector('.photo-lightbox'); return {
    active: { tag: document.activeElement.tagName, classes: document.activeElement.className },
    anchors: Array.from(root.querySelectorAll('.cdk-focus-trap-anchor'), anchor => ({ tabIndex: anchor.tabIndex, display: getComputedStyle(anchor).display, visibility: getComputedStyle(anchor).visibility })),
    buttons: Array.from(root.querySelectorAll('button'), button => ({ classes: button.className, disabled: button.disabled, tabIndex: button.tabIndex })) }; })()`);
  // Real Chromium key input tests native traversal, not a synthetic focus call.
  await pressTab(true);
  await waitUntil(`document.activeElement === document.querySelector('.photo-lightbox__thumb:last-child')`, 'photo lightbox Shift+Tab loops to its last real thumbnail');
  await pressTab(false);
  await waitUntil(`document.activeElement === document.querySelector('.photo-lightbox__close')`, 'photo lightbox Tab loops back to its first close control');
  await evaluate(`document.querySelector('.photo-lightbox__close').click()`);
  await waitUntil(`!document.querySelector('.photo-lightbox')`, 'photo lightbox closes without navigation or mutation');
  await waitUntil(`document.activeElement === document.querySelector('.photo-carousel__stage')`, 'photo lightbox restores keyboard focus to its original stage');
  await evaluate(`document.querySelector('.photo-carousel__stage').dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))`);
  await waitUntil(`!!document.querySelector('.photo-lightbox__dialog')`, 'photo lightbox reopens for its backdrop recipe');
  await evaluate(`document.querySelector('.photo-lightbox__header h2').click()`);
  await waitUntil(`!!document.querySelector('.photo-lightbox__dialog')`, 'photo dialog content click never closes the modal');
  await evaluate(`document.querySelector('.photo-lightbox__backdrop').click()`);
  await waitUntil(`!document.querySelector('.photo-lightbox') && document.activeElement === document.querySelector('.photo-carousel__stage')`, 'named native backdrop retains outside-click close and restores focus');
  await evaluate(`document.querySelector('.photo-carousel__stage').dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))`);
  await waitUntil(`document.activeElement === document.querySelector('.photo-lightbox__close')`, 'photo lightbox captures focus again before Escape');
  await evaluate(`document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))`);
  await waitUntil(`!document.querySelector('.photo-lightbox') && document.activeElement === document.querySelector('.photo-carousel__stage')`, 'photo lightbox Escape preserves its existing close and restores focus');
  cursor = result.apiReads.length;
  await evaluate(`document.querySelector('#park-images-tab-items').click()`);
  await waitUntil(() => result.apiReads.slice(cursor).some(read => read.photos?.kind === 'items' && read.photos.pagination.itemsPerPage === 100), 'item photo tab requests its real server page size');
  const first = result.apiReads.slice(cursor).find(read => read.photos?.kind === 'items' && read.photos.pagination.itemsPerPage === 100);
  const itemIds = new Set(first.photos.ids);
  await verifyLoaded(itemIds, 'item gallery first page matches published visible-item media');
  if (first.photos.pagination.totalItems <= 100 || first.photos.pagination.totalItems > 500) throw new Error('This large-gallery recipe requires a bounded real public population of 101–500 photos.');
  for (let page = 2; itemIds.size < first.photos.pagination.totalItems && page <= 5; page++) {
    cursor = result.apiReads.length;
    await evaluate(`document.querySelector('.park-images-page__load-more button').click()`);
    await waitUntil(() => result.apiReads.slice(cursor).some(read => read.photos?.kind === 'items' && read.photos.pagination.currentPage === page), `item gallery loads real server page ${page}`);
    const read = result.apiReads.slice(cursor).find(read => read.photos?.kind === 'items' && read.photos.pagination.currentPage === page);
    if (read.photos.pagination.itemsPerPage !== 100 || read.photos.pagination.totalItems !== first.photos.pagination.totalItems) throw new Error('The public gallery pagination contract or population changed during its recipe.');
    read.photos.ids.forEach(id => itemIds.add(id));
    await verifyLoaded(itemIds, `item gallery cumulative page ${page} matches its actual API population`);
  }
  await waitUntil(`!document.querySelector('.park-images-page__load-more')`, 'large item gallery ends load-more only after all server pages');
  await evaluate(`document.querySelector('#park-images-tab-park').click()`);
  await verifyLoaded(parkIds, 'park photo tab restores its independent cached population');
  await evaluate('history.back()');
  await waitUntil(`location.pathname === ${JSON.stringify(parent.path)} && navigation.currentEntry.key === ${JSON.stringify(parent.key)} && !!document.querySelector('app-park-detail-page h1')`, 'gallery browser back restores the original full park entry');
}
