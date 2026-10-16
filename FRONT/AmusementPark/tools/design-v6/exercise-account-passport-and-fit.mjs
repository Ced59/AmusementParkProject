// UI-only synthetic private input, inside capture-account-layout's denied network.
// No export, creation, update, sharing, confirmed deletion or remote API call.
export async function exerciseAccountPassportAndFit({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result }) {
  const profile = await evaluate(`({ index: navigation.currentEntry.index, key: navigation.currentEntry.key })`);
  await evaluate(`document.querySelector('.profile-panel button:has(.pi-book)').click()`);
  const firstPageCount = Math.min(2, fixture.visits.length);
  await waitUntil(`location.pathname === '/fr/profile/passport' && document.querySelectorAll('.passport-visit-card').length === ${firstPageCount} && !!document.querySelector(${JSON.stringify(firstPageCount ? '.passport-overview__content' : '.passport-overview__state')})`, 'passport overview renders the exact isolated first cursor page or empty state');
  await captureLayouts('.passport-overview__hero', 'account-passport');
  if (fixture.visits.length > firstPageCount) {
    await evaluate(`document.querySelector('.passport-overview__load-more button').click()`);
    await waitUntil(`document.querySelectorAll('.passport-visit-card').length === ${fixture.visits.length} && !document.querySelector('.passport-overview__load-more')`, 'passport load-more appends the exact final cursor response without duplicating an existing visit');
    await captureLayouts('.passport-overview__content', 'account-passport-final-page');
  }
  await evaluate(`history.go(${profile.index} - navigation.currentEntry.index)`);
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(profile.key)} && location.pathname === '/fr/profile' && !!document.querySelector('.profile-hero')`, 'passport review restores the same profile history entry');

  await navigateDocument('/fr/profile/park-fit/profiles');
  await waitUntil(`location.pathname === '/fr/profile/park-fit/profiles' && document.querySelectorAll('.park-fit-profiles__card').length === ${fixture.fitProfiles.length} && !!document.querySelector(${JSON.stringify(fixture.fitProfiles.length ? '.park-fit-profiles__cards' : '.park-fit-profiles__empty')})`, 'private Park Fit profile deep link uses the existing guard and exact isolated list');
  await captureLayouts('.park-fit-profiles__hero', 'account-park-fit');
  await captureLayouts('.park-fit-profiles__library', 'account-park-fit-library');
  if (fixture.fitProfiles.length) {
    await evaluate(`document.querySelector('.park-fit-profiles__card-actions button').click()`);
    await waitUntil(`document.querySelector('input[formcontrolname="alias"]').value === ${JSON.stringify(fixture.fitProfiles[0].alias)} && !!document.querySelector('.park-fit-profiles__composer .park-fit-profiles__heading button')`, 'Park Fit edit uses the exact existing local profile without updating it');
    await evaluate(`document.querySelector('.park-fit-profiles__composer .park-fit-profiles__heading button').click()`);
    await waitUntil(`document.querySelector('input[formcontrolname="alias"]').value === ''`, 'new-profile action clears the editor without saving');
    await evaluate(`document.querySelector('.park-fit-profiles__delete').click()`);
    await waitUntil(`document.querySelectorAll('.park-fit-profiles__card-actions')[0].querySelectorAll('button').length === 3`, 'Park Fit deletion requires its existing second confirmation');
    await captureLayouts('.park-fit-profiles__library', 'account-park-fit-delete-warning');
    await evaluate(`document.querySelectorAll('.park-fit-profiles__card-actions')[0].querySelector('button:last-child').click()`);
    await waitUntil(`document.querySelectorAll('.park-fit-profiles__card-actions')[0].querySelectorAll('button').length === 2 && document.querySelectorAll('.park-fit-profiles__card').length === ${fixture.fitProfiles.length}`, 'Park Fit deletion confirmation cancels without removing a profile');
  }
  await evaluate(`(() => { const alias = document.querySelector('input[formcontrolname="alias"]'); alias.value = 'Groupe de recette — Élise et Łukasz'; alias.dispatchEvent(new Event('input', { bubbles: true }));
    const height = document.querySelector('input[formcontrolname="heightCentimeters"]'); height.value = '30'; height.dispatchEvent(new Event('input', { bubbles: true })); height.dispatchEvent(new Event('blur')); })()`);
  await waitUntil(`!!document.querySelector('.park-fit-profiles__composer [role="alert"]')`, 'Park Fit height below the existing minimum retains its visible form validation');
  await evaluate(`(() => { const height = document.querySelector('input[formcontrolname="heightCentimeters"]'); height.value = '120'; height.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('input[formcontrolname="canBeAccompanied"]').click(); })()`);
  await waitUntil(`!document.querySelector('.park-fit-profiles__composer [role="alert"]') && !!document.querySelector('input[formcontrolname="companionAgeYears"]')`, 'valid Park Fit facts retain the conditional accompaniment field without submission');
  await captureLayouts('.park-fit-profiles__composer', 'account-park-fit-draft');
  await navigateDocument('/fr/profile');
  await waitUntil(`location.pathname === '/fr/profile' && !!document.querySelector('.profile-hero')`, 'private Park Fit draft is left without a saved mutation');
  result.passportAndFit = { visits: fixture.visits.length, fitProfiles: fixture.fitProfiles.length, persistedWritesExercised: false, sharingNotExercised: true };
}
