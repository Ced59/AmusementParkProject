// Anonymous, existing controls only. External video playback stays blocked by
// the caller's sandbox; this recipe never claims to validate the provider.
export async function exercisePublicMediaHistory({ evaluate, waitUntil, capturePage, result }) {
  const parent = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key, index: navigation.currentEntry.index })`);
  const parkId = parent.path.match(/\/park\/([0-9a-f-]{36})(?:\/|$)/i)?.[1];
  if (!parkId) throw new Error('Media/history acceptance requires the actual parent park.');
  const openParkLink = async suffix => {
    const selector = `app-park-detail-page a[href="${parent.path}/${suffix}"]`;
    await waitUntil(`!!document.querySelector(${JSON.stringify(selector)})`, `actual park ${suffix} link is available`);
    await evaluate(`document.querySelector(${JSON.stringify(selector)}).click()`);
  };
  const restoreParent = async () => {
    await evaluate(`history.go(${parent.index} - navigation.currentEntry.index)`);
    await waitUntil(`location.pathname === ${JSON.stringify(parent.path)} && navigation.currentEntry.key === ${JSON.stringify(parent.key)} && !!document.querySelector('app-park-detail-page h1')`, 'media/history restores the exact park history entry');
  };

  let cursor = result.apiReads.length;
  await openParkLink('videos');
  await waitUntil(`!!document.querySelector('.public-video-list h1') && document.querySelectorAll('.public-video-list__tab').length > 0`, 'actual public video list and tabs are ready');
  const verifyVideos = async (start, filters, label) => {
    const kind = await evaluate(`document.querySelector('.public-video-list__tab[aria-selected="true"]').id.endsWith('-items') ? 'items' : 'park'`);
    let read;
    await waitUntil(() => {
      read = result.apiReads.slice(start).findLast(candidate => {
        const query = new URLSearchParams(candidate.query);
        return candidate.videos?.kind === kind && candidate.videos.pagination.currentPage === 1
          && candidate.videos.pagination.itemsPerPage === 24
          && (kind === 'items' ? candidate.path === `/api/videos/parks/${parkId}/park-items` : query.get('ownerId') === parkId)
          && Object.entries(filters).every(([key, value]) => query.get(key) === value);
      });
      return Boolean(read);
    }, `${label}: real public video page`);
    await waitUntil(`JSON.stringify([...document.querySelectorAll('.public-video-card')].map(link => link.getAttribute('href').match(/\/videos\/([^/]+)/)?.[1])) === ${JSON.stringify(JSON.stringify(read.videos.ids))}
      && (${read.videos.ids.length > 0} ? !document.querySelector('.public-video-list__empty') : !!document.querySelector('.public-video-list__empty'))`, `${label}: public API identifiers match full-detail links or the actual empty state`);
    (result.videoScenarios ??= []).push({ label, path: read.path, query: read.query, ...read.videos });
    return read.videos;
  };
  const original = await verifyVideos(cursor, { type: null, tagId: null, creatorName: null }, 'initial video list');
  if (!original.ids.length) throw new Error('The real video list has no filled sample; this scenario is not covered.');
  await capturePage('video-list-filled', '.public-video-list__filters');
  const originalTab = await evaluate(`document.querySelector('.public-video-list__tab[aria-selected="true"]').id`);
  const otherTabs = await evaluate(`Array.from(document.querySelectorAll('.public-video-list__tab'), node => node.id).filter(id => id !== ${JSON.stringify(originalTab)})`);
  for (const id of otherTabs) {
    await evaluate(`document.getElementById(${JSON.stringify(id)}).click()`);
    await waitUntil(`document.getElementById(${JSON.stringify(id)}).getAttribute('aria-selected') === 'true'`, 'video tab switches through its existing handler');
    await verifyVideos(0, { type: null, tagId: null, creatorName: null }, 'other video tab');
    await evaluate(`document.getElementById(${JSON.stringify(originalTab)}).click()`);
    await verifyVideos(0, { type: null, tagId: null, creatorName: null }, 'original video tab restored');
  }
  cursor = result.apiReads.length;
  const selectedType = await evaluate(`(() => { const select = document.querySelector('.public-video-list__filter-field select'); const option = [...select.options].find(option => option.value); if (!option) throw new Error('Missing video type choices'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); return option.value; })()`);
  await waitUntil(`new URLSearchParams(location.search).get('type') === ${JSON.stringify(selectedType)}`, 'video type retains the existing query parameter');
  await verifyVideos(cursor, { type: selectedType, tagId: null, creatorName: null }, 'video type filter');
  cursor = result.apiReads.length;
  await evaluate(`document.querySelector('.public-video-list__filter-actions button[type="button"]').click()`);
  await verifyVideos(cursor, { type: null, tagId: null, creatorName: null }, 'video filters reset');
  cursor = result.apiReads.length;
  const missingCreator = 'APF-V6-READONLY-NO-CREATOR-792cb17a';
  await evaluate(`(() => { const input = document.querySelector('.public-video-list input[type="search"]'); input.value = ${JSON.stringify(missingCreator)}; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.public-video-list__filter-actions button[type="submit"]').click(); })()`);
  const empty = await verifyVideos(cursor, { type: null, tagId: null, creatorName: missingCreator }, 'video creator empty result');
  if (empty.ids.length) throw new Error('The nonexistent creator unexpectedly matches published content.');
  await capturePage('video-list-empty-filter', '.public-video-list__filters');
  cursor = result.apiReads.length;
  await evaluate(`document.querySelector('.public-video-list__filter-actions button[type="button"]').click()`);
  await verifyVideos(cursor, { type: null, tagId: null, creatorName: null }, 'video creator filter cleared');
  const video = await evaluate(`(() => { const link = document.querySelector('.public-video-card'); return { path: link.getAttribute('href'), title: link.querySelector('.public-video-card__title').textContent.trim() }; })()`);
  await evaluate(`document.querySelector('.public-video-card').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(video.path)} && document.querySelector('.public-video-watch h1')?.textContent.trim() === ${JSON.stringify(video.title)} && !!document.querySelector('.public-video-watch__player')`, 'real video opens its full watch page with matching title');
  const player = await evaluate(`(() => { const iframe = document.querySelector('.public-video-watch__player iframe'); return { embedded: !!iframe, named: !!iframe?.title, externalAction: !!document.querySelector('.public-video-watch__external a'), externalPlaybackNotValidated: true }; })()`);
  if (player.embedded ? !player.named : !player.externalAction) throw new Error('The watch page lost its named player or external fallback.');
  await capturePage('video-watch', '.public-video-watch__header');
  result.videoWatch = { ...video, ...player };
  await restoreParent();

  await openParkLink('history');
  await waitUntil(`document.querySelectorAll('.canonical-history-event').length > 0 && !!document.querySelector('.history-narrative-link')`, 'real park timeline exposes its published narrative');
  const timeline = await evaluate(`({ key: navigation.currentEntry.key, events: document.querySelectorAll('.canonical-history-event').length, snapshotsAvailable: !!document.querySelector('.history-comparison-callout'), paginationAvailable: !!document.querySelector('.park-history-pagination') })`);
  const disclosure = '.canonical-history-event .history-sources';
  await evaluate(`document.querySelector('${disclosure} summary').click()`);
  await waitUntil(`document.querySelector('${disclosure}').open && document.querySelectorAll('${disclosure} li').length > 0`, 'timeline source disclosure exposes its real references');
  await capturePage('history-sources-open', '.canonical-history-timeline');
  await evaluate(`document.querySelector('${disclosure} summary').click()`);
  await waitUntil(`!document.querySelector('${disclosure}').open`, 'timeline source disclosure closes natively');
  const articlePath = await evaluate(`document.querySelector('.history-narrative-link').getAttribute('href')`);
  await evaluate(`document.querySelector('.history-narrative-link').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(articlePath)} && !!document.querySelector('.history-article h1') && document.querySelectorAll('.history-article__content p').length > 0`, 'published narrative opens its actual complete article route');
  const article = await evaluate(`({ title: document.querySelector('.history-article h1').textContent.trim(), paragraphs: document.querySelectorAll('.history-article__content p').length, headings: document.querySelectorAll('.history-article__content h2, .history-article__content h3').length, sources: document.querySelectorAll('.history-article__sources li').length })`);
  await capturePage('history-article-header', '.history-article__hero');
  await capturePage('history-article-body', '.history-article__content');
  await evaluate('history.back()');
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(timeline.key)} && document.querySelectorAll('.canonical-history-event').length === ${timeline.events}`, 'article back restores the exact timeline and event count');
  result.historyScenarios = { timeline, article: { path: articlePath, ...article },
    snapshotsAndComparisonNotExercised: true, serverPaginationNotExercised: true };
  await restoreParent();
}
