// Runs only inside capture-account-layout's network-denied .invalid session.
// These are rendering/input checks, not proof of private APIs or persisted writes.
export async function exerciseAccountLists({ evaluate, waitUntil, captureLayouts, fixture, result, language = 'fr' }) {
  const profile = await evaluate(`({ path: location.pathname, index: navigation.currentEntry.index, key: navigation.currentEntry.key })`);
  const restore = async () => {
    await evaluate(`history.go(${profile.index} - navigation.currentEntry.index)`);
    await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(profile.key)} && location.pathname === ${JSON.stringify(profile.path)} && !!document.querySelector('.profile-hero__title')`, 'account list returns to the exact profile history entry');
  };
  await evaluate(`document.querySelector('.profile-passport-entry--collections button').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile/collections`)} && document.querySelectorAll('.collection-card').length === ${fixture.collections.length} && !!document.querySelector(${JSON.stringify(fixture.collections.length ? '.collections-page__grid' : '.collections-page__empty')})`, 'compiled collections render the exact isolated filled or empty response');
  await captureLayouts('.collections-page__hero', 'account-collections');
  const kinds = ['All', 'Favorite', 'WantToVisit', 'WantToExperience', 'Planned'];
  for (let index = 1; index < kinds.length; index++) {
    const count = fixture.collections.filter(entry => entry.kind === kinds[index]).length;
    await evaluate(`document.querySelectorAll('.collections-page__filter')[${index}].click()`);
    await waitUntil(`document.querySelectorAll('.collections-page__filter')[${index}].getAttribute('aria-pressed') === 'true' && document.querySelectorAll('.collection-card').length === ${count}`, `collection filter ${kinds[index]} retains its exact local response count`);
  }
  await evaluate(`document.querySelector('.collections-page__filter').click()`);
  await waitUntil(`document.querySelectorAll('.collection-card').length === ${fixture.collections.length} && document.querySelector('.collections-page__filter').getAttribute('aria-pressed') === 'true'`, 'all-collections filter restores every existing local entry');
  await restore();

  await evaluate(`document.querySelector('.profile-passport-entry--trips button').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile/trips`)} && document.querySelectorAll('.trip-card').length === ${fixture.trips.length} && !!document.querySelector(${JSON.stringify(fixture.trips.length ? '.trip-list-page__grid' : '.trip-list-page__empty')})`, 'compiled trip list renders the exact isolated filled or empty response');
  await captureLayouts('.trip-list-page__hero', 'account-trips');
  await evaluate(`document.querySelector('.trip-list-page__hero-actions button').click()`);
  await waitUntil(`!!document.querySelector('.trip-composer') && document.querySelector('.trip-composer > button').disabled`, 'empty trip composer retains its disabled creation control');
  await evaluate(`(() => { const title = document.querySelector('.trip-composer input[type="text"]'); title.value = 'Voyage de recette uniquement — titre long Äéł, sans création ni enregistrement'; title.dispatchEvent(new Event('input', { bubbles: true }));
    const dates = document.querySelectorAll('.trip-composer input[type="date"]'); dates[0].value = '2026-12-20'; dates[0].dispatchEvent(new Event('input', { bubbles: true })); dates[1].value = '2026-12-19'; dates[1].dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`!!document.querySelector('.trip-composer [role="alert"]') && document.querySelector('.trip-composer > button').disabled`, 'reversed trip dates retain their existing validation without submission');
  await evaluate(`(() => { const end = document.querySelectorAll('.trip-composer input[type="date"]')[1]; end.value = '2026-12-22'; end.dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`!document.querySelector('.trip-composer [role="alert"]') && !document.querySelector('.trip-composer > button').disabled`, 'valid trip draft enables its existing creation control without activating it');
  await captureLayouts('.trip-composer', 'account-trip-draft');
  await evaluate(`document.querySelector('.trip-list-page__hero-actions button').click()`);
  await waitUntil(`!document.querySelector('.trip-composer') && document.querySelectorAll('.trip-card').length === ${fixture.trips.length}`, 'closing the trip composer does not create a local or remote trip');
  await restore();
  result.lists = { collections: fixture.collections.length, trips: fixture.trips.length, persistedWritesExercised: false };
}
