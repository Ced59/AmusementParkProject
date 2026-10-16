import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { extname, join, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { isCanceledInterceptionError, isRetiredDocumentInterceptionError, advanceMainDocument } from './browser-interception-errors.mjs';
import { publicDirectoryReadEvidence, publicParkItemsReadEvidence, publicRouteEvidenceName } from './public-directory-evidence.mjs';
import { readHomeEvidenceFixtures, summarizeHomeResources, compareHomeNetworkSamples, homeNetworkAcceptance, homeVitalsObserver } from './home-network-evidence.mjs';
import { exercisePublicParkItems } from './exercise-public-park-items.mjs';
import { initialThemeObserver, validateInitialTheme } from './initial-theme-evidence.mjs';
import { publicPhotoReadEvidence } from './public-photo-evidence.mjs';
import { exercisePublicPhotos } from './exercise-public-photos.mjs';
import { resumeStaticCaptures } from './browser-phase-isolation.mjs';
import { photoLightboxLayoutIssues, photoLightboxLayoutExpression } from './photo-lightbox-layout.mjs';
import { exercisePublicPractical } from './exercise-public-practical.mjs';
import { publicPracticalReadEvidence, openingCalendarLayoutIssues } from './public-practical-evidence.mjs';
import { publicVideoReadEvidence } from './public-video-evidence.mjs';
import { exercisePublicMediaHistory } from './exercise-public-media-history.mjs';
import { exercisePublicMap } from './exercise-public-map.mjs';
import { canRenderPublicDocument } from './public-document-evidence.mjs';
import { exercisePublicStandalone } from './exercise-public-standalone.mjs';
import { exerciseAnonymousAuth } from './exercise-anonymous-auth.mjs';
import { mapPopupLayoutIssues, mapReviewScrollDelta } from './map-popup-layout.mjs';

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
  '/fr/parks?page=2',
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
  '/fr/park/1675e96c-6361-48ff-862b-43218167facd/le-pal',
  '/fr/park/94a4c4df-5ab6-4dc3-b756-2de664e947c4/efteling/pricing',
  '/fr/about', '/fr/contact', '/fr/privacy', '/fr/not-found',
];
if (paths.some((path) => path !== '/' && (!/^\/(fr|en|de|nl|it|es|pt|pl)\//.test(path) || /\/admin|\/profile/.test(path)))) {
  throw new Error('Only anonymous localized public routes are accepted.');
}
if (new Set(paths.map(publicRouteEvidenceName)).size !== paths.length) {
  throw new Error('Public evidence filenames must be unique, including query-string pages.');
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
const homeFixtureDocument = process.env.V6_BASELINE_DIST_DIR
  ? JSON.parse(await readFile(join(outputRoot, 'home-public-fixtures.json'), 'utf8')) : null;
if (homeFixtureDocument && (!/^[a-f0-9]{40}$/.test(homeFixtureDocument.baseSha ?? '') || homeFixtureDocument.baseSha !== process.env.V6_BASELINE_SHA)) {
  throw new Error('The home comparison baseline must match the exact PR base SHA.');
}
const homeSsrFixtures = homeFixtureDocument ? readHomeEvidenceFixtures(homeFixtureDocument.ssr) : new Map();
const homeClientFixtures = homeFixtureDocument ? readHomeEvidenceFixtures(homeFixtureDocument.client) : new Map();
let replayHomeSsr = false;
let reads = [];
globalThis.fetch = async (input, options = {}) => {
  const request = new Request(input, options);
  const url = new URL(request.url);
  if (!['GET', 'HEAD'].includes(request.method)) throw new Error('The evidence tool rejects remote writes.');
  if (url.origin !== 'http://api:8080' && !(url.origin === publicOrigin && url.pathname.startsWith('/api/'))) {
    throw new Error(`Unexpected SSR network origin: ${url.origin}`);
  }
  const target = url.origin === 'http://api:8080' ? new URL(`/api${url.pathname}${url.search}`, publicOrigin) : url;
  if (replayHomeSsr) {
    const fixture = homeSsrFixtures.get(`${target.pathname}${target.search}`);
    if (!fixture) throw new Error('A new SSR home request has no exact master fixture; investigate before comparison.');
    reads.push({ path: target.pathname, status: fixture.status, source: 'same anonymous baseline response' });
    return new Response(fixture.bytes, { status: fixture.status, headers: { 'content-type': fixture.contentType } });
  }
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

async function captureHydratedHome(rawHtml, options = {}) {
  const origin = options.networkProbe ? `https://apf-network-${options.label}-${options.width}-${options.sampleNumber ?? 1}.invalid` : 'https://apf-review.invalid';
  const root = options.browserRoot ?? browserRoot;
  const result = { scope: 'anonymous hydrated home, navigation and catalogue smoke; isolated browser origin', apiReads: [], blockedRequests: [], exceptions: [], checks: [], captures: [] };
  if (options.networkProbe) {
    Object.assign(result, { label: options.label, width: options.width, sampleNumber: options.sampleNumber ?? 1, resources: [] });
    if (options.initialTheme) (report.initialThemeSamples ??= []).push(result);
    else (report.homeNetworkSamples ??= []).push(result);
  } else report.hydratedHome = result;
  const mimeTypes = { '.mjs': 'text/javascript', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.svg': 'image/svg+xml', '.webp': 'image/webp', '.png': 'image/png', '.ico': 'image/x-icon' };
  const interceptionErrors = [];
  result.interceptionErrors = interceptionErrors;
  const canceledNetworkRequests = new Set();
  const pendingInterceptions = new Set();
  const requestDocuments = new Map();
  const retiredNetworkRequests = new Set();
  let currentMainDocument;
  const stopRequestDocuments = cdp.on('Network.requestWillBeSent', ({ requestId, frameId, loaderId }) => {
    if (!frameId || !loaderId) return;
    if (requestDocuments.size >= 4096 && !requestDocuments.has(requestId)) {
      interceptionErrors.push('Request/document evidence exceeds its explicit bound.');
      return;
    }
    requestDocuments.set(requestId, { frameId, loaderId });
  });
  const stopDocumentRetirement = cdp.on('Page.frameNavigated', ({ frame }) => {
    currentMainDocument = advanceMainDocument(currentMainDocument, frame, requestDocuments, retiredNetworkRequests);
  });
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
    const resource = options.networkProbe ? { path: `${url.pathname}${url.search}`, type: resourceType, status: null, decodedBytes: 0 } : null;
    if (resource) result.resources.push(resource);
    const respond = async (status, contentType, bytes) => {
      if (resource) Object.assign(resource, { status, decodedBytes: Buffer.byteLength(Buffer.from(bytes)) });
      await fulfill(requestId, status, contentType, bytes);
    };
    if (url.pathname.startsWith('/api/')) {
      const target = new URL(`${url.pathname}${url.search}`, publicOrigin);
      (result.apiRequests ??= []).push({ path: target.pathname, query: target.search });
      if (target.pathname === '/api/search' && target.searchParams.get('query') === 'APF-READONLY-ERROR-FIXTURE') {
        (result.injectedFailures ??= []).push({ path: target.pathname, query: target.search, status: 503, scope: 'isolated browser only; not sent to production' });
        await respond(503, 'application/problem+json', JSON.stringify({ type: 'about:blank', title: 'Read-only review fixture', status: 503 }));
        return;
      }
      const fixture = options.networkProbe ? homeClientFixtures.get(`${target.pathname}${target.search}`) : null;
      if (fixture) {
        result.apiReads.push({ path: target.pathname, query: target.search, status: fixture.status, source: 'same anonymous baseline client response' });
        await respond(fixture.status, fixture.contentType, fixture.bytes);
        return;
      }
      const response = await nativeFetch(target, { method: request.method, signal: AbortSignal.timeout(20000) });
      const bytes = await response.arrayBuffer();
      const directory = publicDirectoryReadEvidence(target, response.status, bytes);
      const parkItems = publicParkItemsReadEvidence(target, response.status, bytes);
      const photos = publicPhotoReadEvidence(target, response.status, bytes);
      const practical = publicPracticalReadEvidence(target, response.status, bytes);
      const videos = publicVideoReadEvidence(target, response.status, bytes);
      result.apiReads.push({ path: target.pathname, query: target.search, status: response.status,
        ...(directory ? { directory } : {}), ...(parkItems ? { parkItems } : {}), ...(photos ? { photos } : {}), ...(practical ? { practical } : {}), ...(videos ? { videos } : {}) });
      await respond(response.status, response.headers.get('content-type') ?? 'application/octet-stream', bytes);
      return;
    }
    if (resourceType === 'Document' && url.origin === origin && url.pathname === '/fr/home') {
      await respond(200, 'text/html; charset=utf-8', rawHtml);
      return;
    }
    if (resourceType === 'Document' && url.origin === origin && !options.networkProbe && options.renderDocument) {
      const path = url.pathname + url.search;
      if (!canRenderPublicDocument(path)) throw new Error(`Unexpected non-public review document: ${path}`);
      if ((result.documentNavigations?.length ?? 0) >= 12) throw new Error('Public document navigation exceeds its explicit recipe bound.');
      const document = await options.renderDocument(path);
      (result.documentNavigations ??= []).push({ path, engineStatus: document.status, reads: document.reads });
      await respond(document.status, 'text/html; charset=utf-8', document.html);
      return;
    }
    const assetPath = resolve(root, `.${decodeURIComponent(url.pathname)}`);
    if (!assetPath.startsWith(`${root}${sep}`)) throw new Error('Rejected asset traversal.');
    try {
      await respond(200, mimeTypes[extname(assetPath)] ?? 'application/octet-stream', await readFile(assetPath));
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
      await respond(404, 'text/plain', 'No review asset at this path.');
    }
  };
  const stopInterception = cdp.on('Fetch.requestPaused', (request) => {
    const task = intercept(request).catch(async (error) => {
      // CDP can deliver loadingFailed just after the failed fulfillment response.
      if (error.message === 'Invalid InterceptionId.' && request.networkId) {
        await new Promise((yes) => setTimeout(yes, 100));
      }
      const canceled = isCanceledInterceptionError(error, request.networkId, canceledNetworkRequests);
      const retired = isRetiredDocumentInterceptionError(error, request.networkId, retiredNetworkRequests);
      if (canceled || retired) {
        (result.canceledRequests ??= []).push({ path: new URL(request.request.url).pathname, networkId: request.networkId,
          reason: canceled ? 'matching-network-cancellation' : 'matching-retired-document',
          ...(retired ? { document: requestDocuments.get(request.networkId) } : {}) });
      } else {
        interceptionErrors.push(error.message);
        (result.interceptionDiagnostics ??= []).push({ message: error.message, networkId: request.networkId,
          document: requestDocuments.get(request.networkId), currentMainDocument, retiredNetworkRequests: [...retiredNetworkRequests].slice(0, 20) });
      }
    }).finally(() => { pendingInterceptions.delete(task); requestDocuments.delete(request.networkId); retiredNetworkRequests.delete(request.networkId); });
    pendingInterceptions.add(task);
  });
  const stopExceptions = cdp.on('Runtime.exceptionThrown', ({ exceptionDetails }) => {
    const message = (exceptionDetails.exception?.description ?? exceptionDetails.text).slice(0, 1500);
    result.exceptions.push(message);
    (result.exceptionContexts ??= []).push({ afterCheck: result.checks.at(-1) ?? null, message });
  });
  const evaluate = async (expression) => {
    const evaluation = await cdp.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (evaluation.exceptionDetails) {
      const description = (evaluation.exceptionDetails.exception?.description ?? evaluation.exceptionDetails.text).split('\n')[0].slice(0, 1000);
      (result.evaluationFailures ??= []).push({ check: result.activeCheck ?? result.checks.at(-1), description });
      throw new Error(description);
    }
    return evaluation.result.value;
  };
  const waitUntil = async (expression, label) => {
    result.activeCheck = label;
    for (let attempt = 0; attempt < 100; attempt++) {
      if (interceptionErrors.length) throw new Error(interceptionErrors.join('; '));
      if (result.exceptions.length) {
        result.failedCheck = label;
        throw new Error(`Browser exception while checking: ${label}; inspect exceptionContexts.`);
      }
      if (typeof expression === 'function' ? await expression() : await evaluate(expression)) { result.checks.push(label); return; }
      await new Promise((yes) => setTimeout(yes, 200));
    }
    result.failedCheck = label;
    try {
      result.failedBrowserState = await evaluate(`({ path: location.pathname + location.search, viewport: innerWidth,
        history: { index: navigation.currentEntry.index, key: navigation.currentEntry.key,
          entries: navigation.entries().map(entry => ({ index: entry.index, key: entry.key,
            url: new URL(entry.url).pathname + new URL(entry.url).search })) },
        heading: document.querySelector('h1')?.textContent?.trim(),
        focus: { tag: document.activeElement?.tagName, classes: document.activeElement?.className,
          insidePhotoDialog: !!document.querySelector('.photo-lightbox__dialog')?.contains(document.activeElement),
          photoAnchors: [...document.querySelectorAll('.photo-lightbox .cdk-focus-trap-anchor')].map(anchor => ({ tabIndex: anchor.tabIndex, classes: anchor.className })) },
        directoryNames: [...document.querySelectorAll('.parks-results .ui-park-card__title')].map(title => title.textContent.trim()),
        directoryPage: document.querySelector('.parks-results .p-paginator-page.p-highlight')?.textContent.trim(),
        treeExpanded: document.querySelector('.app-public-nav__tree-trigger')?.getAttribute('aria-expanded'),
        treeLinks: [...document.querySelectorAll('.app-public-park-tree a')].map(link => ({ href: link.getAttribute('href'), label: link.textContent.trim(), current: link.classList.contains('app-public-park-tree__item--current') })) })`);
      const failurePixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, 'failed-hydrated-check.webp'), Buffer.from(failurePixels.data, 'base64'));
    } catch (error) {
      result.failureDiagnosticError = error.message;
    }
    throw new Error(`Hydrated browser check timed out: ${label}`);
  };
  const verifyDirectory = async (cursor, expected, label) => {
    let read;
    await waitUntil(() => {
      read = result.apiReads.slice(cursor).find(candidate => candidate.directory
        && Object.entries(expected).every(([key, value]) => new URLSearchParams(candidate.query).get(key) === value));
      return Boolean(read);
    }, `${label}: real anonymous directory request`);
    if (read.directory.pagination.currentPage !== Number(expected.page) || read.directory.pagination.itemsPerPage !== Number(expected.size)) {
      throw new Error(`Directory response did not honor the requested page/size: ${label}`);
    }
    await waitUntil(`location.pathname === '/fr/parks'
      && !document.querySelector('.parks-results .app-page-state--loading')
      && JSON.stringify([...document.querySelectorAll('.parks-results .ui-park-card__title')].map(title => title.textContent.trim())) === ${JSON.stringify(JSON.stringify(read.directory.names))}
      && (${read.directory.pagination.totalItems > read.directory.pagination.itemsPerPage}
        ? document.querySelector('.parks-results .p-paginator-page.p-highlight')?.textContent.trim() === '${read.directory.pagination.currentPage}'
        : !document.querySelector('.parks-results .app-pagination'))`, `${label}: API names and pagination match the rendered cards`);
    (result.directoryScenarios ??= []).push({ label, path: read.path, query: read.query, ...read.directory });
    return read.directory;
  };
  let observerScript;
  try {
    await cdp.send('Network.enable');
    if (options.networkProbe) {
      await cdp.send('Page.navigate', { url: 'about:blank' });
      await waitUntil(`location.href === 'about:blank' && document.readyState === 'complete'`, 'previous isolated document unloaded');
      await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });
      await cdp.send('Emulation.setEmulatedMedia', { features: [
        { name: 'prefers-reduced-motion', value: 'reduce' },
        { name: 'prefers-color-scheme', value: options.initialTheme?.system ?? 'light' },
      ] });
      observerScript = (await cdp.send('Page.addScriptToEvaluateOnNewDocument', {
        source: homeVitalsObserver + (options.initialTheme ? initialThemeObserver(options.initialTheme.saved) : ''),
      })).identifier;
    }
    await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    await cdp.send('Network.setBlockedURLs', { urls: [] });
    await cdp.send('Emulation.setDeviceMetricsOverride', { width: options.width ?? 390, height: 1200, deviceScaleFactor: 1, mobile: false });
    await cdp.send('Page.navigate', { url: `${origin}/fr/home` });
    await waitUntil(`location.origin === '${origin}' && document.readyState === 'complete' && document.querySelector('app-root') && !document.querySelector('app-root').hasAttribute('ngh') && !!document.querySelector('#home-search-input')`, 'Angular root hydrated');
    if (!options.networkProbe) {
      // Discard setup/file navigations before the first real interaction so
      // unrelated entries cannot evict this scenario's back/forward chain.
      const beforeHistory = await cdp.send('Page.getNavigationHistory');
      await cdp.send('Page.resetNavigationHistory');
      const afterHistory = await cdp.send('Page.getNavigationHistory');
      result.historySetup = { previousEntryCount: beforeHistory.entries.length,
        entryCount: afterHistory.entries.length, currentIndex: afterHistory.currentIndex };
      if (afterHistory.entries.length !== 1 || afterHistory.currentIndex !== 0) throw new Error('The isolated browser history did not reset before its interaction scenario.');
    }
    if (options.networkProbe) {
      // Identical observation window for both real builds. Not a delay added to
      // an interaction recipe: the rapid catalogue teardown remains unchanged.
      await new Promise(yes => setTimeout(yes, 5000));
      result.vitals = await evaluate(`({ ...window.__apfHomeVitals, fontStatus: document.fonts.status,
        heroNames: [...document.querySelectorAll('.home-hero-card__content strong')].map(node => node.textContent.trim()),
        heroImages: [...document.querySelectorAll('.home-hero-card__media img')].map(image => ({ path: new URL(image.currentSrc || image.src).pathname, complete: image.complete, naturalWidth: image.naturalWidth })),
        domCompleteMs: performance.getEntriesByType('navigation')[0]?.domComplete })`);
      if (options.initialTheme) {
        result.initialThemeEvidence = await evaluate(`({ ...window.__apfInitialTheme,
          firstContentfulPaintMs: performance.getEntriesByType('paint').find(entry => entry.name === 'first-contentful-paint')?.startTime,
          finalTheme: document.body.classList.contains('light-mode') ? 'light' : 'dark',
          finalBackground: getComputedStyle(document.body).backgroundColor })`);
        result.initialThemeSettings = options.initialTheme;
        result.initialThemeVerdict = validateInitialTheme(result.initialThemeEvidence, options.initialTheme.saved ?? options.initialTheme.system);
      }
      // A trusted mouse interaction, not Runtime.evaluate(...click()), for an
      // Event Timing proxy. This is one local unit preference, never field INP.
      const initialMeasurement = await evaluate(`document.querySelector('.app-public-nav__measurement-toggle').textContent.trim()`);
      const unitPoint = await evaluate(`(() => { const rect = document.querySelector('.app-public-nav__measurement-toggle').getBoundingClientRect(); return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 }; })()`);
      await cdp.send('Input.dispatchMouseEvent', { type: 'mousePressed', ...unitPoint, button: 'left', clickCount: 1 });
      await cdp.send('Input.dispatchMouseEvent', { type: 'mouseReleased', ...unitPoint, button: 'left', clickCount: 1 });
      await new Promise(yes => setTimeout(yes, 200));
      result.vitals.unitInteraction = await evaluate(`({ eventTimingAvailable: window.__apfHomeVitals.eventTimingAvailable,
        durationsAtLeast16ms: window.__apfHomeVitals.interactionDurations, measurement: document.querySelector('.app-public-nav__measurement-toggle').textContent.trim() })`);
      if (result.vitals.unitInteraction.measurement === initialMeasurement) throw new Error('The measured native unit click did not update its real control.');
      result.summary = summarizeHomeResources(result.resources);
      const networkScreenshot = `fr_home_network_${options.label}_${options.width}_${options.sampleNumber ?? 1}.webp`;
      const networkPixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, networkScreenshot), Buffer.from(networkPixels.data, 'base64'));
      result.captures.push(networkScreenshot);
      if (result.exceptions.length || interceptionErrors.length) throw new Error('Home network probe raised a browser exception.');
      return;
    }
    await evaluate(`document.querySelector('.app-cookie-consent__button--ghost')?.click()`);
    await waitUntil(`!document.querySelector('.app-cookie-consent')`, 'necessary-cookie choice handled by Angular');
    const initialUnits = await evaluate(`document.querySelector('.app-public-nav__measurement-toggle').textContent.trim()`);
    await evaluate(`document.querySelector('.app-public-nav__measurement-toggle').click()`);
    await waitUntil(`document.querySelector('.app-public-nav__measurement-toggle').textContent.trim() !== ${JSON.stringify(initialUnits)}`, 'anonymous unit control updates its real preference');
    await evaluate(`document.querySelector('.app-public-nav__measurement-toggle').click()`);
    await waitUntil(`document.querySelector('.app-public-nav__measurement-toggle').textContent.trim() === ${JSON.stringify(initialUnits)}`, 'anonymous unit preference restored');
    await evaluate(`document.querySelector('.app-public-nav__language').click()`);
    await waitUntil(`document.querySelectorAll('.app-language-dialog .language-option').length === 8`, 'language dialog exposes all eight existing choices');
    const languageBounds = await evaluate(`(() => { const rect = document.querySelector('.app-language-dialog').getBoundingClientRect(); return { left: rect.left, right: rect.right, viewport: innerWidth }; })()`);
    if (languageBounds.left < -1 || languageBounds.right > languageBounds.viewport + 1) throw new Error('Language dialog overflows the mobile viewport.');
    result.languageDialogBounds = languageBounds;
    const languageScreenshot = 'fr_language_dialog_anonymous_390.webp';
    const languagePixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
    await writeFile(join(outputRoot, languageScreenshot), Buffer.from(languagePixels.data, 'base64'));
    result.captures.push(languageScreenshot);
    await evaluate(`[...document.querySelectorAll('.app-language-dialog .language-option')].find(button => button.querySelector('small')?.textContent.trim() === 'DE').click()`);
    await waitUntil(`location.pathname === '/de/home' && document.querySelector('.app-public-nav__language-code')?.textContent.trim() === 'DE' && !document.querySelector('.app-language-dialog')`, 'real language choice preserves the home route in German');
    await evaluate(`document.querySelector('.app-public-nav__language').click()`);
    await waitUntil(`document.querySelectorAll('.app-language-dialog .language-option').length === 8`, 'language dialog reopens in German');
    await evaluate(`[...document.querySelectorAll('.app-language-dialog .language-option')].find(button => button.querySelector('small')?.textContent.trim() === 'FR').click()`);
    await waitUntil(`location.pathname === '/fr/home' && document.querySelector('.app-public-nav__language-code')?.textContent.trim() === 'FR' && !document.querySelector('.app-language-dialog')`, 'real language choice restores the French home route');
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
    await evaluate(`document.querySelector('.app-public-nav__tree-trigger').click()`);
    // This existing tree is contextual, not a second full park section menu:
    // the overview has catalogue/current park; inventory appears on /items.
    await waitUntil(`document.querySelector('.app-public-nav__tree-trigger')?.getAttribute('aria-expanded') === 'true' && !!document.querySelector('.app-public-park-tree a[href="/fr/parks"]') && !!document.querySelector('.app-public-park-tree a.app-public-park-tree__item--current[href$="/le-fleury"]')`, 'real park overview tree exposes catalogue and current park');
    const treeBounds = await evaluate(`(() => { const rect = document.querySelector('.app-public-park-tree').getBoundingClientRect(); return { left: rect.left, right: rect.right, viewport: innerWidth }; })()`);
    if (treeBounds.left < -1 || treeBounds.right > treeBounds.viewport + 1) throw new Error('Park navigation tree overflows the desktop viewport.');
    result.parkTreeBounds = treeBounds;
    const treeScreenshot = 'fr_fleury_navigation_tree_1366.webp';
    const treePixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
    await writeFile(join(outputRoot, treeScreenshot), Buffer.from(treePixels.data, 'base64'));
    result.captures.push(treeScreenshot);
    await evaluate(`document.querySelector('.app-public-nav__tree-trigger').click()`);
    await waitUntil(`document.querySelector('.app-public-nav__tree-trigger')?.getAttribute('aria-expanded') === 'false' && !document.querySelector('.app-public-park-tree')`, 'real park navigation tree closes without changing the route');
    await evaluate(`document.querySelector('.park-banner__actions .pi-sitemap').closest('button').click()`);
    await waitUntil(`location.pathname.endsWith('/le-fleury/items') && !!document.querySelector('#park-items-search-input') && document.querySelectorAll('app-park-item-card-view').length > 0`, 'real park action opens the full item inventory');
    await exercisePublicParkItems({ evaluate, waitUntil, result });
    await evaluate(`document.querySelector('.app-public-nav__tree-trigger').click()`);
    await waitUntil(`document.querySelector('.app-public-nav__tree-trigger')?.getAttribute('aria-expanded') === 'true' && !!document.querySelector('.app-public-park-tree a.app-public-park-tree__item--current[href$="/le-fleury/items"]')`, 'real inventory tree exposes its current list');
    await evaluate(`document.querySelector('.app-public-nav__tree-trigger').click()`);
    await waitUntil(`!document.querySelector('.app-public-park-tree')`, 'inventory tree closes without navigating');
    result.itemHistory.beforeLeaving = await evaluate(`({ url: location.pathname + location.search,
      index: navigation.currentEntry.index, key: navigation.currentEntry.key })`);
    await evaluate(`history.back()`);
    await waitUntil(`location.pathname.endsWith('/le-fleury') && !!document.querySelector('app-park-detail-page h1')`, 'browser back from inventory restores the full park');
    await exercisePublicPhotos({ evaluate, waitUntil, result, captureLightbox: async () => {
      try {
        for (const [width, height] of [[320, 568], [390, 640], [1366, 480]]) {
          await cdp.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
          await evaluate('new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))');
          let layout;
          let previousLayout;
          await waitUntil(async () => {
            layout = await evaluate(photoLightboxLayoutExpression);
            result.lastLightboxLayout = layout;
            const serialized = JSON.stringify(layout);
            const settled = previousLayout === serialized;
            previousLayout = serialized;
            return settled && layout.width === width && layout.height === height && photoLightboxLayoutIssues(layout).length === 0;
          }, `real photo lightbox settles inside the ${width}×${height} viewport before capture`);
          const screenshot = `fr_photo_lightbox_${width}_${height}_${layout.theme}.webp`;
          const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
          await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
          const issues = photoLightboxLayoutIssues(layout);
          (result.photoLightboxLayouts ??= []).push({ ...layout, screenshot, issues });
          if (issues.length) throw new Error(`Real photo lightbox geometry requires correction at ${width}×${height}: ${issues.join(', ')}`);
        }
      } finally {
        await cdp.send('Emulation.setDeviceMetricsOverride', { width: 1366, height: 1200, deviceScaleFactor: 1, mobile: false });
      }
    }, pressTab: async backward => {
      const key = { key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9, modifiers: backward ? 8 : 0 };
      await cdp.send('Input.dispatchKeyEvent', { type: 'keyDown', ...key });
      await cdp.send('Input.dispatchKeyEvent', { type: 'keyUp', ...key });
    } });
    const captureHydratedPage = async (label, selector) => {
      const originalTheme = await evaluate(`document.body.classList.contains('light-mode') ? 'light' : 'dark'`);
      const setTheme = async theme => {
        if (await evaluate(`!document.body.classList.contains('${theme}-mode')`)) {
          await evaluate(`document.querySelector('app-theme-switcher button').click()`);
          await waitUntil(`document.body.classList.contains('${theme}-mode')`, 'practical theme control updates Angular');
        }
      };
      try {
        for (const theme of [originalTheme, originalTheme === 'dark' ? 'light' : 'dark']) {
          await setTheme(theme);
          const sizes = widths.map(width => ({ width, height: 1200 }));
          if (label === 'map-marker-popup') sizes.push({ width: 390, height: 640 }, { width: 1366, height: 480 });
          for (const { width, height } of sizes) {
            await cdp.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
            const scrollSelector = label === 'map-marker-popup' ? '.park-items-map .leaflet-map-container' : selector;
            await evaluate(`document.querySelector(${JSON.stringify(scrollSelector)}).scrollIntoView({ block: '${label === 'map-marker-popup' ? 'center' : 'start'}', behavior: 'instant' })`);
            await evaluate('new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))');
            const layout = await evaluate(`({ width: innerWidth, height: innerHeight, locale: location.pathname.split('/')[1], documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth), theme: document.body.classList.contains('light-mode') ? 'light' : 'dark' })`);
            if (layout.documentWidth > width + 1) throw new Error(`Hydrated practical page overflows at ${width}px.`);
            if (label === 'map-marker-popup') {
              await waitUntil(async () => {
                const position = await evaluate(`(() => {
                  const map = document.querySelector('.park-items-map .leaflet-map-container').getBoundingClientRect();
                  const bottom = document.querySelector('.app-mobile-bottom-nav')?.getBoundingClientRect();
                  return { mapTop: map.top, mapHeight: map.height, viewportHeight: innerHeight,
                    bottomNavigationHeight: bottom && bottom.height > 0 && bottom.bottom >= innerHeight - 1 ? bottom.height : 0 };
                })()`);
                const scrollDelta = mapReviewScrollDelta(position);
                if (Math.abs(scrollDelta) > 1) {
                  await evaluate(`scrollBy({ top: ${JSON.stringify(scrollDelta)}, behavior: 'instant' })`);
                  await evaluate('new Promise(resolve => requestAnimationFrame(resolve))');
                }
                result.lastPopupLayout = await evaluate(`(() => {
                  const rect = node => { if (!node) return null; const r = node.getBoundingClientRect(); return { x: r.x, y: r.y, right: r.right, bottom: r.bottom }; };
                  const control = node => { const r = rect(node); return r ? { ...r, unobscured: node.contains(document.elementFromPoint((r.x + r.right) / 2, (r.y + r.bottom) / 2)) } : null; };
                  const heading = document.querySelector('.park-items-map .leaflet-popup-content > strong');
                  const popupHost = document.querySelector('.park-items-map .leaflet-popup');
                  const popupContent = document.querySelector('.park-items-map .leaflet-popup-content');
                  const mapNode = document.querySelector('.park-items-map .leaflet-map-container');
                  const popupStyle = popupHost ? getComputedStyle(popupHost) : null;
                  const headingVisible = (() => { if (!heading) return null; const range = document.createRange(); range.selectNodeContents(heading); const rows = [...range.getClientRects()];
                    return { unobscured: rows.length > 0 && rows.every(r => [r.x + 2, r.x + r.width / 2, r.right - 2].every(x => heading.contains(document.elementFromPoint(x, r.y + r.height / 2)))) }; })();
                  return { map: rect(document.querySelector('.park-items-map .leaflet-map-container')), popup: rect(document.querySelector('.park-items-map .leaflet-popup-content-wrapper')), heading: headingVisible,
                    measured: { mapClientWidth: mapNode?.clientWidth, mapClientHeight: mapNode?.clientHeight, popupHost: rect(popupHost),
                      popupOffsetHeight: popupHost?.offsetHeight, content: rect(popupContent),
                      popupStyle: popupStyle ? { marginBottom: popupStyle.marginBottom, bottom: popupStyle.bottom, top: popupStyle.top,
                        position: popupStyle.position, transform: popupStyle.transform, boxSizing: popupStyle.boxSizing } : null },
                    close: control(document.querySelector('.park-items-map .leaflet-popup-close-button')), actions: [...document.querySelectorAll('.park-items-map .leaflet-map-popup__action')].map(control),
                    zoomControls: [...document.querySelectorAll('.park-items-map .leaflet-control-zoom a')].map(control) };
                })()`);
                return mapPopupLayoutIssues(result.lastPopupLayout).length === 0;
              }, `resized real map popup remains complete and actionable at ${width}×${height} (${theme})`);
              layout.popup = result.lastPopupLayout;
            }
            if (label === 'opening-hours-selected') {
              const cells = await evaluate(`(() => {
                const rect = node => { const r = node.getBoundingClientRect(); return { x: r.x, y: r.y, right: r.right, bottom: r.bottom }; };
                return [...document.querySelectorAll('button.park-opening-hours-cell')].map(button => ({
                  bounds: rect(button.closest('td')), button: rect(button), labels: [...button.querySelectorAll('.park-opening-hours-cell__date, .park-opening-hours-cell__today, .park-opening-hours-cell__status, .park-opening-hours-cell__hours')].map(node => ({
                    bounds: rect(node), clientWidth: node.clientWidth, scrollWidth: node.scrollWidth, clientHeight: node.clientHeight, scrollHeight: node.scrollHeight,
                    today: node.classList.contains('park-opening-hours-cell__today'), textOverflow: getComputedStyle(node).textOverflow, whiteSpace: getComputedStyle(node).whiteSpace
                  }))
                }));
              })()`);
              const issues = openingCalendarLayoutIssues(cells);
              layout.calendarCells = cells.length;
              layout.todayBadges = cells.flatMap(cell => cell.labels).filter(label => label.today).length;
              if (layout.todayBadges !== 1) throw new Error('The actual calendar month does not cover its today badge; this layout scenario is not exercised.');
              if (issues.length) throw new Error(`Calendar content overlaps or clips at ${width}px (${theme}): ${issues.join(', ')}`);
              layout.calendarScroll = await evaluate(`(() => {
                const wrap = document.querySelector('.park-opening-hours-month__table-wrap');
                const needsScroll = wrap.scrollWidth > wrap.clientWidth + 1;
                wrap.scrollLeft = wrap.scrollWidth;
                const last = wrap.querySelector('thead th:last-child').getBoundingClientRect();
                const bounds = wrap.getBoundingClientRect();
                return { needsScroll, scrollLeft: wrap.scrollLeft, named: !!wrap.getAttribute('aria-label'), tabIndex: wrap.tabIndex,
                  lastColumnReachable: last.right <= bounds.right + 1 && last.x >= bounds.x - 1 };
              })()`);
              if (!layout.calendarScroll.named || layout.calendarScroll.tabIndex !== 0 || !layout.calendarScroll.lastColumnReachable) {
                throw new Error(`The calendar's last column is not reachable inside its named keyboard-scrollable region at ${width}px.`);
              }
              if (layout.calendarScroll.needsScroll) {
                const scrollEndScreenshot = `${layout.locale}_${label}_${width}_${layout.theme}_scroll-end.webp`;
                const endPixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
                await writeFile(join(outputRoot, scrollEndScreenshot), Buffer.from(endPixels.data, 'base64'));
                layout.calendarScroll.screenshot = scrollEndScreenshot;
              }
              await evaluate(`document.querySelector('.park-opening-hours-month__table-wrap').scrollLeft = 0`);
            }
            const screenshot = `${layout.locale}_${label}_${width}${height === 1200 ? '' : `_${height}`}_${layout.theme}.webp`;
            const pixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
            await writeFile(join(outputRoot, screenshot), Buffer.from(pixels.data, 'base64'));
            (result.practicalLayouts ??= []).push({ label, ...layout, screenshot });
          }
        }
      } finally {
        await cdp.send('Emulation.setDeviceMetricsOverride', { width: 1366, height: 1200, deviceScaleFactor: 1, mobile: false });
        await setTheme(originalTheme);
      }
    };
    await exercisePublicPractical({ evaluate, waitUntil, result, capturePractical: captureHydratedPage });
    await exercisePublicMap({ evaluate, waitUntil, result, capturePage: captureHydratedPage });
    await exercisePublicMediaHistory({ evaluate, waitUntil, result, capturePage: captureHydratedPage });
    await evaluate(`history.back()`);
    await waitUntil(`location.pathname === '/fr/home' && !!document.querySelector('#home-search-input')`, 'browser back returns to the home route');
    let directoryCursor = result.apiReads.length;
    await evaluate(`document.querySelector('.app-public-nav__links a[href="/fr/parks"]').click()`);
    await verifyDirectory(directoryCursor, { page: '1', size: '9' }, 'catalogue entry');
    directoryCursor = result.apiReads.length;
    await evaluate(`[...document.querySelectorAll('.parks-results .p-paginator-page')].find(link => link.textContent.trim() === '2').click()`);
    await verifyDirectory(directoryCursor, { page: '2', size: '9' }, 'catalogue page two');
    await waitUntil(`location.search === '?page=2' && document.querySelector('link[rel="canonical"]')?.href === '${publicOrigin}/fr/parks?page=2'`, 'catalogue page two retains its real URL and canonical');
    directoryCursor = result.apiReads.length;
    await evaluate(`history.back()`);
    await verifyDirectory(directoryCursor, { page: '1', size: '9' }, 'catalogue browser back');
    await waitUntil(`location.search === ''`, 'catalogue browser back restores the first-page URL');
    directoryCursor = result.apiReads.length;
    await evaluate(`(() => { const select = document.querySelector('#search-panel-audienceClassification'); const option = [...select.options].find(option => option.value.endsWith('Regional')); if (!option) throw new Error('Missing Regional filter'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await verifyDirectory(directoryCursor, { page: '1', size: '9', audienceClassification: 'Regional' }, 'catalogue audience filter');
    directoryCursor = result.apiReads.length;
    await evaluate(`(() => { const select = document.querySelector('#search-panel-audienceClassification'); select.selectedIndex = 0; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
    await verifyDirectory(directoryCursor, { page: '1', size: '9', audienceClassification: null }, 'catalogue audience reset');
    for (const rows of [6, 18, 36, 9]) {
      directoryCursor = result.apiReads.length;
      await evaluate(`(() => { const select = document.querySelector('.parks-results .p-paginator-rpp-options'); const option = [...select.options].find(option => option.textContent.trim() === '${rows}'); if (!option) throw new Error('Missing directory page size'); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
      await verifyDirectory(directoryCursor, { page: '1', size: String(rows), audienceClassification: null }, `catalogue page size ${rows}`);
    }
    directoryCursor = result.apiReads.length;
    await evaluate(`(() => { const input = document.querySelector('#parks-search-input'); input.value = 'Fleury'; input.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.parks-toolbar .app-search-panel__submit button').click(); })()`);
    const directorySearch = await verifyDirectory(directoryCursor, { page: '1', size: '9', query: 'Fleury' }, 'catalogue text search');
    await evaluate(`document.querySelector('.parks-results__card-target .ui-park-card__title').click()`);
    await waitUntil(`location.pathname === '/fr/parks' && !!document.querySelector('.parks-results__clear-selection') && document.querySelectorAll('.parks-results__card-target').length === 1 && !!document.querySelector('.parks-results__card-target--selected')`, 'catalogue card body selects the real map park without opening a detail');
    await evaluate(`document.querySelector('.parks-results__clear-selection').click()`);
    await waitUntil(`!document.querySelector('.parks-results__clear-selection') && document.querySelectorAll('.parks-results__card-target').length === ${directorySearch.names.length}`, 'catalogue clear selection restores the real result list');
    for (const width of [390, 1366]) {
      await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1200, deviceScaleFactor: 1, mobile: false });
      await evaluate(`document.querySelector('.parks-results').scrollIntoView({ block: 'start', behavior: 'instant' })`);
      const directoryBounds = await evaluate(`({ viewport: innerWidth, scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) })`);
      if (directoryBounds.scrollWidth > width + 1) throw new Error('Hydrated catalogue overflows the viewport.');
      const directoryScreenshot = `fr_parks_hydrated_search_${width}.webp`;
      const directoryPixels = await cdp.send('Page.captureScreenshot', { format: 'webp', quality: 75, captureBeyondViewport: false });
      await writeFile(join(outputRoot, directoryScreenshot), Buffer.from(directoryPixels.data, 'base64'));
      result.captures.push(directoryScreenshot);
    }
    const directoryDetailPath = await evaluate(`document.querySelector('.parks-results .ui-park-card__cta').getAttribute('href')`);
    await evaluate(`document.querySelector('.parks-results .ui-park-card__cta').click()`);
    await waitUntil(`location.pathname === ${JSON.stringify(directoryDetailPath)} && !!document.querySelector('app-park-detail-page h1') && !document.querySelector('.parks-results')`, 'catalogue detail action opens its full real park route');
    await evaluate(`document.querySelector('.app-public-nav__links a[href="/fr/home"]').click()`);
    await waitUntil(`location.pathname === '/fr/home' && !!document.querySelector('#home-search-input')`, 'catalogue recipe restores the home route');
    await exercisePublicStandalone({ evaluate, waitUntil, result, capturePage: captureHydratedPage });
    await exerciseAnonymousAuth({ evaluate, waitUntil, result, capturePage: captureHydratedPage });
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
    if (options.networkProbe) result.summary = summarizeHomeResources(result.resources);
    try { await cdp.send('Fetch.disable'); } catch (error) { result.exceptions.push(error.message); }
    if (observerScript) await cdp.send('Page.removeScriptToEvaluateOnNewDocument', { identifier: observerScript });
    if (options.networkProbe) await cdp.send('Network.setCacheDisabled', { cacheDisabled: false });
    stopInterception();
    stopCancellations();
    stopRequestDocuments();
    stopDocumentRetirement();
    stopExceptions();
    await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
    if (result.exceptions.length || interceptionErrors.length) {
      console.error(JSON.stringify({ lastCheck: result.checks.at(-1), exceptions: result.exceptions.slice(0, 3).map(message => message.slice(0, 2500)), interceptionErrors: interceptionErrors.slice(0, 3) }));
      throw new Error('Hydrated browser or interception failed; inspect report.json.');
    }
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
  let documentRenderInFlight = false;
  const renderDocument = async path => {
    if (!canRenderPublicDocument(path)) throw new Error('Only bounded public park/attraction documents can use the compiled SSR renderer.');
    if (documentRenderInFlight) throw new Error('Public review documents must render serially.');
    documentRenderInFlight = true;
    const previousReads = reads;
    reads = [];
    try {
      const response = await application.handle(new Request(new URL(path, publicOrigin), { signal: AbortSignal.timeout(45000) }));
      const html = await response?.text();
      if (response?.status !== 200 || !/<app-root\b/i.test(html ?? '') || !/<h1\b/i.test(html ?? '')) throw new Error(`The actual public document did not render a complete successful Angular page: ${path}`);
      return { status: response.status, html, reads: [...reads] };
    } finally {
      reads = previousReads;
      documentRenderInFlight = false;
    }
  };
  let homeRawHtml;
  let hydratedSmokePassed = false;
  for (const path of paths) {
    reads = [];
    let response;
    replayHomeSsr = path === '/fr/home' && homeSsrFixtures.size > 0;
    try {
      response = await application.handle(new Request(new URL(path, publicOrigin), { signal: AbortSignal.timeout(45000) }));
    } finally { replayHomeSsr = false; }
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
    const routeName = publicRouteEvidenceName(path);
    const htmlPath = join(outputRoot, `${routeName}.html`);
    await writeFile(htmlPath, await staticDocument(html));
    // Fail on real interaction defects before spending minutes on all static
    // widths. A passing run still executes every capture and comparison below.
    if (process.env.CI === 'true' && path === '/fr/home') {
      await captureHydratedHome(homeRawHtml, { renderDocument });
      hydratedSmokePassed = true;
      report.hydratedToStaticReset = await resumeStaticCaptures(cdp);
    }
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
            // A forced static theme switch is not an SSR first-paint test.
            // Wait for its real CSS transition, without editing product styles.
            const expectedBackground = '${theme}' === 'light' ? 'rgb(247, 246, 240)' : 'rgb(21, 26, 23)';
            let settled = false;
            for (let attempt = 0; attempt < 30; attempt++) {
              await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
              if (getComputedStyle(document.body).backgroundColor === expectedBackground) { settled = true; break; }
            }
            if (!settled) throw new Error('Static snapshot theme did not settle.');
            const hero = document.querySelector('.home-hero-card--1 .home-hero-card__content');
            const bodyStyle = getComputedStyle(document.body);
            const outline = document.querySelector('.home-hero__title-line--outline');
            const outlineStyle = outline ? getComputedStyle(outline) : null;
            const itemHero = document.querySelector('.item-detail-hero');
            const itemPhoto = itemHero?.querySelector('.item-detail-hero__photo');
            const standaloneHero = document.querySelector('.standalone-hero');
            const standalonePhoto = document.querySelector('.standalone-main__media');
            const mediaPage = document.querySelector('.park-images-page, .park-item-images-page, .public-video-list, .park-map-page, .park-weather-page, .park-opening-hours-page, .park-pricing-page');
            const mediaHeader = mediaPage?.querySelector('.park-subpage-hero, .public-video-list__hero, .park-weather-hero, .park-opening-hours-hero, .park-pricing-hero');
            const navInner = document.querySelector('.app-public-nav__inner');
            const navLinks = document.querySelector('.app-public-nav__links');
            const navActions = document.querySelector('.app-public-nav__right');
            const navBrand = document.querySelector('.app-public-nav__logo');
            const rect = element => {
              if (!element || getComputedStyle(element).display === 'none') return null;
              const { x, y, width, height, bottom, right } = element.getBoundingClientRect();
              return { x, y, width, height, bottom, right };
            };
            const aliases = Object.fromEntries(['--app-field','--app-map','--app-line','--app-line-weak','--app-on-accent','--app-surface-muted']
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
            return { viewport: innerWidth, scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
              title: document.title, fontStatus: document.fonts.status,
              wordmarkColors: [...document.querySelectorAll('.app-brand-wordmark')].map(mark => getComputedStyle(mark).color),
              wordmarkFonts: [...document.querySelectorAll('.app-brand-wordmark')].map(mark => getComputedStyle(mark).fontFamily),
              homeTitleFontSize: document.querySelector('.home-hero__title') ? getComputedStyle(document.querySelector('.home-hero__title')).fontSize : null,
              homeLogoSurface: document.querySelector('.home-hero-card--1 .home-hero-card__media') ? getComputedStyle(document.querySelector('.home-hero-card--1 .home-hero-card__media')).backgroundColor : null,
              homeOutline: outlineStyle ? { color: outlineStyle.color, stroke: outlineStyle.webkitTextStrokeColor, strokeWidth: outlineStyle.webkitTextStrokeWidth } : null,
              navigationLayout: navInner ? { inner: rect(navInner), links: rect(navLinks), actions: rect(navActions), brand: rect(navBrand) } : null,
              itemHeroLayout: itemHero ? { hero: rect(itemHero), title: rect(itemHero.querySelector('h1')),
                photo: rect(itemPhoto), rating: rect(itemHero.querySelector('.item-detail-hero__rating')),
                photoOpacity: itemPhoto ? getComputedStyle(itemPhoto).opacity : null,
                photoPosition: itemPhoto ? getComputedStyle(itemPhoto).position : null,
                imageSizes: itemPhoto?.querySelector('img')?.getAttribute('sizes') ?? null } : null,
              standaloneLayout: standaloneHero ? { hero: rect(standaloneHero),
                title: rect(standaloneHero.querySelector('h1')), chips: rect(standaloneHero.querySelector('.standalone-hero__chips')),
                body: rect(document.querySelector('.standalone-body')), photo: rect(standalonePhoto),
                sidebar: rect(document.querySelector('.standalone-sidebar')),
                photoOpacity: standalonePhoto ? getComputedStyle(standalonePhoto).opacity : null,
                photoPosition: standalonePhoto ? getComputedStyle(standalonePhoto).position : null,
                image: rect(standalonePhoto?.querySelector('img')) } : null,
              publicMediaLayout: mediaPage && mediaHeader ? { page: rect(mediaPage),
                shell: rect(document.querySelector('.app-public-layout__main')), header: rect(mediaHeader),
                headerRadius: getComputedStyle(mediaHeader).borderRadius,
                headerBackground: getComputedStyle(mediaHeader).backgroundColor,
                titleTransform: getComputedStyle(mediaHeader.querySelector('h1')).textTransform,
                yearLabelWidths: [...document.querySelectorAll('.photo-carousel-years .photo-carousel-axis span')].map(label => label.getBoundingClientRect().width) } : null,
              homeSearchLayout: document.querySelector('.home-command-center') ? {
                query: rect(document.querySelector('.home-command-center .app-search-panel__search-field')),
                category: rect(document.querySelector('.home-command-center .app-search-panel__select-field')),
                submit: rect(document.querySelector('.home-command-center .app-search-panel__submit')),
                clear: rect(document.querySelector('.home-command-center .app-search-panel__clear')),
              } : null,
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
    if (path.endsWith('/home') && route.captures.some(capture => capture.state === 'initial' && capture.homeLogoSurface !== null && capture.homeLogoSurface !== 'rgb(247, 246, 240)')) {
      throw new Error(`An entity logo lost its readable neutral matte: ${path}`);
    }
    if (route.captures.some(capture => {
      if (capture.state !== 'initial' || !capture.navigationLayout) return false;
      const { inner, links, actions, brand } = capture.navigationLayout;
      if (!inner || !actions || !brand || actions.right > capture.viewport + 1 || brand.right > capture.viewport + 1) return true;
      return capture.width > 680
        ? !links || Math.abs(links.width - inner.width) > 1 || Math.abs(links.y - inner.y - 85) > 1 || links.y < actions.bottom || links.y < brand.bottom
        : links !== null || actions.y < brand.bottom;
    })) throw new Error(`The V6 masthead must keep separate brand, utility and navigation rows: ${path}`);
    if (path.endsWith('/home') && route.captures.some(capture => {
      if (capture.state !== 'initial') return false;
      const { query, category, submit, clear } = capture.homeSearchLayout ?? {};
      if (!query || !category || !submit || !clear || clear.y < submit.bottom - 1) return true;
      return capture.width > 680
        ? Math.abs(query.y - category.y) > 1 || Math.abs(category.y - submit.y) > 1
        : category.y < query.bottom - 1 || Math.abs(category.y - submit.y) > 1;
    })) throw new Error(`Home search controls differ from the V6 desktop/mobile composition: ${path}`);
    if (route.captures.some(capture => {
      const layout = capture.standaloneLayout;
      if (capture.state !== 'initial' || !layout) return false;
      if (!layout.title || !layout.chips || layout.title.bottom > layout.chips.y + 1) return true;
      if (!layout.photo) return false;
      return layout.photoOpacity !== '1' || layout.photoPosition !== 'relative'
        || layout.photo.y < layout.hero.bottom - 1 || Math.abs(layout.photo.height - 340) > 1
        || layout.photo.x < layout.body.x - 1 || layout.photo.right > layout.body.right + 1
        || !layout.image || Math.abs(layout.image.width - layout.photo.width + 2) > 1
        || (capture.viewport >= 960 && (!layout.sidebar || layout.sidebar.x < layout.photo.right - 1));
    })) throw new Error(`Standalone photograph must remain separate from its identity and technical sidebar: ${path}`);
    if (route.captures.some(capture => {
      const layout = capture.publicMediaLayout;
      if (capture.state !== 'initial' || !layout) return false;
      return !layout.shell || Math.abs(layout.page.x - layout.shell.x) > 1 || Math.abs(layout.page.width - layout.shell.width) > 1
        || layout.headerRadius !== '0px' || layout.headerBackground !== 'rgba(0, 0, 0, 0)' || layout.titleTransform !== 'none'
        || layout.yearLabelWidths.some(width => width < 90);
    })) throw new Error(`Media, map and practical subpages must share the editorial shell and readable year labels: ${path}`);
    if (route.captures.some((capture) => capture.state === 'initial' && (capture.scrollWidth > capture.viewport + 1 || (capture.heroTitleWidth !== null && capture.heroTitleWidth < 112) || Object.values(capture.aliases).some(value => !value) || capture.wordmarkColors.some(color => color !== (capture.theme === 'light' ? 'rgb(22, 10, 0)' : 'rgb(255, 255, 255)')) || (path !== '/' && capture.footerActiveColor !== capture.headingColor)
      || (capture.itemHeroLayout?.photo && (capture.itemHeroLayout.photoOpacity !== '1' || capture.itemHeroLayout.photoPosition !== 'relative'
        || !Number.isFinite(capture.itemHeroLayout.title?.bottom) || capture.itemHeroLayout.photo.width < 112 || capture.itemHeroLayout.photo.height < 100
        || capture.itemHeroLayout.title.bottom > capture.itemHeroLayout.photo.y + 1
        || capture.itemHeroLayout.photo.x < capture.itemHeroLayout.hero.x - 1 || capture.itemHeroLayout.photo.right > capture.itemHeroLayout.hero.right + 1
        || (capture.viewport > 960 && capture.itemHeroLayout.rating && capture.itemHeroLayout.rating.x < capture.itemHeroLayout.photo.right - 1)))))) {
      throw new Error(`Initial SSR presentation requires correction: ${path}; see report.json.`);
    }
  }
  // Never run the hydrated browser smoke locally: its build and browser workload
  // belong to the isolated CI runner, outside the user's shared memory budget.
  if (process.env.CI === 'true' && homeRawHtml) {
    if (!hydratedSmokePassed) throw new Error('The required hydrated smoke did not complete.');
    await verifyFleuryItemRendering(application);
    if (homeFixtureDocument) {
      const baselineHtml = await readFile(join(outputRoot, 'home-baseline.html'), 'utf8');
      for (const width of [390, 1366]) {
        for (let sampleNumber = 1; sampleNumber <= 3; sampleNumber++) {
          // Alternate order to expose warm-runner/compilation effects rather
          // than treating a single faster or slower navigation as a verdict.
          for (const label of sampleNumber % 2 ? ['master', 'v6'] : ['v6', 'master']) {
            await captureHydratedHome(label === 'master' ? baselineHtml : homeRawHtml,
              { networkProbe: true, label, width, sampleNumber,
                ...(label === 'master' ? { browserRoot: resolve(process.env.V6_BASELINE_DIST_DIR, 'browser') } : {}) });
          }
        }
      }
      report.homeNetworkComparison = {
        scope: 'same-runner lab, cold browser cache, identical SSR and client API populations; decoded bytes, not wire compression; LCP/CLS proxies, not field CWV or INP',
        baseSha: homeFixtureDocument.baseSha,
        observationAfterHydrationMs: 5000,
        repetitions: 3,
        ...compareHomeNetworkSamples(report.homeNetworkSamples),
      };
      const ownerApproval = JSON.parse(await readFile(resolve('../../docs/design-v6/V6-PERFORMANCE-EXCEPTION.json'), 'utf8'));
      report.homeNetworkComparison.acceptance = homeNetworkAcceptance(report.homeNetworkComparison, ownerApproval);
      await writeFile(join(outputRoot, 'home-network-comparison.json'), JSON.stringify(report.homeNetworkComparison, null, 2));
      await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
      if (report.homeNetworkComparison.acceptance.status === 'FAIL') {
        throw new Error('P04.6: initial requests exceed the exact owner-approved focus exception; inspect acceptance.failures.');
      }
      for (const system of ['light', 'dark']) {
        for (const saved of [null, 'light', 'dark']) {
          await captureHydratedHome(homeRawHtml, { networkProbe: true, width: 390,
            label: `theme-${system}-${saved ?? 'auto'}`, initialTheme: { system, saved } });
        }
      }
      await writeFile(join(outputRoot, 'initial-theme-report.json'), JSON.stringify({
        scope: 'real initial-theme script and Angular hydration; system and saved preference; post-first-paint frames, not forced static switches',
        samples: report.initialThemeSamples.map(sample => ({ settings: sample.initialThemeSettings, verdict: sample.initialThemeVerdict, evidence: sample.initialThemeEvidence, captures: sample.captures })),
      }, null, 2));
    }
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
