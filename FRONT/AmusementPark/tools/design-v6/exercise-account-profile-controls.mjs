// Presentation only: never save identity, generate an export or submit deletion.
export async function exerciseAccountProfileControls({ evaluate, waitUntil, captureLayouts, fixture, result }) {
  await evaluate(`document.querySelector('.profile-row--identity > button').click()`);
  await waitUntil(`!!document.getElementById('profile-firstname') && document.getElementById('profile-firstname').value === ${JSON.stringify(fixture.user.firstName)} && !!document.querySelector('label[for="profile-firstname"]')`, 'profile identity editor keeps named inputs and the exact isolated data');
  await captureLayouts('.profile-identity-editor', 'account-identity-editor');
  await evaluate(`document.querySelector('.profile-actions--editing button:last-child').click()`);
  await waitUntil(`!document.querySelector('.profile-identity-editor') && document.querySelector('.profile-hero__title').textContent.trim() === ${JSON.stringify(fixture.user.publicDisplayName || fixture.user.firstName || fixture.user.email)}`, 'profile editor cancellation never saves the isolated identity');
  await waitUntil(`document.querySelectorAll('.passport-export__actions button').length === 2 && !document.querySelector('.passport-export__ready')`, 'existing private export controls remain present without requesting an export');
  await evaluate(`document.querySelector('.account-deletion__open').click()`);
  await waitUntil(`!!document.querySelector('.account-deletion__warning') && document.getElementById('account-deletion-email').type === 'email' && document.getElementById('account-deletion-password').type === 'password' && document.getElementById('account-deletion-password').autocomplete === 'current-password' && !document.getElementById('account-deletion-email').value`, 'account deletion disclosure keeps its warning and confirmation fields without submission');
  await captureLayouts('.account-deletion', 'account-deletion-warning');
  await evaluate(`document.querySelector('.account-deletion__actions button:last-child').click()`);
  await waitUntil(`!!document.querySelector('.account-deletion__open') && !document.querySelector('.account-deletion__fields')`, 'account deletion disclosure cancels without sending a deletion request');
  result.profileControls = { identityNotSaved: true, exportNotRequested: true, deletionNotSubmitted: true };
}
