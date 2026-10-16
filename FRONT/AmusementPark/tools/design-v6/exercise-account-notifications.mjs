// Existing compiled controls only. Never mark/read/dismiss/report, change email
// consent or stop a subscription. Automatic page-open telemetry stays local.
export async function exerciseAccountNotifications({ evaluate, waitUntil, captureLayouts, fixture, result, language }) {
  const historyEntry = await evaluate(`({ index: navigation.currentEntry.index, key: navigation.currentEntry.key })`);
  const items = fixture.notificationData.notifications;
  await evaluate(`document.querySelector('.profile-panel button:has(.pi-bell)').click()`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile/notifications`)} && document.querySelectorAll('.notification-card').length === ${Math.min(12, items.length)} && !!document.querySelector('.email-preferences__promise') && ${items.length ? "!!document.querySelector('.live-inbox__notifications')" : "!!document.querySelector('.notifications-empty')"}`, 'notification center renders its exact local page, email disclosure and full or empty live inbox');
  await captureLayouts('.notifications-page__hero', 'account-notifications');
  await captureLayouts('.email-preferences', 'account-notification-email');
  await captureLayouts('.live-inbox', 'account-live-alerts');
  if (!items.length) {
    await waitUntil(`document.querySelector('.email-preferences__actions button').disabled && !document.querySelector('.notification-card') && !document.querySelector('.live-inbox__notifications')`, 'unavailable email and empty inbox retain their existing disabled and empty states');
  } else {
    await captureLayouts('.notifications-list', 'account-notification-list');
    await evaluate(`[...document.querySelectorAll('.p-paginator-page')].find(button => button.textContent.trim() === '2').click()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === 1 && document.querySelector('.p-paginator-page.p-highlight')?.textContent.trim() === '2'`, 'notification page two uses the exact server-shaped final row, not client-only slicing');
    await captureLayouts('.notifications-list', 'account-notification-page-two');
    await evaluate(`document.querySelectorAll('.notifications-filters__quick button')[1].click()`);
    const unread = items.filter(item => item.status === 'Delivered').length;
    await waitUntil(`document.querySelectorAll('.notification-card').length === ${unread} && document.querySelectorAll('.notification-card--unread').length === ${unread} && document.querySelectorAll('.notifications-filters__quick button')[1].getAttribute('aria-pressed') === 'true'`, 'unread-only filter resets pagination and preserves the exact delivered subset');
    await captureLayouts('.notifications-filters', 'account-notification-unread');
    await evaluate(`document.querySelectorAll('.notifications-filters__quick button')[0].click()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === 12`, 'all-notifications filter restores the first server-shaped page');
    const parkId = items[0].target.parkId;
    await evaluate(`(() => { const select = document.querySelectorAll('.notifications-filters select')[0]; select.value = ${JSON.stringify(parkId)}; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === ${items.filter(item => item.target.parkId === parkId).length}`, 'park filter retains the exact local target subset');
    await evaluate(`(() => { const select = document.querySelectorAll('.notifications-filters select')[0]; select.value = ''; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === 12`, 'park filter clears without losing the original page');
    await evaluate(`(() => { const select = document.querySelectorAll('.notifications-filters select')[1]; select.value = 'ParkNameChanged'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === ${items.filter(item => item.eventType === 'ParkNameChanged').length}`, 'event-type filter retains the exact local event subset');
    await evaluate(`(() => { const select = document.querySelectorAll('.notifications-filters select')[1]; select.value = 'VerifiedSourceAdded'; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitUntil(`!!document.querySelector('.notifications-message--error[role="alert"] button')`, 'intentional isolated response failure retains its named retry and error state');
    await captureLayouts('.notifications-message--error', 'account-notification-error');
    const readsBeforeRetry = result.apiReads.filter(read => read.path === '/api/me/notifications' && read.status === 503).length;
    await evaluate(`document.querySelector('.notifications-message--error button').click()`);
    await waitUntil(async () => result.apiReads.filter(read => read.path === '/api/me/notifications' && read.status === 503).length > readsBeforeRetry
      && await evaluate(`!!document.querySelector('.notifications-message--error[role="alert"]')`), 'notification retry repeats the same intentional local failure without a mutation');
    await evaluate(`(() => { const select = document.querySelectorAll('.notifications-filters select')[1]; select.value = ''; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await waitUntil(`document.querySelectorAll('.notification-card').length === 12 && !document.querySelector('.notifications-message--error')`, 'clearing the event filter recovers the original notification page');
  }
  await evaluate(`history.go(${historyEntry.index} - navigation.currentEntry.index)`);
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(historyEntry.key)} && location.pathname === ${JSON.stringify(`/${language}/profile`)} && !!document.querySelector('.profile-hero')`, 'notification review restores the same profile history entry');
  result.notifications = { localRows: items.length, mutationsExercised: false, automaticPageOpenTelemetry: 'exact local stub only; never forwarded',
    emailConsentChanged: false, readDismissReportAndUnsubscribeNotExercised: true };
}
