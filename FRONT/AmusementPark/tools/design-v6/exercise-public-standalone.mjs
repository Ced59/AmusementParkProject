// A named visible QA example, never an application fixture or publication.
export async function exercisePublicStandalone({ evaluate, waitUntil, capturePage, result }) {
  const home = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key, index: navigation.currentEntry.index })`);
  if (home.path !== '/fr/home') throw new Error('Standalone search acceptance must start at the real French home.');
  await evaluate(`(() => { const input = document.querySelector('#home-search-input'); input.value = 'Hasenhorn'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.app-search-panel__submit button').click(); })()`);
  await waitUntil(`!!document.querySelector('.home-results a[href^="/fr/attraction/"]')`, 'actual public search exposes a visible standalone, not a fabricated ParkItem');
  const link = await evaluate(`document.querySelector('.home-results a[href^="/fr/attraction/"]').getAttribute('href')`);
  const attractionId = link.match(/^\/fr\/attraction\/([0-9a-f-]{36})\//i)?.[1];
  if (!attractionId) throw new Error('The public result is not a canonical standalone route.');
  await evaluate(`document.querySelector('.home-results a[href^="/fr/attraction/"]').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(link)} && !!document.querySelector('.standalone-hero h1') && document.querySelectorAll('.standalone-data-row').length > 0`, 'visible standalone opens its own complete detail with real structured fields');
  const title = await evaluate(`document.querySelector('.standalone-hero h1').textContent.trim()`);
  const chooseLanguage = async language => {
    await evaluate(`document.querySelector('.app-public-nav__language').click()`);
    await waitUntil(`document.querySelectorAll('.app-language-dialog .language-option').length === 8`, 'standalone language dialog retains all eight choices');
    await evaluate(`[...document.querySelectorAll('.app-language-dialog .language-option')].find(button => button.querySelector('small')?.textContent.trim() === ${JSON.stringify(language.toUpperCase())}).click()`);
    await waitUntil(`location.pathname.startsWith('/${language}/attraction/${attractionId}/') && document.querySelector('.app-public-nav__language-code')?.textContent.trim() === '${language.toUpperCase()}' && !!document.querySelector('.standalone-hero h1') && !document.querySelector('.app-language-dialog')`, `real standalone language control preserves the autonomous entity in ${language}`);
  };
  result.standaloneScenarios = { attractionId, title, languages: [], hiddenFixtureNotPublishedOrExercised: true };
  const locales = { fr: 'fr_FR', en: 'en_US', de: 'de_DE', nl: 'nl_NL', it: 'it_IT', es: 'es_ES', pt: 'pt_PT', pl: 'pl_PL' };
  for (const language of ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl']) {
    if (language !== 'fr') await chooseLanguage(language);
    await waitUntil(`new URL(document.querySelector('link[rel="canonical"]').href).hostname === 'amusement-parks.fun'
      && new URL(document.querySelector('link[rel="canonical"]').href).pathname === location.pathname
      && document.querySelector('meta[property="og:locale"]')?.content === '${locales[language]}'`, `standalone canonical and Open Graph locale match its real ${language} route`);
    const state = await evaluate(`({ path: location.pathname, detailRows: document.querySelectorAll('.standalone-data-row').length, descriptionPresent: !!document.querySelector('.standalone-rich-text'),
      mapPresent: !!document.querySelector('.standalone-map-card'), inventedParkLinks: document.querySelectorAll('.standalone-hero a[href*="/park/"], .standalone-body a[href*="/park/"]').length })`);
    if (state.inventedParkLinks || !state.detailRows) throw new Error('Standalone lost its real fields or acquired a fabricated park parent.');
    await capturePage('standalone-detail', '.standalone-hero');
    await capturePage('standalone-data', '.standalone-sidebar');
    result.standaloneScenarios.languages.push({ language, ...state });
  }
  await chooseLanguage('fr');
  const detail = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key })`);
  const historySelector = `.standalone-hero__actions a[href="${detail.path}/history"]`;
  await waitUntil(`!!document.querySelector(${JSON.stringify(historySelector)})`, 'standalone exposes its own history, without a parent park');
  await evaluate(`document.querySelector(${JSON.stringify(historySelector)}).click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`${detail.path}/history`)} && !!document.querySelector('app-history-timeline-page h1') && !!document.querySelector(${JSON.stringify(`.history-breadcrumb a[href="${detail.path}"]`)})`, 'standalone history keeps its real owner breadcrumb and route');
  await capturePage('standalone-history', '.history-hero');
  await evaluate('history.back()');
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(detail.key)} && !!document.querySelector('.standalone-hero h1')`, 'standalone history back restores the exact autonomous detail');
  await evaluate(`history.go(${home.index} - navigation.currentEntry.index)`);
  await waitUntil(`location.pathname === ${JSON.stringify(home.path)} && navigation.currentEntry.key === ${JSON.stringify(home.key)} && !!document.querySelector('#home-search-input')`, 'standalone recipe restores the exact original home history entry');
}
