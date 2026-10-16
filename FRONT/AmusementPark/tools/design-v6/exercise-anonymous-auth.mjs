// Isolated UI validation only: no login/registration/reset submission, provider
// popup or real credentials. The caller rejects every non-GET/HEAD request.
export async function exerciseAnonymousAuth({ evaluate, waitUntil, capturePage, result }) {
  const home = await evaluate(`({ path: location.pathname, index: navigation.currentEntry.index, key: navigation.currentEntry.key })`);
  result.anonymousAuth = { loginNotSubmitted: true, registrationNotSubmitted: true,
    googleProviderNotExercised: true, authenticatedAccountNotExercised: true, resetTokenWorkflowNotExercised: true };
  await evaluate(`document.querySelector('.app-public-nav__right .btn-nav--primary').click()`);
  await waitUntil(`!!document.querySelector('app-auth-modal #registerEmail') && !!document.querySelector('app-auth-modal #loginEmail')`, 'anonymous auth validation opens the existing login and registration dialog');
  const registerSubmit = "document.querySelector('app-register-form button[type=submit]')";
  await waitUntil(`${registerSubmit}.disabled`, 'empty registration retains its disabled submit');
  const fill = async values => {
    await evaluate(`(() => { const values = ${JSON.stringify(values)}; for (const [id, value] of Object.entries(values)) {
      const input = document.getElementById(id); input.value = value; input.dispatchEvent(new Event('input', { bubbles: true }));
    } })()`);
  };
  await fill({ registerEmail: 'not-an-email', registerPassword: 'short', confirmPassword: 'different' });
  await waitUntil(`${registerSubmit}.disabled`, 'invalid registration fields retain the real client-side validation');
  await fill({ registerEmail: 'validation@apf-ui-test.invalid', registerPassword: 'Ui-Validation-Only-2026!', confirmPassword: 'different' });
  await waitUntil(`${registerSubmit}.disabled`, 'registration password mismatch retains its disabled submit');
  await fill({ confirmPassword: 'Ui-Validation-Only-2026!' });
  await waitUntil(`!${registerSubmit}.disabled`, 'matching valid registration fields enable the existing control without submitting');
  await fill({ registerEmail: '', registerPassword: '', confirmPassword: '' });
  await waitUntil(`${registerSubmit}.disabled`, 'registration fields are cleared without creating an account');
  result.anonymousAuth.fields = await evaluate(`({ loginEmail: document.getElementById('loginEmail').type,
    loginPassword: document.getElementById('loginPassword').type,
    loginAutocomplete: document.getElementById('loginPassword').autocomplete,
    registerAutocomplete: document.getElementById('registerPassword').autocomplete,
    providerHost: !!document.querySelector('.google-button-host') })`);
  if (result.anonymousAuth.fields.loginEmail !== 'email' || result.anonymousAuth.fields.loginPassword !== 'password'
      || result.anonymousAuth.fields.loginAutocomplete !== 'current-password'
      || result.anonymousAuth.fields.registerAutocomplete !== 'new-password'
      || !result.anonymousAuth.fields.providerHost) throw new Error('Existing auth field/provider presentation contract changed.');
  await evaluate(`document.querySelector('app-login-form .auth-form__link').click()`);
  const language = home.path.split('/')[1];
  await waitUntil(`location.pathname === ${JSON.stringify(`/${language}/forgot-password`)} && !!document.getElementById('forgotPasswordEmail') && !document.querySelector('app-auth-modal')`, 'forgot-password action closes the dialog and opens the same localized account route');
  await waitUntil(`document.querySelector('.auth-page button[type="submit"]').disabled`, 'empty password recovery retains its disabled submit');
  const recoveryKey = await evaluate('navigation.currentEntry.key');
  await capturePage('forgot-password-empty', '.auth-card');
  await evaluate(`history.go(${home.index} - navigation.currentEntry.index)`);
  await waitUntil(`navigation.currentEntry.key === ${JSON.stringify(home.key)} && location.pathname === ${JSON.stringify(home.path)} && !!document.querySelector('#home-search-input')`, 'anonymous auth review restores the exact home history entry without sending email');
  result.anonymousAuth.recovery = { path: `/${language}/forgot-password`, key: recoveryKey, emailNotSent: true };
}
