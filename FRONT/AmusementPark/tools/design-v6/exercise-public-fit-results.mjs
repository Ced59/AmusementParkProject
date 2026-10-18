import { publicFitLayoutDate } from './public-fit-layout-fixtures.mjs';

// Existing form -> results -> comparison, with narrowly intercepted CI input.
// No facade injection, real request, park opening, source report or persistence.
export async function exercisePublicFitResults({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result }) {
  const prefix = `/${fixture.language}/park-fit`;
  const readsBefore = result.apiReads.length;
  const parks = fixture.publicFitLayoutData.parks.filter(park => park.scoreState !== 'Excluded');
  const names = parks.map(park => park.parkName);
  const compare = `${prefix}/compare`;
  await navigateDocument(prefix);
  await waitUntil(`document.querySelectorAll('.park-fit-member').length === 1 && !document.querySelector('.park-fit-saved')`,
    'rich Park Fit recipe starts anonymously, without saved profiles or browser location');
  await evaluate(`(() => { for (const [selector, value] of ${JSON.stringify([
    ['#park-fit-date', publicFitLayoutDate],
    ['.park-fit-member input[formcontrolname="heightCentimeters"]', '120'],
    ['.park-fit-member input[formcontrolname="ageYears"]', '9'],
  ])}) { const input = document.querySelector(selector); input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true })); } })()`);
  await waitUntil(`document.querySelector('.park-fit-form').classList.contains('ng-valid')`, 'exact synthetic search criteria pass the unchanged form validation');
  await evaluate(`document.querySelector('.park-fit-submit button[type="submit"]').click()`);
  await waitUntil(`document.querySelector('.park-fit-result h2')?.textContent.trim() === ${JSON.stringify(names[0])}`,
    'valid form renders the first exact local result without a production search');
  await captureLayouts('.park-fit-result', 'public-fit-result-summary');
  await evaluate(`document.querySelector('.park-fit-result__actions a[href="${prefix}/results"]').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(prefix + '/results')} && JSON.stringify([...document.querySelectorAll('.park-fit-result-card h2')].map(node => node.textContent.trim())) === ${JSON.stringify(JSON.stringify(names))}`,
    'result order and five visible cards match the fixture; the Excluded candidate stays absent');
  await waitUntil(`!document.querySelector('.park-fit-results__compare-tray a') && document.querySelectorAll('.park-fit-result-card__score-unavailable').length === 1 && document.querySelectorAll('.park-fit-result-card__no-source').length === 1`,
    'zero selection, suspended score and missing sources retain their distinct states');
  await captureLayouts('.park-fit-results__compare-tray', 'public-fit-results-selection-empty');
  await captureLayouts('.park-fit-result-card__practical', 'public-fit-results-practical');
  await captureLayouts('.park-fit-result-card__proofs', 'public-fit-results-proofs');
  await captureLayouts('.park-fit-result-card:nth-child(2) .park-fit-result-card__score', 'public-fit-results-capped');
  await captureLayouts('.park-fit-result-card:nth-child(3) .park-fit-result-card__score', 'public-fit-results-suspended');

  const toggle = async index => {
    await evaluate(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed]')[${index}].click()`);
  };
  await toggle(1);
  await waitUntil(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 1 && !document.querySelector('.park-fit-results__compare-tray a')`,
    'one selection does not enable comparison');
  await toggle(0);
  await waitUntil(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 2 && !!document.querySelector('.park-fit-results__compare-tray a')`,
    'two selections enable the existing comparison link');
  const openComparison = async count => {
    await evaluate(`document.querySelector('.park-fit-results__compare-tray a[href="${compare}"]').click()`);
    await waitUntil(`location.pathname === ${JSON.stringify(compare)} && JSON.stringify([...document.querySelectorAll('.park-fit-comparison__parks h2')].map(node => node.textContent.trim())) === ${JSON.stringify(JSON.stringify(names.slice(0, count)))}`,
      `${count}-park comparison retains result rank order despite reversed selection order`);
    await captureLayouts('.park-fit-comparison__parks', `public-fit-comparison-${count}-parks`);
    await waitUntil(`document.querySelectorAll('.park-fit-comparison__section').length === 5 && [...document.querySelectorAll('.park-fit-comparison__row')].every(row => row.querySelectorAll('.park-fit-comparison__cell').length === ${count})`,
      `${count}-park matrix retains all five sections and a cell for every selected park`);
    await captureLayouts('.park-fit-comparison__matrix', `public-fit-comparison-${count}-matrix`);
  };
  await openComparison(2);
  const allRows = await evaluate(`document.querySelectorAll('.park-fit-comparison__row').length`);
  await evaluate(`document.querySelector('.park-fit-comparison__toolbar button').click()`);
  await waitUntil(`document.querySelector('.park-fit-comparison__toolbar button').getAttribute('aria-pressed') === 'true' && document.querySelectorAll('.park-fit-comparison__row').length > 0 && document.querySelectorAll('.park-fit-comparison__row').length < ${allRows} && [...document.querySelectorAll('.park-fit-comparison__row')].every(row => row.classList.contains('park-fit-comparison__row--different'))`,
    'differences-only control removes equal rows and retains different values');
  await captureLayouts('.park-fit-comparison__matrix', 'public-fit-comparison-differences');
  await evaluate(`document.querySelector('.park-fit-comparison__toolbar button').click()`);
  await waitUntil(`document.querySelector('.park-fit-comparison__toolbar button').getAttribute('aria-pressed') === 'false' && document.querySelectorAll('.park-fit-comparison__row').length === ${allRows}`,
    'show-all restores the same complete matrix');
  await evaluate(`document.querySelector('.park-fit-comparison__hero a').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(prefix + '/results')} && document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 2`,
    'router return retains both selections without a second search');
  await toggle(2);
  await waitUntil(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 3`,
    'a third existing candidate can be selected before reaching the comparison limit');
  await toggle(3);
  await waitUntil(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 4 && document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed]')[4].disabled && !!document.querySelector('.park-fit-results__compare-limit')`,
    'four selections preserve the limit, disable the fifth candidate and keep removal enabled');
  await captureLayouts('.park-fit-results__compare-tray', 'public-fit-results-selection-limit');
  await openComparison(4);
  await evaluate(`document.querySelector('.park-fit-comparison__hero a').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(prefix + '/results')} && document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 4`,
    'four selections survive the comparison roundtrip');
  await toggle(3);
  await waitUntil(`document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed="true"]').length === 3 && !document.querySelectorAll('.park-fit-result-card__actions button[aria-pressed]')[4].disabled`,
    'removing a selection re-enables the fifth candidate without changing search results');
  const reads = result.apiReads.slice(readsBefore);
  if (reads.filter(read => read.method === 'POST' && read.path === '/api/public/park-fit/search' && read.status === 200).length !== 1
      || reads.some(read => read.method !== 'POST' || !['/api/public/park-fit/search', '/api/public/park-fit/pilot-events'].includes(read.path))) {
    throw new Error('Rich Park Fit recipe did not retain its one exact local search and bounded local telemetry.');
  }
  result.publicFitResults = { language: fixture.language, visibleCandidates: 5, excludedCandidates: 1,
    comparisonSizes: [2, 4], selectionLimit: 4, differencesFilterExercised: true,
    localFixturesOnly: true, realSearchScoringPersistenceReportsAndLocationNotExercised: true };
}
