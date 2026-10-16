// The read-only hydrated phase deliberately leaves all network work blocked.
// Destroy its document before allowing static files/fonts to load again.
export async function resumeStaticCaptures(cdp, pause = () => new Promise(resolve => setTimeout(resolve, 100))) {
  await cdp.send('Page.navigate', { url: 'about:blank' });
  for (let attempt = 0; attempt < 50; attempt++) {
    const state = await cdp.send('Runtime.evaluate', {
      expression: "location.href === 'about:blank' && document.readyState === 'complete'",
      returnByValue: true,
    });
    if (state.result?.value === true) {
      await cdp.send('Network.setBlockedURLs', { urls: [] });
      return { hydratedDocumentDestroyed: true, networkBlockedUntilBlank: true };
    }
    await pause();
  }
  throw new Error('The hydrated document did not close; network remains blocked.');
}
