// Read-only browser recipe; all actions go through real Angular controls and
// browser history. The application never imports this tool.
export async function exercisePublicParkItems({ evaluate, waitUntil, result }) {
  const original = await evaluate(`({ url: location.pathname + location.search, index: navigation.currentEntry.index })`);
  const originalQuery = new URL(original.url, 'https://apf-review.invalid').searchParams;
  const verify = async (cursor, expected, label) => {
    let read;
    await waitUntil(() => {
      read = result.apiReads.slice(cursor).find(candidate => candidate.parkItems
        && Object.entries(expected).every(([key, value]) => new URLSearchParams(candidate.query).get(key) === value));
      return Boolean(read);
    }, `${label}: real public item-list request`);
    const actual = read.parkItems;
    if (actual.pagination.currentPage !== Number(expected.page) || actual.pagination.itemsPerPage !== Number(expected.size)) {
      throw new Error(`Item API did not honor the requested page and size: ${label}`);
    }
    await waitUntil(`!!document.querySelector('.park-items-page')
      && JSON.stringify([...document.querySelectorAll('.items-grid .ui-item-card__title')].map(node => node.textContent.trim())) === ${JSON.stringify(JSON.stringify(actual.names))}
      && (${actual.pagination.totalItems > actual.pagination.itemsPerPage}
        ? document.querySelector('.park-items-page .p-paginator-page.p-highlight')?.textContent.trim() === '${actual.pagination.currentPage}'
        : !document.querySelector('.park-items-page .app-pagination'))`, `${label}: API names and page match the item cards`);
    (result.parkItemScenarios ??= []).push({ label, path: read.path, query: read.query, ...actual });
    return actual;
  };
  let cursor = result.apiReads.length;
  await evaluate(`[...document.querySelectorAll('.park-items-page .p-paginator-page')].find(button => button.textContent.trim() === '2').click()`);
  await verify(cursor, { page: '2', size: '12' }, 'item inventory page two');
  await waitUntil(`new URLSearchParams(location.search).get('page') === '2'`, 'item page two preserves its real query parameter');
  cursor = result.apiReads.length;
  await evaluate('history.back()');
  await verify(cursor, { page: '1', size: '12' }, 'item inventory browser back');
  cursor = result.apiReads.length;
  await evaluate('history.forward()');
  await verify(cursor, { page: '2', size: '12' }, 'item inventory browser forward');
  cursor = result.apiReads.length;
  await evaluate('history.back()');
  await verify(cursor, { page: '1', size: '12' }, 'item inventory first page restored');
  for (const rows of [6, 24, 12, 48]) {
    cursor = result.apiReads.length;
    await evaluate(`(() => { const select = document.querySelector('.park-items-page .p-paginator-rpp-options'); const option = [...select.options].find(option => option.textContent.trim() === '${rows}'); if (!option) throw new Error('Missing item page-size option'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await verify(cursor, { page: '1', size: String(rows) }, `item inventory page size ${rows}`);
  }
  cursor = result.apiReads.length;
  await evaluate('history.back()');
  await verify(cursor, { page: '1', size: '12' }, 'item inventory page size restored through history');
  cursor = result.apiReads.length;
  await evaluate(`(() => { const select = document.querySelector('#search-panel-category'); const option = [...select.options].find(option => option.value.endsWith('Restaurant')); if (!option) throw new Error('Missing Restaurant category'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await verify(cursor, { page: '1', size: '12', category: 'Restaurant' }, 'item inventory restaurant category');
  cursor = result.apiReads.length;
  await evaluate(`(() => { const select = document.querySelector('#search-panel-category'); select.selectedIndex = 0; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await verify(cursor, { page: '1', size: '12', category: null }, 'item inventory category reset');
  cursor = result.apiReads.length;
  await evaluate(`(() => { const select = document.querySelector('#search-panel-closed'); const option = [...select.options].find(option => option.value.endsWith('all')); if (!option) throw new Error('Missing open-and-closed filter'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
  await verify(cursor, { page: '1', size: '12', closedFilter: 'all' }, 'item inventory includes visible closed items');
  for (const query of ['zzv6qaf0fe25ac1be14b5cba302e46356ba3bb', 'Rhaegal']) {
    cursor = result.apiReads.length;
    await evaluate(`(() => { const input = document.querySelector('#park-items-search-input'); input.value = ${JSON.stringify(query)}; input.dispatchEvent(new Event('input', { bubbles: true })); })()`);
    const items = await verify(cursor, { page: '1', size: '12', closedFilter: 'all', search: query }, `item inventory search ${query === 'Rhaegal' ? 'Rhaegal' : 'empty'}`);
    if (query !== 'Rhaegal') {
      if (items.names.length) throw new Error('The no-result item fixture unexpectedly matched public content.');
      await waitUntil(`!!document.querySelector('.park-items-page .compact-empty-state')`, 'item inventory renders its real empty filtered state');
    }
  }
  const detail = await evaluate(`(() => { const card = document.querySelector('.items-grid .ui-item-card'); return { path: card.querySelector('.ui-item-card__footer a').getAttribute('href'), name: card.querySelector('.ui-item-card__title').textContent.trim() }; })()`);
  await evaluate(`document.querySelector('.items-grid .ui-item-card__footer a').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(detail.path)} && !!document.querySelector('h1')?.textContent.includes(${JSON.stringify(detail.name)}) && !document.querySelector('.park-items-page') && !document.querySelector('.p-dialog')`, 'real item card opens its full detail page, never a popup');
  cursor = result.apiReads.length;
  await evaluate('history.back()');
  await verify(cursor, { page: '1', size: '12', closedFilter: 'all', search: 'Rhaegal' }, 'item detail back restores filtered inventory');
  const restoreDelta = await evaluate(`${original.index} - navigation.currentEntry.index`);
  if (!Number.isInteger(restoreDelta) || restoreDelta >= 0) throw new Error('The item recipe cannot restore its exact original history entry.');
  cursor = result.apiReads.length;
  await evaluate(`history.go(${restoreDelta})`);
  await verify(cursor, { page: String(originalQuery.get('page') ?? 1), size: String(originalQuery.get('size') ?? 12), search: originalQuery.get('search'), closedFilter: originalQuery.get('closed') }, 'item inventory original history entry restored');
  await waitUntil(`location.pathname + location.search === ${JSON.stringify(original.url)} && navigation.currentEntry.index === ${original.index}`, 'item recipe preserves the original park navigation history');
}
