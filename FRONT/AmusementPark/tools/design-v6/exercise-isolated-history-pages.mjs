import { historyLayoutDocuments } from './public-history-layout-fixtures.mjs';

// Synthetic historical capabilities in the denied-network CI browser only.
// Real Fleury capabilities remain unavailable; no history is published.
export async function exerciseIsolatedHistoryPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language }) {
  const paths = historyLayoutDocuments(language);
  const filled = fixture.kind === 'standard';
  await navigateDocument(paths[0]);
  await waitUntil(`!!document.querySelector('.park-history-hero') && document.querySelectorAll('.canonical-history-event').length === ${filled ? 25 : 0} && !document.querySelector('.app-public-nav__profile')`, 'isolated public history timeline uses its exact local first page without an authenticated session');
  await captureLayouts('.park-history-hero', 'isolated-history-timeline');
  if (filled) {
    await evaluate(`[...document.querySelectorAll('.park-history-pagination a')].find(link => link.getAttribute('href').endsWith('/page/2')).click()`);
    await waitUntil(`location.pathname === ${JSON.stringify(paths[1])} && document.querySelectorAll('.canonical-history-event').length === 1`, 'historical timeline next link retains page two and its exact server-shaped final event');
    await captureLayouts('.park-history-hero', 'isolated-history-page-two');
  }
  await navigateDocument(paths[2]);
  await waitUntil(`Number(document.getElementById('snapshot-year')?.value) === 2000 && !!document.querySelector('.snapshot-overview')`, 'isolated snapshot retains the real year resolver and evidence coverage fields');
  await captureLayouts('.park-history-hero', 'isolated-history-snapshot');
  await captureLayouts('.snapshot-overview', 'isolated-history-coverage');
  await evaluate(`(() => { const month = document.querySelector('select[name="snapshotMonth"]'); month.value = [...month.options].find(option => option.value.endsWith(': 1')).value; month.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await waitUntil(`!!document.querySelector('select[name="snapshotDay"]')`, 'snapshot month exposes its existing day selector');
  await evaluate(`(() => { const day = document.querySelector('select[name="snapshotDay"]'); day.value = [...day.options].find(option => option.value.endsWith(': 1')).value; day.dispatchEvent(new Event('change', { bubbles: true })); document.querySelector('.snapshot-date-picker').requestSubmit(); })()`);
  await waitUntil(`location.pathname + location.search === ${JSON.stringify(paths[3])} && !!document.querySelector('.snapshot-overview')`, 'snapshot month/day selection preserves the exact URL and local API query');
  await captureLayouts('.park-history-hero', 'isolated-history-snapshot-day');
  await navigateDocument(paths[4]);
  await waitUntil(`!!document.querySelector('.comparison-summary') && Number(document.getElementById('comparison-from')?.value) === 2000 && Number(document.getElementById('comparison-to')?.value) === 2002`, 'isolated comparison retains its real year-range resolver and summary');
  await captureLayouts('.history-comparison-hero', 'isolated-history-comparison');
  await evaluate(`(() => { const from = document.getElementById('comparison-from'); from.value = '2002'; from.dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`!!document.querySelector('.comparison-picker__error') && document.querySelector('.comparison-picker button[type="submit"]').disabled`, 'invalid equal historical years retain visible validation and disabled submission');
  await evaluate(`(() => { const from = document.getElementById('comparison-from'); from.value = '2001'; from.dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`!document.querySelector('.comparison-picker__error') && !document.querySelector('.comparison-picker button[type="submit"]').disabled`, 'a valid historical range restores the existing read-only navigation control');
  await evaluate(`document.querySelector('.comparison-picker').requestSubmit()`);
  await waitUntil(`location.pathname === ${JSON.stringify(paths[5])} && Number(document.getElementById('comparison-from')?.value) === 2001 && !!document.querySelector('.comparison-summary')`, 'historical comparison navigates with the exact selected range and local response');
  if (filled) {
    await evaluate(`document.querySelector('.comparison-stable summary').click()`);
    await waitUntil(`document.querySelector('.comparison-stable').open && document.querySelectorAll('.comparison-stable li').length === 1`, 'stable historical subject disclosure exposes the exact unchanged subject');
    await captureLayouts('.comparison-stable', 'isolated-history-stable-subject');
  }
  await navigateDocument(paths[6]);
  await waitUntil(`!!document.querySelector('.historical-lineage-page h1') && document.querySelectorAll('.historical-lineage-card').length === ${filled ? 1 : 0}`, 'isolated lineage retains its root, relation direction and full or empty relation list');
  await captureLayouts('.historical-lineage-hero', 'isolated-history-lineage');
  if (filled) {
    await evaluate(`document.querySelector('.historical-lineage-sources summary').click()`);
    await waitUntil(`document.querySelector('.historical-lineage-sources').open && document.querySelectorAll('.historical-lineage-sources li').length === 1`, 'lineage source disclosure retains its exact local citation without external navigation');
    await captureLayouts('.historical-lineage-card', 'isolated-history-lineage-source');
  }
  await navigateDocument(`/${language}/profile`);
  await waitUntil(`!!document.querySelector('.profile-hero')`, 'isolated historical review restores the original local fixture account');
  result.isolatedHistory = { syntheticCapabilitiesOnly: true, publicFleuryCapabilitiesNotReclassified: true,
    filled, historyPublished: false, serverSeoAndHttpNotExercised: true };
}
