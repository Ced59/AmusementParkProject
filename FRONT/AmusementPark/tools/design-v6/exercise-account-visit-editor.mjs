// Existing UI states with local synthetic input only. No save, assessment,
// ride logging, status mutation, confirmed deletion or publication.
export async function exerciseAccountVisitEditor({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language }) {
  for (const data of fixture.visitEditorData) {
    const path = `/${language}/profile/visits/${data.visit.id}`;
    const state = data.visit.status.toLowerCase();
    await navigateDocument(path);
    await waitUntil(`location.pathname === ${JSON.stringify(path)} && !!document.querySelector('.passport-visit') && !!document.querySelector('.passport-timeline') && !document.querySelector('.passport-editor__state--error') && document.querySelectorAll('.passport-occurrence').length === ${Math.min(2, data.occurrences.length)}`, `visit ${state} deep link renders its exact local date, parent availability and first timeline cursor page`);
    await captureLayouts('.passport-editor__hero', `account-visit-${state}`);
    await captureLayouts('.passport-visit', `account-visit-${state}-metadata`);
    await captureLayouts('.passport-timeline', `account-visit-${state}-timeline`);
    if (data.visit.status === 'Draft') {
      await waitUntil(`!!document.querySelector('.passport-visit__details summary') && document.querySelectorAll('.passport-attraction').length === ${data.targets.length}`, 'draft visit retains editable disclosure and exact historical target rows');
      await captureLayouts('.passport-selector', 'account-visit-draft-selector');
      await evaluate(`document.querySelector('.passport-visit__details summary').click()`);
      await waitUntil(`document.querySelector('.passport-visit__details').open && document.querySelector('.passport-visit__form button').disabled`, 'unchanged visit metadata retains its disabled save control');
      await evaluate(`(() => { const input = document.querySelector('.passport-visit__form input[maxlength="160"]'); input.value = 'Titre de recette uniquement — essai local Äéł'; input.dispatchEvent(new Event('input', { bubbles: true })); })()`);
      await waitUntil(`!document.querySelector('.passport-visit__form button').disabled`, 'local visit title draft enables the existing save control without activation');
      await captureLayouts('.passport-visit__details', 'account-visit-draft-unsubmitted');
      await evaluate(`(() => { const input = document.querySelector('.passport-visit__form input[maxlength="160"]'); input.value = ${JSON.stringify(data.visit.title ?? '')}; input.dispatchEvent(new Event('input', { bubbles: true })); })()`);
      await waitUntil(`document.querySelector('.passport-visit__form button').disabled`, 'restoring the persisted title clears the unsaved metadata draft without saving');
    } else await waitUntil(`!document.querySelector('.passport-visit__details') && !!document.querySelector('.passport-visit__locked') && !document.querySelector('.passport-selector')`, 'completed or archived visit retains its existing read-only controls');
    if (data.occurrences.length > 2) {
      await evaluate(`document.querySelector('.passport-timeline__more').click()`);
      await waitUntil(`document.querySelectorAll('.passport-occurrence').length === ${data.occurrences.length} && !document.querySelector('.passport-timeline__more')`, 'visit journal final cursor appends without duplicating the existing occurrence');
      await captureLayouts('.passport-timeline', `account-visit-${state}-timeline-final`);
    }
    await evaluate(`document.querySelector('.passport-visit-deletion > button').click()`);
    await waitUntil(`!!document.querySelector('.passport-visit-deletion__impact') && document.querySelector('.passport-visit-deletion__actions button').disabled && !document.querySelector('.passport-visit-deletion__confirmation input').checked`, 'visit deletion preview retains counts and explicit unchecked destructive confirmation');
    await captureLayouts('.passport-visit-deletion', `account-visit-${state}-deletion-warning`);
    await evaluate(`document.querySelector('.passport-visit-deletion__actions button:last-child').click()`);
    await waitUntil(`!document.querySelector('.passport-visit-deletion__impact') && !!document.querySelector('.passport-visit-deletion > button')`, 'visit deletion preview cancels without deleting, archiving or exporting');
    if (data.visit.status === 'Completed') {
      await waitUntil(`!!document.querySelector('.visit-share') && !document.querySelector('.visit-share-editor')`, 'completed visit retains unpublished sharing controls without opening publication preview');
      await captureLayouts('.visit-share', 'account-visit-completed-sharing');
    }
  }
  await navigateDocument(`/${language}/profile`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile`)} && !!document.querySelector('.profile-hero')`, 'visit editor review returns with no persisted change or destructive operation');
  result.visitEditor = { statuses: fixture.visitEditorData.map(data => data.visit.status), localFixturesOnly: true,
    writesAssessmentsRideLogsStatusChangesDeletionAndPublicationNotExercised: true };
}
