import { accountLayoutResetQuery } from './account-layout-fixtures.mjs';

// No confirmation, reset or recovery API call. A synthetic token only exposes
// the existing reset form inside the network-denied .invalid browser.
export async function exerciseAccountAuthPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, expectedConfirmationMessage }) {
  const prefix = `/${fixture.language}`;
  await navigateDocument(`${prefix}/confirm-account`);
  await waitUntil(`!!document.querySelector('.auth-feedback--error p') && document.querySelector('.auth-feedback--error p').textContent.trim() === ${JSON.stringify(expectedConfirmationMessage)} && document.querySelector('.auth-feedback a').getAttribute('href') === ${JSON.stringify(`${prefix}/home`)}`, 'missing confirmation token retains its localized fallback, no request and exact home link');
  await captureLayouts('.auth-card', 'account-confirm-missing-token');

  await navigateDocument(`${prefix}/reset-password`);
  await waitUntil(`!!document.querySelector('.auth-feedback--error') && !document.getElementById('newPassword')`, 'missing reset token retains its existing invalid-link state');
  await captureLayouts('.auth-card', 'account-reset-missing-token');

  await navigateDocument(`${prefix}/reset-password${accountLayoutResetQuery}`);
  await waitUntil(`document.getElementById('newPassword')?.type === 'password' && document.getElementById('newPassword').autocomplete === 'new-password' && document.querySelector('.auth-form button[type="submit"]').disabled`, 'local synthetic reset token exposes only the existing empty password form');
  await evaluate(`(() => { for (const id of ['newPassword', 'confirmPassword']) { const input = document.getElementById(id); input.value = 'Ui-Only-Not-Submitted-2026!'; input.dispatchEvent(new Event('input', { bubbles: true })); } })()`);
  await waitUntil(`!document.querySelector('.auth-form button[type="submit"]').disabled`, 'filled reset form retains its existing enabled control without activation');
  await captureLayouts('.auth-card', 'account-reset-form-unsubmitted');

  await navigateDocument(`${prefix}/forgot-password`);
  await waitUntil(`document.getElementById('forgotPasswordEmail')?.type === 'email' && document.querySelector('.auth-page button[type="submit"]').disabled`, 'recovery deep link retains its existing empty email form');
  await captureLayouts('.auth-card', 'account-recovery-empty');
  await evaluate(`(() => { const input = document.getElementById('forgotPasswordEmail'); input.value = ${JSON.stringify(fixture.user.email)}; input.dispatchEvent(new Event('input', { bubbles: true })); })()`);
  await waitUntil(`!document.querySelector('.auth-page button[type="submit"]').disabled`, 'filled recovery form retains its existing enabled control without sending email');
  await captureLayouts('.auth-card', 'account-recovery-unsubmitted');
  await navigateDocument(`${prefix}/profile`);
  await waitUntil(`location.pathname === ${JSON.stringify(`${prefix}/profile`)} && !!document.querySelector('.profile-hero')`, 'auth-page review returns to the same local account without authentication mutations');
  result.authPages = { language: fixture.language, confirmationNotRequested: true, resetNotSubmitted: true, recoveryEmailNotSent: true,
    syntheticTokenScope: 'local UI input only; never a real account token or sent to a server' };
}
