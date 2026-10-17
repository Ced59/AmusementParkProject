// Existing public controls only. No valid submit, geolocation or remote search.
export async function exercisePublicFitForm({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result }) {
  const prefix = `/${fixture.language}/park-fit`;
  const readsBefore = result.apiReads.length;
  await navigateDocument(prefix);
  await waitUntil(`!!document.querySelector('.park-fit-form') && document.querySelectorAll('.park-fit-member').length === 1 && !document.querySelector('.park-fit-saved')`,
    'anonymous Park Fit starts with one local participant and no saved-profile request');
  await captureLayouts('.park-fit-form', 'public-fit-form-initial');

  await evaluate(`document.querySelector('.park-fit-member input[formcontrolname="canBeAccompanied"]').click()`);
  await waitUntil(`!!document.querySelector('.park-fit-field--companion')`, 'accompanied participant exposes the existing companion-age field');
  await evaluate(`(() => { for (const [name, value] of [['heightCentimeters', '120'], ['ageYears', '9'], ['companionAgeYears', '32']]) {
    const input = document.querySelector('.park-fit-member input[formcontrolname="' + name + '"]');
    input.value = value; input.dispatchEvent(new Event('input', { bubbles: true }));
  } })()`);
  await captureLayouts('.park-fit-member', 'public-fit-form-accompanied');

  for (let count = 2; count <= 8; count++) {
    await evaluate(`document.querySelector('.park-fit-group-actions button').click()`);
    await waitUntil(`document.querySelectorAll('.park-fit-member').length === ${count}`, `Park Fit retains its existing participant count ${count}`);
  }
  await waitUntil(`document.querySelector('.park-fit-group-actions button').disabled`, 'the eighth participant disables the existing add control');
  await captureLayouts('.park-fit-member:last-child', 'public-fit-form-eighth-participant');
  for (let count = 7; count >= 1; count--) {
    await evaluate(`document.querySelector('.park-fit-member:last-child .park-fit-member__remove').click()`);
    await waitUntil(`document.querySelectorAll('.park-fit-member').length === ${count}`, `local removal restores participant count ${count}`);
  }
  await waitUntil(`!document.querySelector('.park-fit-member__remove') && !document.querySelector('.park-fit-group-actions button').disabled`,
    'the last participant cannot be removed and adding is enabled again');
  await evaluate(`document.querySelector('.park-fit-preference').click(); document.querySelector('input[formcontrolname="preferIndoor"]').click()`);
  await waitUntil(`document.querySelector('.park-fit-preference').getAttribute('aria-pressed') === 'true' && document.querySelector('input[formcontrolname="preferIndoor"]').checked`,
    'existing preference and indoor controls keep their local selected states');
  await captureLayouts('.park-fit-preferences', 'public-fit-form-preferences');
  await evaluate(`document.querySelector('.park-fit-preference').click()`);
  await waitUntil(`document.querySelector('.park-fit-preference').getAttribute('aria-pressed') === 'false'`, 'the attraction preference toggles off without a search');

  await evaluate(`(() => { const input = document.getElementById('park-fit-date'); input.value = ''; input.dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`document.getElementById('park-fit-date').classList.contains('ng-invalid')`, 'the deliberately empty date is invalid before activating submit');
  await evaluate(`document.querySelector('.park-fit-submit button[type="submit"]').click()`);
  await waitUntil(`document.querySelector('.park-fit-inline-error')?.getAttribute('role') === 'alert' && !document.querySelector('.park-fit-result')`,
    'invalid submit exposes the existing date error, never a computed result');
  await captureLayouts('.park-fit-step', 'public-fit-form-invalid-date');

  await navigateDocument(prefix + '/results');
  await waitUntil(`!!document.querySelector('.park-fit-results__empty') && document.querySelector('.park-fit-results__empty a').getAttribute('href') === ${JSON.stringify(prefix)}`,
    'results without a prior search preserve their missing-result state and criteria link');
  await captureLayouts('.park-fit-results', 'public-fit-results-missing');
  await navigateDocument(prefix + '/compare');
  await waitUntil(`!!document.querySelector('.park-fit-comparison__missing') && !document.querySelector('.park-fit-comparison__parks')`,
    'comparison without a selection preserves its existing missing-selection state');
  await captureLayouts('.park-fit-comparison', 'public-fit-comparison-missing');
  if (result.apiReads.length !== readsBefore) throw new Error('The local Park Fit form unexpectedly requested an API or search.');
  result.publicFitForm = { language: fixture.language, maximumParticipants: 8, invalidSubmitOnly: true,
    searchRequested: false, locationRequested: false, scoredResultsExercised: false };
}
