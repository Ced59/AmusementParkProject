import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

// Initial SSR only: this tool does not start a server, hydrate Angular, authenticate,
// exercise mutations or claim to replace browser workflow / visual-diff acceptance.
const publicOrigin = 'https://amusement-parks.fun';
const browserRoot = resolve('dist/amusement-park/browser');
const outputRoot = resolve(process.env.V6_SSR_EVIDENCE_DIR ?? '../../docs/design-v6/evidence/public-ssr');
const widths = (process.env.V6_SSR_WIDTHS ?? '320,360,390,414,480,680,768,960,1024,1180,1366,1440,1920')
  .split(',').map(Number);
if (widths.some((width) => !Number.isInteger(width) || width < 320 || width > 1920)) {
  throw new Error('Screenshot widths must be integers between 320 and 1920.');
}
const paths = process.env.V6_SSR_PATHS?.split(',') ?? [
  ...['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'].map((language) => `/${language}/home`),
  '/fr/parks',
  '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury',
  '/fr/about', '/fr/contact', '/fr/privacy', '/fr/not-found',
];
if (paths.some((path) => !/^\/(fr|en|de|nl|it|es|pt|pl)\//.test(path) || /\/admin|\/profile/.test(path))) {
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

async function connect(webSocketUrl) {
  const socket = new WebSocket(webSocketUrl);
  await new Promise((yes, no) => { socket.addEventListener('open', yes, { once: true }); socket.addEventListener('error', no, { once: true }); });
  let sequence = 0;
  const pending = new Map();
  socket.addEventListener('message', ({ data }) => {
    const message = JSON.parse(data);
    if (!message.id) return;
    const handler = pending.get(message.id);
    pending.delete(message.id);
    message.error ? handler?.reject(new Error(message.error.message)) : handler?.resolve(message.result);
  });
  return {
    close: () => socket.close(),
    send: (method, params = {}) => new Promise((resolveRequest, rejectRequest) => {
      const id = ++sequence;
      const timer = setTimeout(() => { pending.delete(id); rejectRequest(new Error(`CDP timeout: ${method}`)); }, 30000);
      pending.set(id, { resolve: (value) => { clearTimeout(timer); resolveRequest(value); }, reject: (error) => { clearTimeout(timer); rejectRequest(error); } });
      socket.send(JSON.stringify({ id, method, params }));
    }),
  };
}

const tempRoot = await mkdtemp(join(tmpdir(), 'apf-v6-ssr-'));
let chrome;
let cdp;
let entry;
const report = {
  scope: 'Angular initial SSR, anonymous public data, no hydration or workflow acceptance',
  buildSha: process.env.GITHUB_SHA ?? 'UNVERIFIED_LOCAL_BUILD',
  sourceHeadSha: process.env.V6_SOURCE_HEAD_SHA ?? 'UNVERIFIED_LOCAL_BUILD',
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
  entry = await import(pathToFileURL(resolve('dist/amusement-park/server/main.server.mjs')));
  const application = entry.ɵgetOrCreateAngularServerApp();
  for (const path of paths) {
    reads = [];
    const response = await application.handle(new Request(new URL(path, publicOrigin), { signal: AbortSignal.timeout(45000) }));
    if (!response) throw new Error(`No Angular route matched ${path}`);
    const html = await response.text();
    if (!/<app-root\b[^>]*>[\s\S]*?<h1\b/i.test(html)) throw new Error(`Missing initial Angular content for ${path}`);
    const routeName = path.slice(1).replaceAll('/', '_');
    const htmlPath = join(outputRoot, `${routeName}.html`);
    await writeFile(htmlPath, await staticDocument(html));
    const route = { path, engineStatus: response.status, bytes: Buffer.byteLength(html), reads: [...reads], captures: [] };
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
          expression: `(async () => { document.body.classList.remove('light-mode','dark-mode'); document.body.classList.add('${theme}-mode'); await document.fonts.ready; await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))); const hero = document.querySelector('.home-hero-card--1 .home-hero-card__content'); return { viewport: innerWidth, scrollWidth: document.documentElement.scrollWidth, title: document.title, fontStatus: document.fonts.status, heroTitleWidth: hero ? hero.getBoundingClientRect().width : null }; })()`,
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
          route.captures.push({ width, theme, state: 'consent-hidden-for-visual-inspection-only', screenshot: cleanScreenshot });
          await cdp.send('Runtime.evaluate', { expression: `document.querySelector('app-cookie-consent-banner').style.removeProperty('display')` });
        }
      }
    }
    console.log(`${path}: ${route.captures.length} initial SSR captures; ${reads.length} anonymous API reads`);
    await writeFile(join(outputRoot, 'report.json'), JSON.stringify(report, null, 2));
    if (route.captures.some((capture) => capture.scrollWidth > capture.viewport + 1 || (capture.heroTitleWidth !== null && capture.heroTitleWidth < 112))) {
      throw new Error(`Initial SSR geometry requires correction: ${path}; see report.json.`);
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
