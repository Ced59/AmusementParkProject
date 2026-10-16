// Compiled private deep links, read-only local fixtures. No save, invitation,
// role change, passport confirmation, download, print or drag-drop mutation.
export async function exerciseAccountTripPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language }) {
  const data = fixture.tripData;
  const prefix = `/${language}/profile/trips/${data.plan.tripPlanId}`;
  const pages = [
    ['', '.trip-overview-page__hero', '.trip-dates'],
    ['/preferences', '.trip-preferences-hero', '.preference-filters'],
    ['/preference-summary', '.trip-summary-hero', '.trip-summary-tools'],
    ['/coherence', '.trip-coherence-hero', '.trip-coherence-section'],
    ['/activity', '.trip-activity-hero', '.trip-activity-feed'],
    ['/export', '.trip-export-hero', '.trip-export-summary'],
    ['/passport', '.trip-passport-hero', '.trip-passport-intro'],
  ];
  for (const [suffix, hero, content] of pages) {
    await navigateDocument(prefix + suffix);
    await waitUntil(`location.pathname === ${JSON.stringify(prefix + suffix)} && !!document.querySelector(${JSON.stringify(content)}) && document.querySelector('h1').textContent.trim() === ${JSON.stringify(data.plan.title)}`, `private trip ${suffix || 'overview'} renders its exact local plan without an API mutation`);
    const name = suffix.slice(1) || 'overview';
    await captureLayouts(hero, `account-trip-${name}`);
    await captureLayouts(content, `account-trip-${name}-content`);
    if (suffix === '') {
      await waitUntil(`document.querySelectorAll('.trip-dates input[type="date"]').length === ${data.plan.canEditPlan ? 2 : 0} && ${data.plan.canEditPlan ? "!document.querySelector('.trip-dates__proposal--readonly')" : "!!document.querySelector('.trip-dates__proposal--readonly')"}`, 'trip date editor or read-only proposal retains its exact local permission state');
      await waitUntil(`document.querySelectorAll('.participant-card').length === 2 && document.querySelectorAll('.participant-card select').length === ${data.participants.canManageRoles ? 1 : 0} && !!document.querySelector('.trip-notification-panel__actions')`, 'trip participants and notification panel retain local role controls without activation');
      await captureLayouts('.trip-notification-panel', 'account-trip-notification-settings');
      await captureLayouts('.participant-panel', 'account-trip-participants');
      if (data.plan.canInvite) {
        await waitUntil(`!!document.querySelector('.invitation-panel__empty') && !!document.querySelector('.invitation-panel__form')`, 'owner invitation form retains its empty list without sending an invitation');
        await captureLayouts('.invitation-panel', 'account-trip-invitations');
      } else await waitUntil(`!document.querySelector('.invitation-panel')`, 'viewer does not acquire owner invitation controls');
    }
    if (suffix === '/preferences') {
      await waitUntil(`document.querySelectorAll('.preference-card').length === ${data.preferences.items.length} && ${data.preferences.canVote ? "!document.querySelector('.trip-preferences-readonly')" : "!!document.querySelector('.trip-preferences-readonly')"}`, 'preference board retains exact rows and voting permission');
      if (data.preferences.items.length) {
        await evaluate(`(() => { const select = document.querySelectorAll('.preference-filters select')[1]; select.value = 'MustDo'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await waitUntil(`document.querySelectorAll('.preference-card').length === 1`, 'local preference filter retains only the exact MustDo item without voting');
        await evaluate(`(() => { const select = document.querySelectorAll('.preference-filters select')[1]; select.value = ''; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await waitUntil(`document.querySelectorAll('.preference-card').length === ${data.preferences.items.length}`, 'preference filter clears without modifying a vote');
      }
    }
    if (suffix === '/preference-summary') {
      await waitUntil(`document.querySelectorAll('.trip-summary-card').length === ${data.summary.items.length} && document.querySelectorAll('.trip-summary-editor').length === ${data.summary.canDecide ? data.summary.items.length : 0}`, 'collective summary retains exact compatibility rows and decision permission');
    }
    if (suffix === '/activity' && data.activity.nextBeforeSequence !== null) {
      await evaluate(`document.querySelector('.trip-activity-feed > button').click()`);
      await waitUntil(`document.querySelectorAll('.trip-activity-entry').length === 2 && !document.querySelector('.trip-activity-feed > button')`, 'activity load-more deduplicates the exact repeated sequence and reaches the final cursor page');
      await captureLayouts('.trip-activity-feed', 'account-trip-activity-final-page');
    }
    if (suffix === '/export') {
      await waitUntil(`document.querySelectorAll('.trip-export-hero__actions button').length === 2`, 'private export retains print and JSON controls without activating them');
    }
    if (suffix === '/passport') {
      await waitUntil(`document.querySelectorAll('.trip-passport-day').length === ${data.transition.days.length}`, 'passport transition retains the exact eligible days without confirming or creating a visit');
    }
  }
  await navigateDocument(`/${language}/profile`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile`)} && !!document.querySelector('.profile-hero')`, 'private trip review returns to the same local account without a saved write');
  result.tripPages = { pages: pages.length, permission: data.plan.effectiveRole, localFixturesOnly: true,
    persistenceInvitationsDownloadsPrintAndPassportConfirmationNotExercised: true };
}
