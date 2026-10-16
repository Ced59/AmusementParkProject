import { readFile, writeFile } from 'node:fs/promises';
import { collectAdminShellLayout, adminShellLayoutIssues } from './browser-admin-shell.mjs';
import { exercisePublicFitForm } from './exercise-public-fit-form.mjs';
import { settleAccountDocument } from './settle-account-document.mjs';
import { extname, join, resolve, sep } from 'node:path';
import { accountLayoutFixtureOrigin, accountLayoutRefreshMarker, accountLayoutFixtureResponse, accountLayoutResetQuery } from './account-layout-fixtures.mjs';
import { isCanceledInterceptionError, isRetiredDocumentInterceptionError, advanceMainDocument, forgetRetiredDocumentOwners } from './browser-interception-errors.mjs';
import { exerciseAccountLists } from './exercise-account-lists.mjs';
import { exerciseAccountProfileControls } from './exercise-account-profile-controls.mjs';
import { exerciseAccountPassportAndFit } from './exercise-account-passport-and-fit.mjs';
import { collectContrastCandidates } from './browser-text-contrast.mjs';
import { exerciseAccountAuthPages } from './exercise-account-auth-pages.mjs';
import { exerciseAccountNotifications } from './exercise-account-notifications.mjs';
import { exerciseAccountTripPages } from './exercise-account-trip-pages.mjs';
import { accountComparisonFixtureToken } from './account-passport-statistics-fixtures.mjs';
import { exerciseAccountPassportStatistics } from './exercise-account-passport-statistics.mjs';
import { exerciseAccountVisitEditor } from './exercise-account-visit-editor.mjs';
import { anonymousDraftIds } from './account-anonymous-draft-fixtures.mjs';
import { exerciseAnonymousDraftPages } from './exercise-anonymous-draft-pages.mjs';
import { historyLayoutDocuments } from './public-history-layout-fixtures.mjs';
import { exerciseIsolatedHistoryPages } from './exercise-isolated-history-pages.mjs';
import { accountContrastIssue, accountLayoutMustStop, accountLayoutsHaveFailure } from './account-layout-acceptance.mjs';
import { positionAccountCaptureTarget } from './browser-account-target.mjs';

export function accountLayoutCaptureAllowed(ci, platform) {
  return ci === 'true' && platform === 'linux';
}

// Compiled application, existing guards/services, synthetic HTTP input only.
// This is UI acceptance, never proof of authentication or a private API call.
export async function captureAccountLayout({ cdp, browserRoot, outputRoot, widths, fixture, label, homeFixtures = new Map(), deferContrastFailure = false }) {
  if (!accountLayoutCaptureAllowed(process.env.CI, process.platform)) throw new Error('Account layout capture requires isolated Linux CI.');
  if (typeof deferContrastFailure !== 'boolean' || (deferContrastFailure && process.env.V6_ISOLATED_PREFLIGHT !== 'true')) throw new Error('Deferred contrast findings are restricted to the explicit diagnostic preflight.');
  if (!['master', 'v6'].includes(label) || !['standard', 'incomplete', 'expired', 'admin-shell'].includes(fixture.kind)) throw new Error('Unsupported account layout capture case.');
  const language = fixture.language;
  if (!['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'].includes(language)) throw new Error('Unsupported account fixture language.');
  const profilePath = `/${language}/profile`;
  const fitPath = `${profilePath}/park-fit/profiles`;
  const homePath = `/${language}/home`;
  const confirmPath = `/${language}/confirm-account`;
  const resetPath = `/${language}/reset-password`;
  const recoveryPath = `/${language}/forgot-password`;
  const tripPath = `${profilePath}/trips/${fixture.tripData.plan.tripPlanId}`;
  const tripDocuments = ['', '/preferences', '/preference-summary', '/coherence', '/activity', '/export', '/passport'].map(suffix => tripPath + suffix);
  const passportData = fixture.passportStatisticsData;
  const passportDocuments = ['/statistics', '/statistics/history', `/items/${passportData.itemId}`, `/parks/${passportData.parkId}`,
    '/years/2026', '/share', `/comparisons/invitations/${accountComparisonFixtureToken}`].map(suffix => `${profilePath}/passport${suffix}`);
  const visitDocuments = fixture.visitEditorData.map(data => `${profilePath}/visits/${data.visit.id}`);
  const anonymousPath = `/${language}/passport/local`;
  const anonymousDocuments = [anonymousPath, ...anonymousDraftIds.map(id => `${anonymousPath}/${id}`)];
  const historyDocuments = historyLayoutDocuments(language);
  const publicFitDocuments = ['', '/results', '/compare'].map(suffix => `/${language}/park-fit${suffix}`);
  const unauthenticatedDocuments = [...anonymousDocuments, ...publicFitDocuments, ...new Set(historyDocuments.map(path => path.split('?')[0]))];
  const accountDocuments = [profilePath, fitPath, confirmPath, resetPath, recoveryPath, ...tripDocuments, ...passportDocuments, ...visitDocuments, ...anonymousDocuments, ...historyDocuments, ...publicFitDocuments];
  if (fixture.kind === 'expired' && (!(homeFixtures instanceof Map) || !homeFixtures.size)) throw new Error('Expired account UI requires exact local public-home fixtures.');
  if (!Array.isArray(widths) || widths.length < 1 || widths.length > 13 || widths.some(width => !Number.isInteger(width) || width < 320 || width > 1920)) throw new Error('Account capture widths exceed their bound.');
  const root = resolve(browserRoot);
  const rawHtml = await readFile(join(root, 'index.csr.html'), 'utf8');
  const result = { scope: 'compiled public/account UI with CI-only synthetic input; not authentication/API/HTTP-SEO acceptance', label,
    kind: fixture.kind, language, deferContrastFailure, checks: [], layouts: [], apiReads: [], blockedRequests: [], fixtureFailures: [], exceptions: [] };
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
      const response = accountLayoutFixtureResponse(request, fixture, homeFixtures);
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
      if (!((url.pathname === profilePath || (fixture.kind !== 'expired' && accountDocuments.includes(url.pathname))) && !url.search)
          && !(fixture.kind !== 'expired' && historyDocuments.includes(url.pathname + url.search))
          && !(fixture.kind === 'standard' && url.pathname === resetPath && url.search === accountLayoutResetQuery)
          && !(fixture.kind === 'expired' && url.pathname === homePath && url.searchParams.size === 1 && url.searchParams.get('returnUrl') === profilePath)) throw new Error('Unexpected account fixture document navigation.');
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
  const stopRetirement = cdp.on('Page.frameNavigated', ({ frame }) => {
    mainDocument = advanceMainDocument(mainDocument, frame, documents, retired);
    forgetRetiredDocumentOwners(documents, retired);
  });
  const stopExceptions = cdp.on('Runtime.exceptionThrown', ({ exceptionDetails }) => result.exceptions.push({ text: exceptionDetails.text, line: exceptionDetails.lineNumber }));
  const stopInterception = cdp.on('Fetch.requestPaused', request => {
    const task = intercept(request).catch(async error => {
      if (error.message === 'Invalid InterceptionId.' && request.networkId) await new Promise(yes => setTimeout(yes, 100));
      if (!isCanceledInterceptionError(error, request.networkId, canceled) && !isRetiredDocumentInterceptionError(error, request.networkId, retired)) {
        result.fixtureFailures.push({ method: request.request.method, path: new URL(request.request.url).pathname, reason: error.message.slice(0, 1000),
          requestId: request.requestId, networkId: request.networkId ?? null, frameId: request.frameId ?? null, resourceType: request.resourceType,
          requestOwner: documents.get(request.networkId) ?? null, mainDocument,
          nativeCancellationObserved: canceled.has(request.networkId), nativeRetirementObserved: retired.has(request.networkId) });
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
      `if (location.origin === ${JSON.stringify(accountLayoutFixtureOrigin)}) { if (${JSON.stringify(unauthenticatedDocuments)}.includes(location.pathname)) localStorage.removeItem(${JSON.stringify(accountLayoutRefreshMarker)}); else localStorage.setItem(${JSON.stringify(accountLayoutRefreshMarker)}, 'true'); }` })).identifier;
    await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] });
    await cdp.send('Page.navigate', { url: accountLayoutFixtureOrigin + profilePath });
    const displayName = fixture.user.publicDisplayName || fixture.user.firstName || fixture.user.email;
    if (fixture.kind === 'expired') {
      await waitUntil(`location.pathname === ${JSON.stringify(homePath)} && new URLSearchParams(location.search).get('returnUrl') === ${JSON.stringify(profilePath)} && !!document.querySelector('app-auth-modal #loginEmail') && !document.querySelector('.profile-hero')`, 'expired local refresh retains the real guard redirect, return URL and login dialog without private profile content');
      await waitUntil(`localStorage.getItem(${JSON.stringify(accountLayoutRefreshMarker)}) === null`, 'expired local refresh clears the existing session marker');
    } else {
      await waitUntil(`document.querySelector('.profile-hero__title')?.textContent.trim() === ${JSON.stringify(displayName)}`, 'existing guard and refresh flow render the local account fixture');
      if (fixture.kind === 'admin-shell') {
        await waitUntil(`document.querySelectorAll('a[href="/${language}/admin"]').length === 2 && !!document.querySelector('.admin-public-view-toolbar')`, 'local admin presentation exposes the two existing navigation entries and lazy toolbar');
      } else {
        await waitUntil(`!document.querySelector('a[href="/${language}/admin"]')`, 'standard account fixture does not expose the conditional admin entry');
      }
      await evaluate(`document.querySelector('.app-cookie-consent__button--ghost')?.click()`);
      await waitUntil(`!document.querySelector('.app-cookie-consent')`, 'account layout uses the real necessary-cookie control');
      await evaluate(`document.querySelector('.profile-row--identity > button').click()`);
      await waitUntil(`!!document.getElementById('profile-firstname') && document.getElementById('profile-firstname').value === ${JSON.stringify(fixture.user.firstName)}`, 'identity editor retains the exact fixture fields without submitting');
      await evaluate(`document.querySelector('.profile-actions--editing button:last-child').click()`);
      await waitUntil(`!document.querySelector('.profile-identity-editor') && document.querySelector('.profile-hero__title').textContent.trim() === ${JSON.stringify(displayName)}`, 'identity cancellation restores the same visible account');
    }
    const captureLayouts = async (selector, page) => {
      for (const theme of ['dark', 'light']) {
        if (!(await evaluate(`document.body.classList.contains('${theme}-mode')`))) {
          if (fixture.kind === 'expired') {
            await evaluate(`document.querySelector('.p-dialog-header-close').click()`);
            await waitUntil(`!document.querySelector('app-auth-modal')`, 'expired account dialog closes before using background theme navigation');
          }
          await evaluate(`document.querySelector('app-theme-switcher button').click()`);
          if (fixture.kind === 'expired') {
            await evaluate(`document.querySelector('.app-public-nav__right .btn-nav--primary').click()`);
            await waitUntil(`!!document.querySelector('app-auth-modal #loginEmail') && new URLSearchParams(location.search).get('returnUrl') === ${JSON.stringify(profilePath)}`, 'expired account login dialog reopens with the same return URL after the real theme change');
          }
        }
        await waitUntil(`document.body.classList.contains('${theme}-mode')`, `account fixture uses the existing ${theme} theme control`);
        await settleAccountDocument({ evaluate, pending });
        for (const width of widths) {
          await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
          let layout;
          let previous;
          await waitUntil(async () => {
            const target = await evaluate(`(${positionAccountCaptureTarget.toString()})(${JSON.stringify(selector)})`);
            layout = await evaluate(`({ width: innerWidth, height: innerHeight, documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
              theme: document.body.classList.contains('light-mode') ? 'light' : 'dark', titleWidth: (document.querySelector('.app-account-layout__content h1') ?? document.querySelector('h1'))?.getBoundingClientRect().width ?? null,
              avatarWidth: document.querySelector('.profile-avatar, .profile-avatar--placeholder')?.getBoundingClientRect().width ?? null,
              dialog: (() => { const node = document.querySelector('.p-dialog'); if (!node) return null; const box = node.getBoundingClientRect();
                const close = node.querySelector('.p-dialog-header-close'); const rect = close.getBoundingClientRect();
                return { x: box.x, y: box.y, right: box.right, bottom: box.bottom, closeUnobscured: close.contains(document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2)) }; })() })`);
            layout.target = target;
            result.lastLayout = layout;
            const serialized = JSON.stringify(layout);
            const stable = previous === serialized;
            previous = serialized;
            return stable && layout.width === width && layout.theme === theme && layout.target.visible;
          }, `${label} ${fixture.kind} ${page} layout settles at ${width}px (${theme})`);
          const issues = layout.documentWidth > width + 1 ? ['document horizontal overflow'] : [];
          if (fixture.kind === 'admin-shell') {
            layout.adminShell = await evaluate(`(${collectAdminShellLayout.toString()})()`);
            issues.push(...adminShellLayoutIssues(layout.adminShell));
          }
          if (layout.target.x < -1 || layout.target.right > width + 1) issues.push('captured target horizontal bounds');
          if (page === 'isolated-history-comparison' && width > 520) {
            layout.yearFields = await evaluate(`['comparison-from', 'comparison-to'].map(id => { const input = document.getElementById(id); return input ? { top: input.getBoundingClientRect().top, width: input.getBoundingClientRect().width } : null; })`);
            if (layout.yearFields.some(field => !field || field.width <= 0) || Math.abs(layout.yearFields[0].top - layout.yearFields[1].top) > 1) issues.push('historical comparison year fields not aligned');
          }
          if ([390, 1366].includes(width)) {
            layout.contrast = await evaluate(`(${collectContrastCandidates.toString()})({ includeCoverage: true })`);
            if (!layout.contrast.checked) issues.push('no measurable visible flat text for the account contrast probe');
            if (layout.contrast.warnings.length) issues.push(accountContrastIssue);
          }
          if (page === 'account-expired' && (!layout.dialog || layout.dialog.x < -1 || layout.dialog.y < -1
              || layout.dialog.right > width + 1 || layout.dialog.bottom > layout.height + 1 || !layout.dialog.closeUnobscured)) issues.push('expired login dialog bounds or close action');
          const screenshot = `${label}_${fixture.kind}_${language}_${page}_${width}_${theme}.webp`;
          const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
          await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
          result.layouts.push({ ...layout, page, screenshot, issues });
          if (accountLayoutMustStop(label, issues, deferContrastFailure)) throw new Error(`Account ${page} layout requires correction: ${width}px ${theme}`);
        }
      }
    };
    if (fixture.kind === 'expired') {
      await captureLayouts('.p-dialog', 'account-expired');
      if (result.apiReads.some(read => read.path.startsWith('/api/users/') || read.path.startsWith('/api/me/'))) throw new Error('Expired layout unexpectedly requested private profile data.');
      result.expiredSession = { localRefreshRejected: true, guardRedirectExercised: true, realAuthIntegrationExercised: false };
    } else if (fixture.kind === 'admin-shell') {
      await captureLayouts('.profile-hero', 'account-admin-shell');
      if (result.apiReads.some(read => read.path.startsWith('/api/admin/'))) throw new Error('Admin shell presentation unexpectedly requested an administration API.');
      result.adminShell = { presentationRoleOnly: true, serverAuthorizationExercised: false, adminWorkflowsExercised: false };
    } else {
      const navigateDocument = async path => {
        if (!accountDocuments.includes(path) && path !== resetPath + accountLayoutResetQuery) throw new Error('Unknown isolated account deep-link recipe.');
        await settleAccountDocument({ evaluate, pending });
        await cdp.send('Page.navigate', { url: accountLayoutFixtureOrigin + path });
      };
      await captureLayouts('.profile-hero', 'account-profile');
      await exerciseAccountProfileControls({ evaluate, waitUntil, captureLayouts, fixture, result });
      await exerciseAccountLists({ evaluate, waitUntil, captureLayouts, fixture, result, language });
      await exerciseAccountPassportAndFit({ evaluate, waitUntil, captureLayouts, fixture, result, language, navigateDocument });
      await exerciseAccountNotifications({ evaluate, waitUntil, captureLayouts, fixture, result, language });
      await exerciseAccountTripPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language });
      await exerciseAccountPassportStatistics({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language });
      await exerciseAccountVisitEditor({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language });
      if (fixture.kind === 'standard') {
        const dictionary = JSON.parse(await readFile(join(root, `assets/i18n/${language}.json`), 'utf8'));
        const expectedConfirmationMessage = dictionary.auth.confirm.invalidLinkMessage ?? 'Le lien de confirmation est invalide.';
        await exerciseAccountAuthPages({ evaluate, waitUntil, captureLayouts, fixture, result, navigateDocument, expectedConfirmationMessage });
      }
      await exerciseAnonymousDraftPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language });
      await exercisePublicFitForm({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result });
      await exerciseIsolatedHistoryPages({ evaluate, waitUntil, captureLayouts, navigateDocument, fixture, result, language });
    }
    if (result.fixtureFailures.length || result.exceptions.length) throw new Error('Account layout accumulated an unexpected fixture or browser failure.');
    if (accountLayoutsHaveFailure(label, result.layouts)) throw new Error('Account layout accumulated visual findings; inspect every layouts[].issues and contrast.warnings.');
    result.passed = true;
  } catch (error) {
    result.passed = false;
    result.error = error.message;
    try {
      result.lastDocument = await evaluate(`({ path: location.pathname, query: location.search, hasLogin: !!document.querySelector('app-auth-modal'), hasProfile: !!document.querySelector('.profile-hero'), occurrenceCount: document.querySelectorAll('.passport-occurrence').length, hasMoreOccurrences: !!document.querySelector('.passport-timeline__more') })`);
      const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      result.failureScreenshot = `${label}_${fixture.kind}_${language}_account-failure.webp`;
      await writeFile(join(outputRoot, result.failureScreenshot), Buffer.from(pixels.data, 'base64'));
    } catch { /* Keep the original failed check if the browser is unavailable. */ }
  } finally {
    closing = true;
    await cdp.send('Page.navigate', { url: 'about:blank' });
    for (let pass = 0; pending.size && pass < 16; pass++) await Promise.allSettled([...pending]);
    await cdp.send('Fetch.disable');
    stopInterception(); stopCancellation(); stopExceptions(); stopDocuments(); stopRetirement();
    if (injectedScript) await cdp.send('Page.removeScriptToEvaluateOnNewDocument', { identifier: injectedScript });
    if (result.fixtureFailures.length || result.exceptions.length) { result.passed = false; result.error ??= 'Unexpected account fixture or browser failure during cleanup.'; }
    await writeFile(join(outputRoot, `${label}_${fixture.kind}_${language}_account-report.json`), JSON.stringify(result, null, 2));
  }
  if (!result.passed) throw new Error(`Isolated account layout ${label}/${fixture.kind}: ${result.error}`);
  return result;
}
