import { accountComparisonFixtureToken } from './account-passport-statistics-fixtures.mjs';

// Private deep-link rendering only. No publication preview POST, sharing,
// comparison acceptance/revocation, export or saved account mutation.
export async function exerciseAccountPassportStatistics({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language }) {
  const data = fixture.passportStatisticsData;
  const prefix = `/${language}/profile/passport`;
  const filled = fixture.kind === 'standard';
  const pages = [
    ['/statistics', '.passport-global-statistics__hero', '.passport-global-statistics__filters'],
    ['/statistics/history', '.passport-history-stats__hero', filled ? '.passport-history-stats__cards' : '.passport-history-stats__state'],
    [`/items/${data.itemId}`, '.passport-statistics-page__hero', '.passport-statistics-page__heading'],
    [`/parks/${data.parkId}`, '.passport-statistics-page__hero', '.passport-statistics-page__heading'],
    ['/years/2026', '.passport-statistics-page__hero', '.passport-statistics-page__heading'],
    ['/share', '.passport-share__hero', '.passport-share__editor'],
    [`/comparisons/invitations/${accountComparisonFixtureToken}`, '.comparison-consent__hero', '.comparison-consent__card'],
  ];
  for (const [suffix, hero, content] of pages) {
    await navigateDocument(prefix + suffix);
    await waitUntil(`location.pathname === ${JSON.stringify(prefix + suffix)} && !!document.querySelector(${JSON.stringify(content)}) && !document.querySelector('.passport-statistics-page__state--error, .passport-global-statistics__inline-error')`, `private passport ${suffix.split('/')[1]} retains its real guarded deep link and exact isolated response`);
    const name = suffix.startsWith('/items/') ? 'item' : suffix.startsWith('/parks/') ? 'park' : suffix.startsWith('/years/') ? 'year'
      : suffix.startsWith('/comparisons/') ? 'comparison-consent' : suffix.slice(1).replaceAll('/', '-');
    await captureLayouts(hero, `account-passport-${name}`);
    if (!suffix.startsWith('/items/') && !suffix.startsWith('/parks/') && !suffix.startsWith('/years/')) await captureLayouts(content, `account-passport-${name}-content`);
    else await captureLayouts(filled ? '.passport-statistics-page__cards' : '.passport-statistics-page__state', `account-passport-${name}-content`);
    if (suffix === '/statistics' && filled) {
      await evaluate(`(() => { const select = document.querySelectorAll('.passport-global-statistics__filters select')[0]; select.value = '2026'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
      await waitUntil(`document.querySelectorAll('.passport-global-statistics__filters select')[0].value === '2026' && !document.querySelector('.passport-global-statistics__filter-loading') && !!document.querySelector('.passport-global-statistics__filters button')`, 'global passport year filter uses its exact local API query');
      await evaluate(`(() => { const select = document.querySelectorAll('.passport-global-statistics__filters select')[1]; select.value = '0'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
      await waitUntil(`document.querySelectorAll('.passport-global-statistics__filters select')[1].value === '0' && !document.querySelector('.passport-global-statistics__filter-loading')`, 'global passport park filter retains the selected local park');
      await evaluate(`document.querySelector('.passport-global-statistics__filters button').click()`);
      await waitUntil(`[...document.querySelectorAll('.passport-global-statistics__filters select')].every(select => select.value === '') && !document.querySelector('.passport-global-statistics__filter-loading')`, 'global passport filters clear without a saved mutation');
    }
    if (suffix === '/share') {
      await waitUntil(`document.querySelectorAll('.comparison-management__list article').length === ${data.comparisons.length} && document.querySelector('.passport-share__editor > button').disabled`, 'unpublished passport retains its disabled preview and exact comparison management list');
      await captureLayouts('.comparison-management', 'account-passport-comparison-management');
      if (filled) {
        await evaluate(`document.querySelector('.passport-share__choices--compact input').click(); document.querySelectorAll('.passport-share__editor fieldset')[1].querySelector('input').click()`);
        await waitUntil(`!document.querySelector('.passport-share__editor > button').disabled && !document.querySelector('.passport-share__preview')`, 'selecting a local year and park enables preview without requesting or publishing it');
        await captureLayouts('.passport-share__editor', 'account-passport-share-unsubmitted');
      }
    }
    if (suffix.startsWith('/comparisons/')) await waitUntil(filled
      ? `!!document.querySelector('.comparison-consent__people') && !!document.querySelector('.comparison-consent__card > button:not(:disabled)')`
      : `!!document.querySelector('.comparison-consent__blocked') && !document.querySelector('.comparison-consent__people')`, 'synthetic comparison invitation retains its ready or expired consent state without accepting');
  }
  await navigateDocument(`/${language}/profile`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile`)} && !!document.querySelector('.profile-hero')`, 'private passport statistics review returns without publication, comparison consent or account mutation');
  result.passportStatisticsPages = { pages: pages.length, filled, localInputOnly: true, sharingComparisonAndPublicationNotExercised: true };
}
