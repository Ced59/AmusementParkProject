import { readFile, writeFile } from 'node:fs/promises';
import { extname, join, resolve, sep } from 'node:path';
import { accountLayoutFixtureOrigin, accountLayoutRefreshMarker, accountLayoutFixtureResponse } from './account-layout-fixtures.mjs';
import { isCanceledInterceptionError, isRetiredDocumentInterceptionError, advanceMainDocument } from './browser-interception-errors.mjs';

export function accountLayoutCaptureAllowed(ci, platform) {
  return ci === 'true' && platform === 'linux';
}

// Compiled application, existing guards/services, synthetic HTTP input only.
// This is UI acceptance, never proof of authentication or a private API call.
export async function captureAccountLayout({ cdp, browserRoot, outputRoot, widths, fixture, label }) {
  if (!accountLayoutCaptureAllowed(process.env.CI, process.platform)) throw new Error('Account layout capture requires isolated Linux CI.');
  if (!['master', 'v6'].includes(label) || !['standard', 'incomplete'].includes(fixture.kind)) throw new Error('Unsupported account layout capture case.');
  if (!Array.isArray(widths) || widths.length < 1 || widths.length > 13 || widths.some(width => !Number.isInteger(width) || width < 320 || width > 1920)) throw new Error('Account capture widths exceed their bound.');
  const root = resolve(browserRoot);
  const rawHtml = await readFile(join(root, 'index.csr.html'), 'utf8');
  const result = { scope: 'compiled account UI with CI-only synthetic input; not authentication/API/HTTP-SEO acceptance', label,
    kind: fixture.kind, checks: [], layouts: [], apiReads: [], blockedRequests: [], fixtureFailures: [], exceptions: [] };
  const pending = new Set();
  const canceled = new Set();
  const documents = new Map();
  const retired = new Set();
  let mainDocument;
  const mimeTypes = { '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json',
    '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.svg': 'image/svg+xml', '.webp': 'image/webp', '.png': 'image/png', '.ico': 'image/x-icon' };
  let closing = false;
  let injectedScript;
  const evaluate = async expression => {
    const response = await cdp.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (response.exceptionDetails) throw new Error((response.exceptionDetails.exception?.description ?? response.exceptionDetails.text).split('\n')[0].slice(0, 1000));
    return response.result.value;
  };
  const waitUntil = async (predicate, label) => {
    result.activeCheck = label;
    for (let attempt = 0; attempt < 100; attempt++) {
      if (result.fixtureFailures.length || result.exceptions.length) throw new Error(`Isolated account fixture failed: ${label}`);
      if (typeof predicate === 'function' ? await predicate() : await evaluate(predicate)) { result.checks.push(label); return; }
      await new Promise(resolveWait => setTimeout(resolveWait, 200));
    }
    throw new Error(`Isolated account layout timed out: ${label}`);
  };
  const fulfill = (requestId, status, contentType, bytes) => cdp.send('Fetch.fulfillRequest', { requestId, responseCode: status,
    responseHeaders: [{ name: 'Content-Type', value: contentType }, { name: 'Cache-Control', value: 'no-store' }], body: Buffer.from(bytes).toString('base64') });
  const intercept = async ({ requestId, request, resourceType }) => {
    const url = new URL(request.url);
    if (closing || url.origin !== accountLayoutFixtureOrigin) {
      result.blockedRequests.push({ method: request.method, origin: url.origin, path: url.pathname });
      await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' });
      return;
    }
    if (url.pathname.startsWith('/api/')) {
      const response = accountLayoutFixtureResponse(request, fixture);
      if (!response) {
        result.fixtureFailures.push({ method: request.method, path: url.pathname, query: url.search, reason: 'no exact local fixture; never forwarded' });
        await fulfill(requestId, 503, 'application/problem+json', JSON.stringify({ status: 503, title: 'Missing isolated account fixture' }));
        return;
      }
      result.apiReads.push({ method: request.method, path: url.pathname, status: response.status, source: 'local CI fixture only' });
      await fulfill(requestId, response.status, 'application/json', JSON.stringify(response.body));
      return;
    }
    if (!['GET', 'HEAD'].includes(request.method)) throw new Error('Unexpected non-fixture mutation in account layout capture.');
    if (resourceType === 'Document') {
      if (url.pathname !== '/fr/profile' || url.search) throw new Error('Unexpected account fixture document navigation.');
      await fulfill(requestId, 200, 'text/html; charset=utf-8', rawHtml);
      return;
    }
    const assetPath = resolve(root, `.${decodeURIComponent(url.pathname)}`);
    if (!assetPath.startsWith(root + sep)) throw new Error('Rejected account fixture asset traversal.');
    try {
      await fulfill(requestId, 200, mimeTypes[extname(assetPath)] ?? 'application/octet-stream', await readFile(assetPath));
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
      await fulfill(requestId, 404, 'text/plain', 'No isolated build asset at this path.');
    }
  };
  const stopCancellation = cdp.on('Network.loadingFailed', ({ requestId, canceled: wasCanceled }) => { if (wasCanceled) canceled.add(requestId); });
  const stopDocuments = cdp.on('Network.requestWillBeSent', ({ requestId, frameId, loaderId }) => {
    if (!frameId || !loaderId) return;
    if (documents.size >= 4096 && !documents.has(requestId)) { result.fixtureFailures.push({ reason: 'request/document evidence bound exceeded' }); return; }
    documents.set(requestId, { frameId, loaderId });
  });
  const stopRetirement = cdp.on('Page.frameNavigated', ({ frame }) => { mainDocument = advanceMainDocument(mainDocument, frame, documents, retired); });
  const stopExceptions = cdp.on('Runtime.exceptionThrown', ({ exceptionDetails }) => result.exceptions.push({ text: exceptionDetails.text, line: exceptionDetails.lineNumber }));
  const stopInterception = cdp.on('Fetch.requestPaused', request => {
    const task = intercept(request).catch(async error => {
      if (error.message === 'Invalid InterceptionId.' && request.networkId) await new Promise(yes => setTimeout(yes, 100));
      if (!isCanceledInterceptionError(error, request.networkId, canceled) && !isRetiredDocumentInterceptionError(error, request.networkId, retired)) {
        result.fixtureFailures.push({ method: request.request.method, path: new URL(request.request.url).pathname, reason: error.message.slice(0, 1000) });
        try { await cdp.send('Fetch.failRequest', { requestId: request.requestId, errorReason: 'BlockedByClient' }); } catch { /* original error is retained */ }
      }
    }).finally(() => pending.delete(task));
    pending.add(task);
  });
  try {
    await cdp.send('Page.enable');
    await cdp.send('Runtime.enable');
    await cdp.send('Network.enable');
    await cdp.send('Network.setBlockedURLs', { urls: [] });
    await cdp.send('Emulation.setScriptExecutionDisabled', { value: false });
    await cdp.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }, { name: 'prefers-color-scheme', value: 'dark' }] });
    await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });
    injectedScript = (await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source:
      `if (location.origin === ${JSON.stringify(accountLayoutFixtureOrigin)}) localStorage.setItem(${JSON.stringify(accountLayoutRefreshMarker)}, 'true');` })).identifier;
    await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] });
    await cdp.send('Page.navigate', { url: `${accountLayoutFixtureOrigin}/fr/profile` });
    const displayName = fixture.user.publicDisplayName || fixture.user.firstName || fixture.user.email;
    await waitUntil(`document.querySelector('.profile-hero__title')?.textContent.trim() === ${JSON.stringify(displayName)}`, 'existing guard and refresh flow render the local account fixture');
    await waitUntil(`!document.querySelector('a[href="/fr/admin"]')`, 'standard account fixture does not expose the conditional admin entry');
    await evaluate(`document.querySelector('.app-cookie-consent__button--ghost')?.click()`);
    await waitUntil(`!document.querySelector('.app-cookie-consent')`, 'account layout uses the real necessary-cookie control');
    await evaluate(`document.querySelector('.profile-row--identity > button').click()`);
    await waitUntil(`!!document.getElementById('profile-firstname') && document.getElementById('profile-firstname').value === ${JSON.stringify(fixture.user.firstName)}`, 'identity editor retains the exact fixture fields without submitting');
    await evaluate(`document.querySelector('.profile-actions--editing button:last-child').click()`);
    await waitUntil(`!document.querySelector('.profile-identity-editor') && document.querySelector('.profile-hero__title').textContent.trim() === ${JSON.stringify(displayName)}`, 'identity cancellation restores the same visible account');
    for (const theme of ['dark', 'light']) {
      if (!(await evaluate(`document.body.classList.contains('${theme}-mode')`))) await evaluate(`document.querySelector('app-theme-switcher button').click()`);
      await waitUntil(`document.body.classList.contains('${theme}-mode')`, `account fixture uses the existing ${theme} theme control`);
      for (const width of widths) {
        await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
        await evaluate(`document.querySelector('.profile-hero').scrollIntoView({ block: 'start', behavior: 'instant' })`);
        let layout;
        let previous;
        await waitUntil(async () => {
          layout = await evaluate(`({ width: innerWidth, height: innerHeight, documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
            theme: document.body.classList.contains('light-mode') ? 'light' : 'dark', titleWidth: document.querySelector('.profile-hero__title').getBoundingClientRect().width,
            avatarWidth: document.querySelector('.profile-avatar, .profile-avatar--placeholder')?.getBoundingClientRect().width ?? null })`);
          result.lastLayout = layout;
          const serialized = JSON.stringify(layout);
          const stable = previous === serialized;
          previous = serialized;
          return stable && layout.width === width && layout.theme === theme;
        }, `${label} ${fixture.kind} account layout settles at ${width}px (${theme})`);
        const issues = layout.documentWidth > width + 1 ? ['document horizontal overflow'] : [];
        const screenshot = `${label}_${fixture.kind}_account-profile_${width}_${theme}.webp`;
        const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
        await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
        result.layouts.push({ ...layout, screenshot, issues });
        if (label === 'v6' && issues.length) throw new Error(`Account profile layout requires correction: ${width}px ${theme}`);
      }
    }
    result.passed = true;
  } catch (error) {
    result.passed = false;
    result.error = error.message;
  } finally {
    closing = true;
    await cdp.send('Page.navigate', { url: 'about:blank' });
    for (let pass = 0; pending.size && pass < 16; pass++) await Promise.allSettled([...pending]);
    await cdp.send('Fetch.disable');
    stopInterception(); stopCancellation(); stopExceptions(); stopDocuments(); stopRetirement();
    if (injectedScript) await cdp.send('Page.removeScriptToEvaluateOnNewDocument', { identifier: injectedScript });
    await writeFile(join(outputRoot, `${label}_${fixture.kind}_account-report.json`), JSON.stringify(result, null, 2));
  }
  if (!result.passed) throw new Error(`Isolated account layout ${label}/${fixture.kind}: ${result.error}`);
  return result;
}
