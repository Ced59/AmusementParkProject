import { accountLayoutFixtureOrigin } from './account-layout-fixtures.mjs';
import { createAnonymousDraftFixtures, anonymousDraftIds, seedAnonymousDraftBrowser } from './account-anonymous-draft-fixtures.mjs';

export function openUnlockedAnonymousDraft() {
  const unlocked = Array.from(document.querySelectorAll('.anonymous-draft-card'))
    .filter(card => card.querySelector('.anonymous-draft-card__actions button:last-child')?.disabled === false);
  if (unlocked.length !== 1) throw new Error('The isolated draft list must contain exactly one unlocked draft.');
  const button = unlocked[0].querySelector('.anonymous-draft-card__actions button:first-child');
  if (!button || button.disabled) throw new Error('The unlocked draft has no enabled continue control.');
  button.click();
}

// Local IndexedDB only, in the isolated CI origin. No import, export, confirmed
// deletion, account creation or persisted remote write.
export async function exerciseAnonymousDraftPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language }) {
  const prefix = `/${language}/passport/local`;
  const seed = drafts => evaluate(`(${seedAnonymousDraftBrowser.toString()})(${JSON.stringify(accountLayoutFixtureOrigin)}, ${JSON.stringify(drafts)})`);
  const before = result.apiReads.length;
  try {
    await seed([]);
    await navigateDocument(prefix);
    await waitUntil(`location.pathname === ${JSON.stringify(prefix)} && !!document.querySelector('.anonymous-passport__empty') && document.querySelector('.anonymous-passport__hero-actions button').disabled && !document.querySelector('.app-public-nav__profile')`, 'anonymous local passport starts empty with no authenticated header or enabled export');
    await captureLayouts('.anonymous-passport__hero', 'account-anonymous-passport-empty');
    if (fixture.kind === 'standard') {
      const drafts = createAnonymousDraftFixtures(fixture.user.createdAt);
      await seed(drafts);
      await navigateDocument(prefix);
      await waitUntil(`document.querySelectorAll('.anonymous-draft-card').length === 2 && document.querySelector('.anonymous-passport__heading button').disabled`, 'real IndexedDB reader accepts both exact local draft schemas and protects the import-locked draft');
      await captureLayouts('.anonymous-passport__content', 'account-anonymous-passport-list');
      await evaluate(`(${openUnlockedAnonymousDraft.toString()})()`);
      await waitUntil(`location.pathname === ${JSON.stringify(`${prefix}/${anonymousDraftIds[0]}`)} && document.querySelectorAll('.anonymous-attraction').length === 1 && !!document.querySelector('.anonymous-editor__rides') && !document.querySelector('.anonymous-editor__error')`, 'anonymous draft detail opens the existing route and preserves local rides without an account import');
      await captureLayouts('.anonymous-editor__hero', 'account-anonymous-editor');
      await captureLayouts('.anonymous-editor__timeline', 'account-anonymous-editor-timeline');
      await evaluate(`document.querySelector('.anonymous-attraction').click()`);
      await waitUntil(`!!document.querySelector('.anonymous-editor__ride-form') && !document.querySelector('.anonymous-editor__ride-form input[type="time"]')`, 'local attraction selection exposes the existing ride form without inventing time for a year-only visit');
      await captureLayouts('.anonymous-editor__ride-form', 'account-anonymous-editor-unsubmitted');
      await navigateDocument(`${prefix}/${anonymousDraftIds[1]}`);
      await waitUntil(`!!document.querySelector('.anonymous-editor__warning') && !document.querySelector('.anonymous-editor__add') && document.querySelector('.anonymous-editor__hero-actions button:last-child').disabled`, 'pending anonymous import retains its locked state without starting or resuming it');
      await captureLayouts('.anonymous-editor__hero', 'account-anonymous-editor-locked');
      await captureLayouts('.anonymous-editor__warning', 'account-anonymous-editor-lock-warning');
    } else {
      await navigateDocument(`${prefix}/${anonymousDraftIds[0]}`);
      await waitUntil(`!!document.querySelector('.anonymous-editor__state[role="alert"]') && !document.querySelector('.anonymous-editor__hero')`, 'missing local draft retains its existing named error and back control');
      await captureLayouts('.anonymous-editor__state', 'account-anonymous-editor-missing');
    }
    if (result.apiReads.slice(before).some(read => read.path.startsWith('/api/users/') || read.path.startsWith('/api/me/') || read.path.startsWith('/api/auth/'))) throw new Error('Anonymous draft review unexpectedly requested authenticated data.');
    result.anonymousDrafts = { localOnly: true, indexedDbExercised: true, rows: fixture.kind === 'standard' ? 2 : 0,
      importExportAndConfirmedDeletionNotExercised: true };
  } finally {
    await seed([]);
  }
  await navigateDocument(`/${language}/profile`);
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/profile`)} && !!document.querySelector('.profile-hero')`, 'anonymous draft review restores the original local fixture account after clearing only its isolated test records');
}
