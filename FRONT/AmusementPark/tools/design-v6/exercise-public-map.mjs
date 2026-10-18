// Existing Leaflet DOM controls only. No synthetic coordinates or private map
// fields; external tiles/directions remain outside this read-only CI sandbox.
import { exerciseMapGestures } from './browser-map-gestures.mjs';
export function mapViewportSettled(samples) {
  const recent = samples.slice(-3);
  return recent.length === 3 && recent.every(sample => sample.transform
    && !sample.animating && sample.transform === recent[0].transform
    && sample.paneTransform === recent[0].paneTransform);
}

export async function exercisePublicMap({ cdp, evaluate, waitUntil, capturePage, result }) {
  const parent = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key, index: navigation.currentEntry.index })`);
  const selector = `app-park-detail-page a[href="${parent.path}/map"]`;
  await waitUntil(`!!document.querySelector(${JSON.stringify(selector)})`, 'park exposes its actual map link');
  await evaluate(`document.querySelector(${JSON.stringify(selector)}).click()`);
  await waitUntil(`!!document.querySelector('.park-map-page h1') && !!document.querySelector('.park-items-map .leaflet-marker-icon')`, 'real park Leaflet map renders its published markers');
  await evaluate(`document.querySelector('.park-items-map').scrollIntoView({ block: 'start', behavior: 'instant' })`);
  await waitUntil(`!document.querySelector('.park-items-map .leaflet-zoom-anim')`, 'initial Leaflet bounds finish before the separate zoom recipe');
  const mapKey = await evaluate('navigation.currentEntry.key');
  const initial = await evaluate(`({ markers: document.querySelectorAll('.park-items-map .leaflet-marker-icon').length, officialTabAvailable: !!document.getElementById('park-map-tab-official') })`);
  const proxy = '.park-items-map .leaflet-proxy';
  result.mapScenarios = { initial, viewportSamples: [], zoomObservations: [], externalTilesAndDirectionsNotValidated: true, pointerDragNotExercised: true };
  const settleViewport = async (samples, label) => {
    await waitUntil(async () => {
      const sample = await evaluate(`({ transform: document.querySelector('${proxy}')?.style.transform ?? '',
        paneTransform: document.querySelector('.park-items-map .leaflet-map-pane')?.style.transform ?? '',
        animating: !!document.querySelector('.park-items-map .leaflet-zoom-anim, .park-items-map .leaflet-pan-anim') })`);
      samples.push(sample);
      if (samples.length > 12) samples.shift();
      return mapViewportSettled(samples);
    }, label);
  };
  await settleViewport(result.mapScenarios.viewportSamples, 'initial Leaflet viewport is stable across consecutive DOM observations');
  const beforeZoom = await evaluate(`document.querySelector('${proxy}')?.style.transform ?? ''`);
  result.mapScenarios.beforeZoom = beforeZoom;
  if (!beforeZoom) throw new Error('The real Leaflet zoom transform is unavailable; zoom acceptance is not covered.');
  await evaluate(`document.querySelector('.park-items-map .leaflet-control-zoom-in').click()`);
  await waitUntil(async () => {
    const sample = await evaluate(`({ transform: document.querySelector('${proxy}')?.style.transform ?? '', animating: !!document.querySelector('.park-items-map .leaflet-zoom-anim'), disabled: document.querySelector('.park-items-map .leaflet-control-zoom-in')?.getAttribute('aria-disabled') })`);
    if (result.mapScenarios.zoomObservations.length < 12) result.mapScenarios.zoomObservations.push(sample);
    return sample.transform !== beforeZoom && !sample.animating;
  }, 'native zoom-in changes the real map transform and finishes');
  await evaluate(`document.querySelector('.park-items-map .leaflet-control-zoom-out').click()`);
  await waitUntil(`document.querySelector('${proxy}')?.style.transform === ${JSON.stringify(beforeZoom)} && !document.querySelector('.park-items-map .leaflet-zoom-anim')`, 'native zoom-out restores the original map transform');
  await exerciseMapGestures({ cdp, evaluate, waitUntil, result });
  const category = await evaluate(`(() => { const buttons = [...document.querySelectorAll('.park-items-map__category-button')]; const total = Number(buttons[0]?.querySelector('strong')?.textContent.trim()); const button = buttons.slice(1).find(node => { const count = Number(node.querySelector('strong').textContent.trim()); return count > 0 && count < total; }); if (!button) return null; const index = buttons.indexOf(button); return { index, count: Number(button.querySelector('strong').textContent.trim()), total }; })()`);
  if (!category) throw new Error('No distinct nonempty public map category; this filter scenario is not covered.');
  await evaluate(`document.querySelectorAll('.park-items-map__category-button')[${category.index}].click()`);
  await waitUntil(`document.querySelectorAll('.park-items-map__category-button')[${category.index}].classList.contains('park-items-map__category-button--active') && document.querySelectorAll('.park-items-map .leaflet-marker-icon').length === ${category.count}`, 'map category filter matches its actual published count');
  await capturePage('map-category-selected', '.park-items-map');
  await evaluate(`document.querySelector('.park-items-map__category-button').click()`);
  await waitUntil(`document.querySelector('.park-items-map__category-button').classList.contains('park-items-map__category-button--active') && document.querySelectorAll('.park-items-map .leaflet-marker-icon').length === ${category.total}`, 'all map categories restore their actual markers');
  result.mapScenarios.resetViewportSamples = [];
  await settleViewport(result.mapScenarios.resetViewportSamples, 'category reset finishes native bounds and pan before opening the normal popup');
  await evaluate(`document.querySelector('.park-items-map .leaflet-marker-icon').click()`);
  await waitUntil(`!!document.querySelector('.leaflet-popup .leaflet-map-popup__action--detail')`, 'real marker opens its detail popup');
  const detail = await evaluate(`({ path: document.querySelector('.leaflet-popup .leaflet-map-popup__action--detail').getAttribute('href'), directionsAvailable: !!document.querySelector('.leaflet-popup .leaflet-map-popup__action--directions') })`);
  result.mapScenarios.detail = detail;
  if (!detail.path.startsWith(`${parent.path}/item/`)) throw new Error('Map popup did not retain its real park/item parent.');
  result.mapScenarios.popupViewportSamples = [];
  await settleViewport(result.mapScenarios.popupViewportSamples, 'normal popup auto-pan finishes before responsive geometry capture');
  await capturePage('map-marker-popup', '.park-items-map');
  await evaluate(`document.querySelector('.leaflet-popup .leaflet-map-popup__action--detail').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(detail.path)} && !!document.querySelector('app-park-item-detail-page h1') && !document.querySelector('app-root')?.hasAttribute('ngh') && !document.querySelector('.p-dialog')`, 'map popup opens the full actual item page, never a modal');
  await evaluate('history.back()');
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(mapKey)} && !!document.querySelector('.park-map-page h1')`, 'map detail browser back restores the exact map entry');
  const official = { available: initial.officialTabAvailable, multiYearNotExercised: true };
  if (official.available) {
    await evaluate(`document.getElementById('park-map-tab-official').click()`);
    await waitUntil(`document.getElementById('park-map-tab-official').getAttribute('aria-selected') === 'true' && document.querySelectorAll('.official-map-card').length > 0`, 'official maps open through the existing tab');
    const years = await evaluate(`Array.from(document.querySelectorAll('.official-maps__year select option'), option => option.value)`);
    official.years = years;
    if (years.length > 1) {
      for (const year of [years[1], years[0]]) {
        await evaluate(`(() => { const select = document.querySelector('.official-maps__year select'); select.value = ${JSON.stringify(year)}; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await waitUntil(`Array.from(document.querySelectorAll('.official-map-card__meta > span:first-child'), node => node.textContent.trim()).every(value => value === ${JSON.stringify(year)}) && document.querySelectorAll('.official-map-card').length > 0`, 'official map year switches without inventing an edition');
      }
      official.multiYearNotExercised = false;
    }
    await capturePage('official-map-year', '.official-maps');
  }
  Object.assign(result.mapScenarios, { category, detail, official });
  await evaluate(`history.go(${parent.index} - navigation.currentEntry.index)`);
  await waitUntil(`location.pathname === ${JSON.stringify(parent.path)} && navigation.currentEntry.key === ${JSON.stringify(parent.key)} && !!document.querySelector('app-park-detail-page h1')`, 'map recipe restores the exact original park history entry');
}
