// Read-only history controls from the current public timeline. Missing published
// capabilities are recorded, never manufactured with fixture URLs or mock data.
export async function exercisePublicHistoryExplorer({ evaluate, waitUntil, capturePage, result }) {
  const timeline = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key, index: navigation.currentEntry.index,
    events: document.querySelectorAll('.canonical-history-event').length,
    next: document.querySelector('.park-history-pagination a:last-child')?.getAttribute('href') ?? null,
    snapshot: document.querySelector('.canonical-history-event__header a[href]')?.getAttribute('href') ?? null,
    comparison: document.querySelector('.history-comparison-callout')?.getAttribute('href') ?? null,
    lineage: document.querySelector('.history-lineage-link')?.getAttribute('href') ?? null })`);
  const evidence = result.historyExplorer = { capabilities: timeline, checks: [], unavailable: [] };
  const follow = async (path, selector, label) => {
    await evaluate(`(() => { const link = [...document.querySelectorAll('a[href]')].find(node => node.getAttribute('href') === ${JSON.stringify(path)}); if (!link) throw new Error('Published history link is missing'); link.click(); })()`);
    await waitUntil(`location.pathname === ${JSON.stringify(path)} && !!document.querySelector(${JSON.stringify(selector)})`, label);
    evidence.checks.push(label);
  };
  const restore = async () => {
    await evaluate(`history.go(${timeline.index} - navigation.currentEntry.index)`);
    await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(timeline.key)} && location.pathname === ${JSON.stringify(timeline.path)} && document.querySelectorAll('.canonical-history-event').length === ${timeline.events}`, 'history explorer restores the exact timeline entry');
  };
  if (timeline.next && timeline.next.includes('/page/')) {
    await follow(timeline.next, '.canonical-history-timeline', 'published timeline pagination opens its real next route');
    const page = await evaluate(`({ path: location.pathname, events: document.querySelectorAll('.canonical-history-event').length,
      summary: document.querySelector('.park-history-metrics article:nth-child(2) strong').textContent.trim() })`);
    if (Number(page.summary.split('/')[0].trim()) !== Number(timeline.next.split('/page/')[1])) throw new Error('Timeline page summary disagrees with its actual route.');
    evidence.pagination = page;
    await restore();
  } else evidence.unavailable.push('published next timeline page');

  if (timeline.snapshot) {
    await follow(timeline.snapshot, 'app-park-historical-snapshot-page #snapshot-year', 'published timeline year opens its complete historical snapshot');
    evidence.snapshot = await evaluate(`({ path: location.pathname, year: Number(document.getElementById('snapshot-year').value),
      coverageFields: document.querySelectorAll('.snapshot-overview article').length })`);
    if (!Number.isInteger(evidence.snapshot.year) || evidence.snapshot.coverageFields < 1) throw new Error('Historical snapshot identity or evidence coverage is missing.');
    await capturePage('history-snapshot', '.park-history-hero');
    const date = await evaluate(`(() => {
      const month = document.querySelector('select[name="snapshotMonth"]'); const option = [...month.options].find(option => option.value.endsWith(': 1'));
      if (!option) throw new Error('Published snapshot has no first month choice'); month.value = option.value; month.dispatchEvent(new Event('change', { bubbles: true }));
      return { year: Number(document.getElementById('snapshot-year').value), month: Number(option.value.split(':').at(-1).trim()) };
    })()`);
    await waitUntil(`!!document.querySelector('select[name="snapshotDay"]')`, 'snapshot month exposes its real day control');
    date.day = await evaluate(`(() => { const day = document.querySelector('select[name="snapshotDay"]'); const option = [...day.options].find(option => option.value.endsWith(': 1'));
      if (!option) throw new Error('Published snapshot has no first day choice'); day.value = option.value; day.dispatchEvent(new Event('change', { bubbles: true })); return Number(option.value.split(':').at(-1).trim()); })()`);
    await evaluate(`document.querySelector('.snapshot-date-picker').requestSubmit()`);
    await waitUntil(`location.pathname === ${JSON.stringify(evidence.snapshot.path)} && new URLSearchParams(location.search).get('month') === ${JSON.stringify(String(date.month))}
      && new URLSearchParams(location.search).get('day') === ${JSON.stringify(String(date.day))} && !!document.querySelector('.snapshot-overview')`, 'snapshot date selection preserves real month/day URL parameters');
    evidence.snapshot.selectedDate = date;
    await restore();
  } else evidence.unavailable.push('published historical snapshot link');

  if (timeline.comparison) {
    await follow(timeline.comparison, '.history-comparison-page #comparison-from', 'published comparison link opens its actual year range');
    const range = await evaluate(`({ from: Number(document.getElementById('comparison-from').value), to: Number(document.getElementById('comparison-to').value),
      counts: [...document.querySelectorAll('.comparison-summary strong')].map(node => Number(node.textContent.trim())) })`);
    if (!Number.isInteger(range.from) || !Number.isInteger(range.to) || range.from >= range.to || range.counts.length !== 4 || range.counts.some(count => !Number.isInteger(count) || count < 0)) throw new Error('Historical comparison lost its real range or summary.');
    evidence.comparison = range;
    await capturePage('history-comparison', '.history-comparison-hero');
    await evaluate(`(() => { const from = document.getElementById('comparison-from'); from.value = ${JSON.stringify(String(range.to))}; from.dispatchEvent(new Event('input', { bubbles: true })); })()`);
    await waitUntil(`!!document.querySelector('.comparison-picker__error') && document.querySelector('.comparison-picker button[type="submit"]').disabled`, 'invalid historical year range retains its validation and disabled submit');
    const newFrom = range.to - 1;
    await evaluate(`(() => { const from = document.getElementById('comparison-from'); from.value = ${JSON.stringify(String(newFrom))}; from.dispatchEvent(new Event('input', { bubbles: true })); })()`);
    await waitUntil(`!document.querySelector('.comparison-picker__error') && !document.querySelector('.comparison-picker button[type="submit"]').disabled`, 'valid historical year range restores its existing submit');
    await evaluate(`document.querySelector('.comparison-picker').requestSubmit()`);
    const expectedPath = `${timeline.comparison.split('/compare/')[0]}/compare/${newFrom}/${range.to}`;
    await waitUntil(`location.pathname === ${JSON.stringify(expectedPath)} && Number(document.getElementById('comparison-from')?.value) === ${newFrom} && !!document.querySelector('.comparison-summary')`, 'comparison form navigates with its actual selected year parameters');
    evidence.comparison.selectedPath = expectedPath;
    const stableCount = await evaluate(`document.querySelectorAll('.comparison-stable li').length`);
    if (stableCount) {
      await evaluate(`document.querySelector('.comparison-stable summary').click()`);
      await waitUntil(`document.querySelector('.comparison-stable').open && document.querySelectorAll('.comparison-stable li').length === ${stableCount}`, 'comparison disclosure exposes its existing stable subjects');
      await evaluate(`document.querySelector('.comparison-stable summary').click()`);
      await waitUntil(`!document.querySelector('.comparison-stable').open`, 'comparison stable disclosure closes natively');
      evidence.comparison.stableSubjects = stableCount;
    } else evidence.unavailable.push('stable subjects in selected historical range');
    await restore();
  } else evidence.unavailable.push('published historical comparison link');

  if (timeline.lineage) {
    await follow(timeline.lineage, '.historical-lineage-page h1', 'published subject link opens its complete lineage');
    evidence.lineage = await evaluate(`({ path: location.pathname, title: document.querySelector('.historical-lineage-page h1').textContent.trim(),
      summaryFields: document.querySelectorAll('.historical-lineage-summary strong').length })`);
    if (!evidence.lineage.title || evidence.lineage.summaryFields < 2) throw new Error('Published lineage has lost its identity or relation summary.');
    await capturePage('history-lineage', '.historical-lineage-hero');
    await restore();
  } else evidence.unavailable.push('published subject lineage link');
}
