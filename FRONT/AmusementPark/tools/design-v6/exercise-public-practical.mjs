// Existing Angular links and controls only. Anonymous reads, no clock/data mocks.
export async function exercisePublicPractical({ evaluate, waitUntil, capturePractical, result }) {
  const parent = await evaluate(`({ path: location.pathname, key: navigation.currentEntry.key, index: navigation.currentEntry.index })`);
  const parkId = parent.path.match(/\/park\/([0-9a-f-]{36})(?:\/|$)/i)?.[1];
  if (!parkId) throw new Error('The practical recipe requires its actual public park parent.');
  const backToPark = async selector => {
    await evaluate(`document.querySelector(${JSON.stringify(`${selector} a[href="${parent.path}"]`)}).click()`);
    await waitUntil(`location.pathname === ${JSON.stringify(parent.path)} && !!document.querySelector('app-park-detail-page h1')`, 'practical back link restores the complete park');
  };

  const hoursLink = `app-park-detail-page a[href="${parent.path}/opening-hours"]`;
  await waitUntil(`!!document.querySelector(${JSON.stringify(hoursLink)})`, 'park exposes its actual opening-hours link');
  await evaluate(`document.querySelector(${JSON.stringify(hoursLink)}).click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`${parent.path}/opening-hours`)} && document.querySelectorAll('.park-opening-hours-cell').length > 1 && !document.querySelector('.park-opening-hours-calendar__loading')`, 'real opening calendar loads its available month');
  const initialMonth = await evaluate(`document.querySelector('.park-opening-hours-month__header h2').textContent.trim()`);
  const initialDayDetail = await evaluate(`document.querySelector('.park-opening-hours-day-detail h3')?.textContent.trim() ?? ''`);
  const selectedDay = await evaluate(`(() => { const button = [...document.querySelectorAll('button.park-opening-hours-cell')].find(button => button.getAttribute('aria-pressed') !== 'true'); const day = button.querySelector('.park-opening-hours-cell__date').textContent.trim(); button.click(); return day; })()`);
  await waitUntil(`document.querySelectorAll('button.park-opening-hours-cell[aria-pressed="true"]').length === 1 && document.querySelector('button.park-opening-hours-cell[aria-pressed="true"] .park-opening-hours-cell__date')?.textContent.trim() === ${JSON.stringify(selectedDay)} && !!document.querySelector('.park-opening-hours-day-detail h3') && document.querySelector('.park-opening-hours-day-detail h3').textContent.trim() !== ${JSON.stringify(initialDayDetail)}`, 'calendar day selection updates the existing detail and pressed state');
  await capturePractical('opening-hours-selected', '.park-opening-hours-calendar');
  const direction = await evaluate(`(() => { const buttons = document.querySelectorAll('.park-opening-hours-calendar__nav button'); return !buttons[2].disabled ? 2 : !buttons[0].disabled ? 0 : null; })()`);
  if (direction === null) throw new Error('The calendar fixture has no adjacent published month; this scenario is not covered.');
  let cursor = result.apiReads.length;
  await evaluate(`document.querySelectorAll('.park-opening-hours-calendar__nav button')[${direction}].click()`);
  await waitUntil(`document.querySelector('.park-opening-hours-month__header h2')?.textContent.trim() !== ${JSON.stringify(initialMonth)} && !!document.querySelector('.park-opening-hours-month__header h2') && !document.querySelector('.park-opening-hours-calendar__loading')`, 'calendar changes to the adjacent real month without loading failure');
  const changedMonth = await evaluate(`document.querySelector('.park-opening-hours-month__header h2').textContent.trim()`);
  const monthReads = result.apiReads.slice(cursor).filter(read => read.path === `/api/parks/${parkId}/opening-hours`);
  if (monthReads.some(read => read.status !== 200)) throw new Error('Calendar month API read failed.');
  const returnButton = await evaluate(`(() => { const buttons = document.querySelectorAll('.park-opening-hours-calendar__nav button'); return !buttons[1].disabled ? 1 : ${direction === 2 ? 0 : 2}; })()`);
  await evaluate(`document.querySelectorAll('.park-opening-hours-calendar__nav button')[${returnButton}].click()`);
  await waitUntil(`document.querySelector('.park-opening-hours-month__header h2')?.textContent.trim() === ${JSON.stringify(initialMonth)} && !document.querySelector('.park-opening-hours-calendar__loading')`, 'calendar returns to its original real month');
  (result.practicalScenarios ??= []).push({ kind: 'opening-hours', initialMonth, changedMonth, selectedDay, monthReads, returnButton });
  await backToPark('.park-opening-hours-hero');

  const weatherLink = `app-park-detail-page a[href="${parent.path}/weather"]`;
  await waitUntil(`!!document.querySelector(${JSON.stringify(weatherLink)})`, 'park exposes its actual weather link');
  await evaluate(`document.querySelector(${JSON.stringify(weatherLink)}).click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`${parent.path}/weather`)} && !!document.querySelector('.park-weather-chart__plot') && document.querySelectorAll('.park-weather-chart__row--days strong').length > 0`, 'real weather forecast and date matrix load');
  const forecastDays = await evaluate(`document.querySelectorAll('.park-weather-chart__row--days strong').length`);
  const historyPath = `/api/parks/${parkId}/weather/historical-comparisons`;
  if ((result.apiRequests ?? []).some(read => read.path === historyPath)) throw new Error('Weather history was fetched before its real disclosure opened.');
  cursor = result.apiReads.length;
  await evaluate(`document.querySelector('.park-weather-history summary').click()`);
  await waitUntil(() => result.apiReads.slice(cursor).some(read => read.path === historyPath && read.practical?.kind === 'weather-history'), 'weather history reads its real deferred public response');
  const history = result.apiReads.slice(cursor).find(read => read.path === historyPath && read.practical?.kind === 'weather-history');
  if (history.status !== 200) throw new Error('Weather history API read failed.');
  const parameters = new URLSearchParams(history.query);
  if (Number(parameters.get('days')) !== Math.min(7, forecastDays) || parameters.get('years') !== '10'
    || (parameters.get('forecastDates') ?? '').split(',').filter(Boolean).length !== Math.min(7, forecastDays)) {
    throw new Error('Weather history request no longer matches the current forecast dates and existing year limit.');
  }
  if (history.practical.yearsCount === 0) throw new Error('This ready-history scenario has no published comparison years; empty-state acceptance remains separate.');
  await waitUntil(`document.querySelector('.park-weather-history')?.open && document.querySelectorAll('.park-weather-history-grid__row').length === ${history.practical.yearsCount + 1}`, 'weather history renders its actual returned year count');
  await capturePractical('weather-history-open', '.park-weather-history');
  await evaluate(`document.querySelector('.park-weather-history summary').click()`);
  await waitUntil(`!document.querySelector('.park-weather-history').open`, 'weather historical disclosure closes natively');
  const beforeReopen = result.apiRequests.filter(read => read.path === historyPath).length;
  await evaluate(`document.querySelector('.park-weather-history summary').click()`);
  await waitUntil(`document.querySelector('.park-weather-history').open`, 'weather historical disclosure reopens');
  await evaluate(`new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))`);
  if (result.apiRequests.filter(read => read.path === historyPath).length !== beforeReopen) throw new Error('Reopening cached weather history triggered another API read.');
  result.practicalScenarios.push({ kind: 'weather', forecastDays, historyYears: history.practical.yearsCount, historyQuery: history.query, cachedReopen: true });
  await backToPark('.park-weather-hero');
  // Returning by history, not a reconstructed URL, preserves the caller's entry.
  await evaluate(`history.go(${parent.index} - navigation.currentEntry.index)`);
  await waitUntil(`location.pathname === ${JSON.stringify(parent.path)} && navigation.currentEntry.key === ${JSON.stringify(parent.key)}`, 'practical recipe restores the exact original park history entry');
}
