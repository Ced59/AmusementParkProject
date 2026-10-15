import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { extname, join, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { isCanceledInterceptionError } from './browser-interception-errors.mjs';

// Initial SSR snapshots plus a CI-only anonymous hydration smoke. This tool never
// starts a server, authenticates, performs remote writes or replaces full acceptance.
const publicOrigin = 'https://amusement-parks.fun';
const browserRoot = resolve('dist/amusement-park/browser');
const outputRoot = resolve(process.env.V6_SSR_EVIDENCE_DIR ?? '../../docs/design-v6/evidence/public-ssr');
const widths = (process.env.V6_SSR_WIDTHS ?? '320,360,390,414,480,680,768,960,1024,1180,1366,1440,1920')
  .split(',').map(Number);
if (widths.some((width) => !Number.isInteger(width) || width < 320 || width > 1920)) {
  throw new Error('Screenshot widths must be integers between 320 and 1920.');
}
const paths = process.env.V6_SSR_PATHS?.split(',') ?? [
  '/',
  ...['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'].map((language) => `/${language}/home`),
  '/fr/parks',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/item/088b092c-808f-4eba-9407-34141caa94dc/rhaegal',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/item/c01aa022-260c-424b-897a-793e1c406421/l-epee-d-odin',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/item/d20e775e-1beb-48ce-919d-8f55f586cb31/la-catapulte',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/item/8b479fdb-4e5d-4b05-8af1-420b07c3689f/la-creperie',
  '/fr/attraction/46a57823-75cd-4ff3-9fde-495ad0204b1b/hasenhorn-coaster',
  '/fr/attraction/46a57823-75cd-4ff3-9fde-495ad0204b1b/hasenhorn-coaster/history',
  ...['items', 'zones', 'map', 'images', 'videos', 'weather', 'opening-hours', 'pricing', 'comments', 'history']
    .map((suffix) => `/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/${suffix}`),
  '/fr/rankings', '/fr/rankings/methodology', '/fr/manufacturers', '/fr/technical', '/fr/versions',
  '/fr/park/94a4c4df-5ab6-4dc3-b756-2de664e947c4/efteling',
  '/fr/park/94a4c4df-5ab6-4dc3-b756-2de664e947c4/efteling/pricing',
  '/fr/about', '/fr/contact', '/fr/privacy', '/fr/not-found',
];
if (paths.some((path) => path !== '/' && (!/^\/(fr|en|de|nl|it|es|pt|pl)\//.test(path) || /\/admin|\/profile/.test(path)))) {
  throw new Error('Only anonymous localized public routes are accepted.');
}

const chromeCandidates = [
  process.env.CHROME_PATH,
  process.env.PROGRAMFILES && join(process.env.PROGRAMFILES, 'Google/Chrome/Application/chrome.exe'),
  process.env['PROGRAMFILES(X86)'] && join(process.env['PROGRAMFILES(X86)'], 'Google/Chrome/Application/chrome.exe'),
  'google-chrome-stable', 'google-chrome', 'chromium', 'chromium-browser',
].filter(Boolean);
const chromeExecutable = chromeCandidates.find((candidate) => candidate.includes('/') || candidate.includes('\\')
  ? existsSync(candidate)
  : spawnSync(candidate, ['--version'], { encoding: 'utf8' }).status === 0);
if (!chromeExecutable) throw new Error('A Chromium executable is required.');

const nativeFetch = globalThis.fetch;
let reads = [];
globalThis.fetch = async (input, options = {}) => {
  const request = new Request(input, options);
  const url = new URL(request.url);
  if (!['GET', 'HEAD'].includes(request.method)) throw new Error('The evidence tool rejects remote writes.');
  if (url.origin !== 'http://api:8080' && !(url.origin === publicOrigin && url.pathname.startsWith('/api/'))) {
    throw new Error(`Unexpected SSR network origin: ${url.origin}`);
  }
  const target = url.origin === 'http://api:8080' ? new URL(`/api${url.pathname}${url.search}`, publicOrigin) : url;
  const headers = new Headers(request.headers);
  for (const name of ['authorization', 'cookie', 'X-AmusementPark-Internal-SSR']) headers.delete(name);
  const response = await nativeFetch(target, { method: request.method, headers, signal: AbortSignal.timeout(20000) });
  reads.push({ path: target.pathname, status: response.status });
  return response;
};

function assetUrl(value) {
  if (value.startsWith('/api/')) return `${publicOrigin}${value}`;
  if (value.startsWith('/assets/')) return pathToFileURL(join(browserRoot, value.slice(1))).href;
  return value;
}

async function staticDocument(html) {
  const cssPaths = [...html.matchAll(/<link\b[^>]*rel="stylesheet"[^>]*href="([^"]+)"[^>]*>/gi)].map((match) => match[1]);
  // Angular may serialize href before rel.
  for (const match of html.matchAll(/<link\b[^>]*href="([^"]+)"[^>]*rel="stylesheet"[^>]*>/gi)) cssPaths.push(match[1]);
  const sheets = [];
  for (const path of new Set(cssPaths)) {
    if (!/^(?:\/)?styles[-.\w]*\.css$/.test(path)) throw new Error(`Unexpected stylesheet: ${path}`);
    sheets.push(await readFile(join(browserRoot, path.replace(/^\//, '')), 'utf8'));
  }
  let result = html.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, '')
    .replace(/<base\b[^>]*>/gi, '')
    .replace(/<link\b[^>]*(?:rel="(?:stylesheet|modulepreload)"|as="script")[^>]*>/gi, '')
    .replace('</head>', `<base href="${pathToFileURL(`${browserRoot}/`).href}"><style>${sheets.join('\n')}</style></head>`);
  result = result.replace(/\b(src|href)="([^"]+)"/g, (_, name, value) => `${name}="${assetUrl(value)}"`)
    .replace(/\bsrcset="([^"]+)"/g, (_, value) => `srcset="${value.split(',').map((candidate) => candidate.trim().split(/\s+/).map((part, index) => index === 0 ? assetUrl(part) : part).join(' ')).join(', ')}"`)
    .replace(/url\((['"]?)(\/assets\/[^)'"\s]+)\1\)/g, (_, quote, value) => `url("${assetUrl(value)}")`);
  return result;
}

// Diagnostic candidates, not a blanket AA verdict: raster imagery, overlays,
// authored rich text and other complex backgrounds still need manual review.
function collectContrastCandidates() {
  const parseColor = (value) => {
    if (!/^rgba?\(|^color\(srgb /.test(value)) return null;
    const channels = value.match(/[\d.]+/g)?.map(Number);
    if (!channels || channels.length < 3) return null;
    return [...channels.slice(0, 3).map((channel) => value.startsWith('color(') ? channel : channel / 255), channels[3] ?? 1];
  };
  const over = (front, back) => front.slice(0, 3).map((channel, index) => channel * front[3] + back[index] * (1 - front[3]));
  const luminance = (channels) => channels.slice(0, 3).map((channel) => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
    .reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
  const warnings = [];
  const seen = new Set();
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.textContent.trim();
    const element = node.parentElement;
    if (!text || !element || element.closest('script,style,svg,.app-brand-wordmark,.language-entry__playground-mark,[aria-hidden="true"],[aria-disabled="true"],button:disabled')) continue;
    const bounds = element.getBoundingClientRect();
    if (bounds.width === 0 || bounds.height === 0 || bounds.bottom < 0 || bounds.top > innerHeight) continue;
    const style = getComputedStyle(element);
    const foreground = parseColor(style.color);
    if (!foreground || foreground[3] < 0.1) continue;
    const ancestors = [];
    for (let parent = element; parent; parent = parent.parentElement) ancestors.push(parent);
    if (ancestors.some((parent) => {
      const css = getComputedStyle(parent);
      const hiddenDetailsContent = parent.tagName === 'DETAILS' && !parent.open
        && !parent.querySelector(':scope > summary')?.contains(element);
      return hiddenDetailsContent || css.display === 'none' || css.visibility !== 'visible'
        || css.backgroundImage !== 'none' || Number(css.opacity) < 0.99;
    })) continue;
    let background = [1, 1, 1];
    for (const parent of ancestors.reverse()) {
      const color = parseColor(getComputedStyle(parent).backgroundColor);
      if (color) background = over(color, background);
    }
    const foregroundLuminance = luminance(over(foreground, background));
    const backgroundLuminance = luminance(background);
    const ratio = (Math.max(foregroundLuminance, backgroundLuminance) + 0.05) / (Math.min(foregroundLuminance, backgroundLuminance) + 0.05);
    const size = Number.parseFloat(style.fontSize);
    const required = size >= 24 || (size >= 18.666 && Number(style.fontWeight) >= 700) ? 3 : 4.5;
    const selector = `${element.tagName.toLowerCase()}.${[...element.classList].join('.')}`;
    const key = `${selector}:${style.color}:${text}`;
    if (ratio + 0.01 < required && !seen.has(key)) {
      seen.add(key);
      warnings.push({ selector, text: text.slice(0, 160), foreground: style.color, background, ratio: Number(ratio.toFixed(2)), required, fontSize: size });
    }
  }
  return warnings;
}

async function connect(webSocketUrl) {
  const socket = new WebSocket(webSocketUrl);
  await new Promise((yes, no) => { socket.addEventListener('open', yes, { once: true }); socket.addEventListener('error', no, { once: true }); });
  let sequence = 0;
  const pending = new Map();
  const listeners = new Map();
  socket.addEventListener('message', ({ data }) => {
    const message = JSON.parse(data);
    if (!message.id) {
      for (const listener of listeners.get(message.method) ?? []) listener(message.params);
      return;
    }
    const handler = pending.get(message.id);
    pending.delete(message.id);
    message.error ? handler?.reject(new Error(message.error.message)) : handler?.resolve(message.result);
  });
  socket.addEventListener('close', () => {
    for (const handler of pending.values()) handler.reject(new Error('Chromium closed its connection.'));
    pending.clear();
  });
  return {
    close: () => socket.close(),
    on: (method, listener) => {
      const handlers = listeners.get(method) ?? new Set();
      handlers.add(listener);
      listeners.set(method, handlers);
      return () => handlers.delete(listener);
    },
    send: (method, params = {}) => new Promise((resolveRequest, rejectRequest) => {
      const id = ++sequence;
      const timer = setTimeout(() => { pending.delete(id); rejectRequest(new Error(`CDP timeout: ${method}`)); }, 30000);
      pending.set(id, { resolve: (value) => { clearTimeout(timer); resolveRequest(value); }, reject: (error) => { clearTimeout(timer); rejectRequest(error); } });
      socket.send(JSON.stringify({ id, method, params }));
    }),
  };
}

async function captureHydratedHome(rawHtml) {
  const origin = 'https://apf-review.invalid';
  const result = { scope: 'anonymous hydrated home smoke; isolated browser origin', apiReads: [], blockedRequests: [], exceptions: [], checks: [], captures: [] };
  report.hydratedHome = result;
  const mimeTypes = { '.mjs': 'text/javascript', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.svg': 'image/svg+xml', '.webp': 'image/webp', '.png': 'image/png', '.ico': 'image/x-icon' };
  const interceptionErrors = [];
  result.interceptionErrors = interceptionErrors;
  const canceledNetworkRequests = new Set();
  const pendingInterceptions = new Set();
  const stopCancellations = cdp.on('Network.loadingFailed', ({ requestId, canceled }) => {
    if (canceled) canceledNetworkRequests.add(requestId);
  });
  const fulfill = (requestId, status, contentType, bytes) => cdp.send('Fetch.fulfillRequest', {
    requestId, responseCode: status,
    responseHeaders: [{ name: 'Content-Type', value: contentType }, { name: 'Cache-Control', value: 'no-store' }],
    body: Buffer.from(bytes).toString('base64'),
  });
  const intercept = async ({ requestId, request, resourceType }) => {
    const url = new URL(request.url);
    if (!['GET', 'HEAD'].includes(request.method) || ![origin, publicOrigin].includes(url.origin)) {
      result.blockedRequests.push({ method: request.method, origin: url.origin, path: url.pathname });
      await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' });
      return;
    }
    if (url.pathname.startsWith('/api/')) {
      const target = new URL(`${url.pathname}${url.search}`, publicOrigin);
      if (target.pathname === '/api/search' && target.searchParams.get('query') === 'APF-READONLY-ERROR-FIXTURE') {
        (result.injectedFailures ??= []).push({ path: target.pathname, query: target.search, status: 503, scope: 'isolated browser only; not sent to production' });
        await fulfill(requestId, 503, 'application/problem+json', JSON.stringify({ type: 'about:blank', title: 'Read-only review fixture', status: 503 }));
        return;
      }
      const response = await nativeFetch(target, { method: request.method, signal: AbortSignal.timeout(20000) });
      result.apiReads.push({ path: target.pathname, query: target.search, status: response.status });
      await fulfill(requestId, response.status, response.headers.get('content-type') ?? 'application/octet-stream', await response.arrayBuffer());
      return;
    }
    if (resourceType === 'Document' && url.origin === origin && url.pathname === '/fr/home') {
      await fulfill(requestId, 200, 'text/html; charset=utf-8', rawHtml);
      return;
    }
    const assetPath = resolve(browserRoot, `.${decodeURIComponent(url.pathname)}`);
    if (!assetPath.startsWith(`${browserRoot}${sep}`)) throw new Error('Rejected asset traversal.');
    try {
      await fulfill(requestId, 200, mimeTypes[extname(assetPath)] ?? 'application/octet-stream', await readFile(assetPath));
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
      await fulfill(requestId, 404, 'text/plain', 'No review asset at this path.');
    }
  };
  const stopInterception = cdp.on('Fetch.requestPaused', (request) => {
    const task = intercept(request).catch(async (error) => {
      // CDP can deliver loadingFailed just after the failed fulfillment response.
      if (error.message === 'Invalid InterceptionId.' && request.networkId) {
        await new Promise((yes) => setTimeout(yes, 100));
      }
      if (isCanceledInterceptionError(error, request.networkId, canceledNetworkRequests)) {
        (result.canceledRequests ??= []).push({ path: new URL(request.request.url).pathname, networkId: request.networkId });
      } else {
        interceptionErrors.push(error.message);
      }
    }).finally(() => pendingInterceptions.delete(task));
    pendingInterceptions.add(task);
  });
  const stopExceptions = cdp.on('Runtime.exceptionThrown', ({ exceptionDetails }) => {
    result.exceptions.push((exceptionDetails.exception?.description ?? exceptionDetails.text).slice(0, 1500));
  });
  const evaluate = async (expression) => {
    const evaluation = await cdp.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (evaluation.exceptionDetails) throw new Error(evaluation.exceptionDetails.text);
    return evaluation.result.value;
  };
  const waitUntil = async (expression, label) => {
    for (let attempt = 0; attempt < 100; attempt++) {
      if (interceptionErrors.length) throw new Error(interceptionErrors.join('; '));
      if (await evaluate(expression)) { result.checks.push(label); return; }
      await new Promise((yes) => setTimeout(yes, 200));
    }
    throw new Error(`Hydrated browser check timed out: ${label}`);
  };
  try {
    await cdp.send('Network.enable');
    await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    await cdp.send('Emulation.setDeviceMetricsOverride', { width: 390, height: 1200, deviceScaleFactor: 1, mobile: false });
    await cdp.send('Page.navigate', { url: `${origin}/fr/home` });
    await waitUntil(`location.origin === '${origin}' && document.readyState === 'complete' && document.querySelector('app-root') && !document.querySelector('app-root').hasAttribute('ngh') && !!document.querySelector('#home-search-input')`, 'Angular root hydrated');
    await evaluate(`document.querySelector('.app-cookie-consent__button--ghost')?.click()`);
    await waitUntil(`!document.querySelector('.app-cookie-consent')`, 'necessary-cookie choice handled by Angular');
    const initialTheme = await evaluate(`document.body.classList.contains('dark-mode') ? 'dark' : 'light'`);
    await evaluate(`document.querySelector('app-theme-switcher button').click()`);
    await waitUntil(`document.body.classList.contains('${initialTheme === 'dark' ? 'light' : 'dark'}-mode')`, 'theme button handled by Angular');
    for (const width of [390, 1366]) {
      await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
      const screenshot = `fr_home_hydrated_${width}.webp`;
      const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
      result.captures.push(screenshot);
    }
    await evaluate(`(() => { const input = document.querySelector('#home-search-input'); input.value = 'Fleury'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.app-search-panel__submit button').click(); })()`);
    await waitUntil(`!!document.querySelector('.home-results a[href*="/le-fleury"]')`, 'real public search returns Le Fleury');
    const screenshot = 'fr_home_hydrated_search_1366.webp';
    const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
    await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
    result.captures.push(screenshot);
    await evaluate(`(() => { const input = document.querySelector('#home-search-input'); input.value = 'park'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.app-search-panel__submit button').click(); })()`);
    await waitUntil(`document.querySelectorAll('.home-results .p-paginator-page').length > 1`, 'real public search offers multiple pages');
    await evaluate(`[...document.querySelectorAll('.home-results .p-paginator-page')].find(button => button.textContent.trim() === '2').click()`);
    await waitUntil(`document.querySelector('.home-results .p-paginator-page.p-highlight')?.textContent.trim() === '2' && !!document.querySelector('.home-results__grid app-ui-search-result-card')`, 'real search page two loaded');
    if (!result.apiReads.some(read => read.path === '/api/search' && new URLSearchParams(read.query).get('page') === '2')) throw new Error('Search pagination did not issue the real page-two API read.');
    for (const rows of [20, 10, 5]) {
      await evaluate(`(() => { const select = document.querySelector('.home-results .p-paginator-rpp-options'); const option = [...select.options].find(option => option.textContent.trim() === '${rows}'); if (!option) throw new Error('Missing page-size option'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
      await waitUntil(`document.querySelectorAll('.home-results__grid app-ui-search-result-card').length === ${rows} && document.querySelector('.home-results .p-paginator-page.p-highlight')?.textContent.trim() === '1'`, `real search page size ${rows} loaded and reset to page one`);
    }
    for (const [query, selector, label] of [
      ['zzv6qaf0fe25ac1be14b5cba302e46356ba3bb', '.home-results .app-page-state--empty', 'real no-result search shows the empty state'],
      ['APF-READONLY-ERROR-FIXTURE', '.home-results .app-page-state--error', 'isolated 503 fixture shows the real search error state'],
    ]) {
      await evaluate(`(() => { const input = document.querySelector('#home-search-input'); input.value = '${query}'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.app-search-panel__submit button').click(); })()`);
      await waitUntil(`!!document.querySelector('${selector}')`, label);
      const stateScreenshot = `fr_home_hydrated_${selector.endsWith('empty') ? 'empty' : 'error'}_1366.webp`;
      const statePixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, stateScreenshot), Buffer.from(statePixels.data, 'base64'));
      result.captures.push(stateScreenshot);
    }
    await evaluate(`document.querySelector('.app-public-nav__right .btn-nav--primary').click()`);
    await waitUntil(`!!document.querySelector('app-auth-modal #loginEmail') && !!document.querySelector('app-auth-modal #registerEmail')`, 'anonymous authentication dialog rendered both forms');
    for (const width of [390, 1366]) {
      await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
      const bounds = await evaluate(`(() => { const dialog = document.querySelector('.p-dialog'); const bounds = dialog.getBoundingClientRect(); return { viewport: innerWidth, left: bounds.left, right: bounds.right, width: bounds.width, scrollWidth: document.documentElement.scrollWidth }; })()`);
      if (bounds.left < -1 || bounds.right > width + 1 || bounds.scrollWidth > width + 1) throw new Error('Anonymous auth dialog overflows the viewport.');
      const authScreenshot = `fr_auth_dialog_anonymous_${width}.webp`;
      const authPixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, authScreenshot), Buffer.from(authPixels.data, 'base64'));
      result.captures.push(authScreenshot);
      (result.authDialogBounds ??= []).push(bounds);
    }
    await evaluate(`document.querySelector('.p-dialog-header-close').click()`);
    await waitUntil(`!document.querySelector('app-auth-modal')`, 'anonymous authentication dialog closed without submission');
    await evaluate(`(() => { const input = document.querySelector('#home-search-input'); input.value = 'Fleury'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.app-search-panel__submit button').click(); })()`);
    await waitUntil(`!!document.querySelector('.home-results a[href*="/le-fleury"]')`, 'Fleury detail link restored after pagination');
    await evaluate(`document.querySelector('.home-results a[href*="/le-fleury"]').click()`);
    await waitUntil(`location.pathname.endsWith('/le-fleury') && !!document.querySelector('app-park-detail-page h1')`, 'real result link opens the full park page');
    await evaluate(`history.back()`);
    await waitUntil(`location.pathname === '/fr/home' && !!document.querySelector('#home-search-input')`, 'browser back returns to the home route');
    if (result.exceptions.length || interceptionErrors.length) throw new Error('Hydrated browser raised an exception; inspect report.json.');
    console.log(`Hydrated home: ${result.checks.length} checks passed; production writes blocked.`);
  } finally {
    // Block new network work first, then drain existing intercepted reads.
    // Navigating away before draining invalidates otherwise valid Fetch IDs.
    // Keeping the block in place also prevents released requests from escaping
    // the read-only sandbox when Fetch.disable is called during cleanup.
    try { await cdp.send('Network.setBlockedURLs', { urls: ['*'] }); } catch (error) { result.exceptions.push(error.message); }
    for (let pass = 0; pass < 10 && pendingInterceptions.size; pass++) {
      await Promise.allSettled([...pendingInterceptions]);
    }
    if (pendingInterceptions.size) interceptionErrors.push('Interception cleanup did not finish.');
    try { await cdp.send('Fetch.disable'); } catch (error) { result.exceptions.push(error.message); }
    stopInterception();
    stopCancellations();
    stopExceptions();
    await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
    if (result.exceptions.length || interceptionErrors.length) throw new Error('Hydrated browser or interception failed; inspect report.json.');
  }
}

async function verifyFleuryItemRendering(application) {
  const parkPath = '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury';
  const inventoryResponse = await nativeFetch(`${publicOrigin}/api/park-items/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4?page=1&size=48&closedFilter=all`, { signal: AbortSignal.timeout(20000) });
  if (!inventoryResponse.ok) throw new Error('Could not read the public Fleury item inventory.');
  const inventory = await inventoryResponse.json();
  const total = inventory.pagination?.totalItems;
  if (!Number.isInteger(total) || total > 96) throw new Error('The public inventory exceeds this bounded review fixture; update its coverage explicitly.');
  const expected = [...inventory.data];
  if (total > 48) {
    const nextResponse = await nativeFetch(`${publicOrigin}/api/park-items/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4?page=2&size=48&closedFilter=all`, { signal: AbortSignal.timeout(20000) });
    if (!nextResponse.ok) throw new Error('Could not read the second public inventory page.');
    expected.push(...(await nextResponse.json()).data);
  }
  if (expected.length !== total || expected.some((item) => item.isVisible !== true)) throw new Error('The anonymous public fixture is incomplete or contains a non-public entity.');
  const linksById = new Map();
  for (let page = 1; page <= Math.max(1, Math.ceil(total / 48)); page++) {
    const response = await application.handle(new Request(`${publicOrigin}${parkPath}/items?size=48&page=${page}&closed=all`, { signal: AbortSignal.timeout(45000) }));
    const html = await response?.text();
    if (!html) throw new Error('No Angular SSR item-list content.');
    for (const match of html.matchAll(/href="(\/fr\/park\/[^"?#]+\/item\/([^/"?#]+)\/[^/"?#]+)"/g)) linksById.set(match[2], match[1]);
  }
  const result = { scope: 'initial SSR entity rendering; no interaction or HTTP-server acceptance', closedFilter: 'all', expectedVisibleCount: total, discoveredDetailLinks: linksById.size, items: [] };
  report.fleuryItemRendering = result;
  try {
    for (const item of expected) {
      const path = linksById.get(item.id);
      if (!path) throw new Error(`No actual Angular detail link for public item ${item.id}.`);
      const response = await application.handle(new Request(new URL(path, publicOrigin), { signal: AbortSignal.timeout(45000) }));
      const html = await response?.text();
      const heading = html?.match(/<h1\b[^>]*>([\s\S]*?)<\/h1>/i)?.[1] ?? '';
      const escapedName = item.name.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
      const hasEntityHeading = heading.includes(escapedName);
      result.items.push({ id: item.id, name: item.name, category: item.category, type: item.type, path, engineStatus: response?.status, hasEntityHeading });
      if (!hasEntityHeading) throw new Error(`Initial SSR item heading missing: ${item.name}.`);
    }
    console.log(`Fleury initial SSR: ${result.items.length}/${total} public item details contain their entity heading.`);
  } finally {
    await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
  }
}

const tempRoot = await mkdtemp(join(tmpdir(), 'apf-v6-ssr-'));
let chrome;
let cdp;
let entry;
const report = {
  scope: 'Anonymous public SSR snapshots and a CI-only hydrated home smoke; not full workflow acceptance',
  buildSha: process.env.GITHUB_SHA ?? 'UNVERIFIED_LOCAL_BUILD',
  sourceHeadSha: process.env.V6_SOURCE_HEAD_SHA ?? 'UNVERIFIED_LOCAL_BUILD',
  reducedMotion: true,
  generatedAt: new Date().toISOString(),
  routes: [],
};
try {
  await mkdir(outputRoot, { recursive: true });
  chrome = spawn(chromeExecutable, ['--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
    '--allow-file-access-from-files', '--remote-debugging-port=0', `--user-data-dir=${tempRoot}`, 'about:blank'], { stdio: 'ignore' });
  let port;
  for (let attempt = 0; attempt < 100; attempt++) {
    try { port = Number((await readFile(join(tempRoot, 'DevToolsActivePort'), 'utf8')).split('\n')[0]); break; } catch { await new Promise((yes) => setTimeout(yes, 100)); }
  }
  if (!port) throw new Error('Chromium did not start.');
  const targets = await (await nativeFetch(`http://127.0.0.1:${port}/json/list`)).json();
  cdp = await connect(targets.find((target) => target.type === 'page').webSocketDebuggerUrl);
  await cdp.send('Page.enable');
  await cdp.send('Runtime.enable');
  await cdp.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
  entry = await import(pathToFileURL(resolve('dist/amusement-park/server/main.server.mjs')));
  const application = entry.ɵgetOrCreateAngularServerApp();
  let homeRawHtml;
  for (const path of paths) {
    reads = [];
    const response = await application.handle(new Request(new URL(path, publicOrigin), { signal: AbortSignal.timeout(45000) }));
    if (!response) throw new Error(`No Angular route matched ${path}`);
    const html = await response.text();
    if (path === '/fr/home') homeRawHtml = html;
    // The public Fleury pricing API genuinely returns 404. Production V5 renders
    // its existing empty-state h3, not an entity h1. Require both the API evidence
    // and the real empty component; this is not permission to accept blank pages.
    const expectedEmptyPricing = path === '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury/pricing'
      && reads.some((read) => read.path === '/api/parks/4e0155db-9dbf-4755-a32f-0cc8950d61c4/pricing' && read.status === 404)
      && /<app-park-pricing-page\b[\s\S]*?<app-empty-state\b[\s\S]*?<h3\b/i.test(html);
    if (!/<app-root\b/i.test(html) || (!/<h1\b/i.test(html) && !expectedEmptyPricing)) {
      await writeFile(join(outputRoot, 'failed-initial-content.html'), await staticDocument(html));
      throw new Error(`Missing initial Angular content for ${path}`);
    }
    if (!path.endsWith('/not-found') && /<app-public-not-found-page\b/i.test(html)) {
      throw new Error(`Unexpected not-found component in initial Angular content: ${path}`);
    }
    const routeName = path === '/' ? 'language-entry' : path.slice(1).replaceAll('/', '_');
    const htmlPath = join(outputRoot, `${routeName}.html`);
    await writeFile(htmlPath, await staticDocument(html));
    const headingPattern = expectedEmptyPricing ? /<h3\b[^>]*>([\s\S]*?)<\/h3>/i : /<h1\b[^>]*>([\s\S]*?)<\/h1>/i;
    const initialHeading = html.match(headingPattern)?.[1].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    const route = { path, engineStatus: response.status, initialState: expectedEmptyPricing ? 'empty-public-pricing-404' : 'content',
      initialHeading, initialHeadingLevel: expectedEmptyPricing ? 3 : 1, bytes: Buffer.byteLength(html), reads: [...reads], captures: [] };
    report.routes.push(route);
    const fileUrl = pathToFileURL(htmlPath).href;
    await cdp.send('Page.navigate', { url: fileUrl });
    let loaded = false;
    for (let attempt = 0; attempt < 100; attempt++) {
      const state = await cdp.send('Runtime.evaluate', { expression: `location.href === ${JSON.stringify(fileUrl)} && document.readyState === 'complete'`, returnByValue: true });
      if (state.result.value === true) { loaded = true; break; }
      await new Promise((yes) => setTimeout(yes, 100));
    }
    if (!loaded) throw new Error(`SSR snapshot did not finish loading: ${path}`);
    for (const width of widths) {
      await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
      for (const theme of ['light', 'dark']) {
        const measurement = await cdp.send('Runtime.evaluate', {
          expression: `(async () => {
            document.body.classList.remove('light-mode','dark-mode');
            document.body.classList.add('${theme}-mode');
            await document.fonts.ready;
            await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
            const hero = document.querySelector('.home-hero-card--1 .home-hero-card__content');
            const bodyStyle = getComputedStyle(document.body);
            const outline = document.querySelector('.home-hero__title-line--outline');
            const outlineStyle = outline ? getComputedStyle(outline) : null;
            const aliases = Object.fromEntries(['--app-field','--app-map','--app-line','--app-line-weak','--app-on-accent']
              .map(name => [name,bodyStyle.getPropertyValue(name).trim()]));
            const probe = document.createElement('span');
            probe.style.color = 'var(--app-heading)';
            document.body.append(probe);
            const headingColor = getComputedStyle(probe).color;
            probe.remove();
            const footerLink = document.querySelector('.app-public-footer__language-link--active');
            let footerActiveColor = null;
            if (footerLink) {
              footerLink.scrollIntoView({ behavior: 'instant', block: 'end' });
              for (let attempt = 0; attempt < 10; attempt++) {
                footerActiveColor = getComputedStyle(footerLink).color;
                if (footerActiveColor === headingColor) break;
                await new Promise(r => setTimeout(r, 50));
              }
              window.scrollTo({ top: 0, behavior: 'instant' });
              await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
            }
            return { viewport: innerWidth, scrollWidth: document.documentElement.scrollWidth,
              title: document.title, fontStatus: document.fonts.status,
              wordmarkColors: [...document.querySelectorAll('.app-brand-wordmark')].map(mark => getComputedStyle(mark).color),
              wordmarkFonts: [...document.querySelectorAll('.app-brand-wordmark')].map(mark => getComputedStyle(mark).fontFamily),
              homeTitleFontSize: document.querySelector('.home-hero__title') ? getComputedStyle(document.querySelector('.home-hero__title')).fontSize : null,
              homeOutline: outlineStyle ? { color: outlineStyle.color, stroke: outlineStyle.webkitTextStrokeColor, strokeWidth: outlineStyle.webkitTextStrokeWidth } : null,
              heroTitleWidth: hero ? hero.getBoundingClientRect().width : null,
              bodyBackground: bodyStyle.backgroundColor, aliases, headingColor,
              footerActiveColor };
          })()`,
          awaitPromise: true, returnByValue: true,
        });
        if (measurement.exceptionDetails) throw new Error('Could not measure the rendered SSR snapshot.');
        const screenshot = `${routeName}_${width}_${theme}.webp`;
        const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
        await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
        route.captures.push({ width, theme, state: 'initial', screenshot, ...measurement.result.value });
        const consent = await cdp.send('Runtime.evaluate', {
          expression: `(() => { const panel = document.querySelector('app-cookie-consent-banner'); if (!panel) return false; panel.style.display = 'none'; return true; })()`, returnByValue: true,
        });
        if (consent.result.value) {
          const cleanScreenshot = `${routeName}_${width}_${theme}_consent-hidden.webp`;
          const cleanPixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
          await writeFile(join(outputRoot, cleanScreenshot), Buffer.from(cleanPixels.data, 'base64'));
          const contrast = [390, 1366].includes(width)
            ? await cdp.send('Runtime.evaluate', { expression: `(${collectContrastCandidates.toString()})()`, returnByValue: true })
            : null;
          route.captures.push({ width, theme, state: 'consent-hidden-for-visual-inspection-only', screenshot: cleanScreenshot,
            ...(contrast ? { contrastCandidates: contrast.result.value } : {}) });
          await cdp.send('Runtime.evaluate', { expression: `document.querySelector('app-cookie-consent-banner').style.removeProperty('display')` });
        }
      }
    }
    console.log(`${path}: ${route.captures.length} initial SSR captures; ${reads.length} anonymous API reads`);
    await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
    if (route.captures.some(capture => capture.state === 'initial' && capture.wordmarkFonts.some(font => !/^"?Bebas Neue/.test(font)))) {
      throw new Error(`Protected wordmark font changed: ${path}`);
    }
    if (path.endsWith('/home') && route.captures.some(capture => {
      if (capture.state !== 'initial') return false;
      const expected = capture.width <= 350 ? 35 : capture.width <= 680
        ? Math.min(53, Math.max(38, capture.width * 0.09)) : Math.min(74, Math.max(39, capture.width * 0.053));
      return Math.abs(Number.parseFloat(capture.homeTitleFontSize) - expected) > 0.1;
    })) throw new Error(`V6 home title size differs from the reference scale: ${path}`);
    if (path.endsWith('/home') && route.captures.some(capture => {
      if (capture.state !== 'initial') return false;
      const outline = capture.homeOutline;
      return !outline || (capture.width <= 680
        ? outline.color !== capture.headingColor || Number.parseFloat(outline.strokeWidth) !== 0
        : outline.stroke !== capture.headingColor || Math.abs(Number.parseFloat(outline.strokeWidth) - 1.4) > 0.01);
    })) throw new Error(`A legacy theme override changed the V6 home title: ${path}`);
    if (route.captures.some((capture) => capture.state === 'initial' && (capture.scrollWidth > capture.viewport + 1 || (capture.heroTitleWidth !== null && capture.heroTitleWidth < 112) || Object.values(capture.aliases).some(value => !value) || capture.wordmarkColors.some(color => color !== (capture.theme === 'light' ? 'rgb(22, 10, 0)' : 'rgb(255, 255, 255)')) || (path !== '/' && capture.footerActiveColor !== capture.headingColor)))) {
      throw new Error(`Initial SSR presentation requires correction: ${path}; see report.json.`);
    }
  }
  // Never run the hydrated browser smoke locally: its build and browser workload
  // belong to the isolated CI runner, outside the user's shared memory budget.
  if (process.env.CI === 'true' && homeRawHtml) {
    await verifyFleuryItemRendering(application);
    await captureHydratedHome(homeRawHtml);
  }
} finally {
  entry?.ɵdestroyAngularServerApp();
  try { await cdp?.send('Browser.close'); } catch { /* Chrome may close its socket first. */ }
  cdp?.close();
  if (chrome && chrome.exitCode === null) {
    const exited = once(chrome, 'exit');
    chrome.kill();
    await Promise.race([exited, new Promise((yes) => setTimeout(yes, 5000))]);
  }
  globalThis.fetch = nativeFetch;
  // tempRoot is the unique directory returned by mkdtemp above.
  await rm(tempRoot, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
}
