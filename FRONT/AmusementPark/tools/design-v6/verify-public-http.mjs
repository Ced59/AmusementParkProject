import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { createServer } from 'node:net';
import { resolve, join } from 'node:path';

// Never start an application server on the owner's Windows PC. This acceptance
// check runs serially after Chromium has closed, on the isolated Linux CI runner.
if (process.env.CI !== 'true' || process.platform !== 'linux') {
  throw new Error('Public HTTP acceptance is restricted to the isolated Linux CI runner.');
}
const evidenceRoot = resolve(process.env.V6_SSR_EVIDENCE_DIR ?? '.tmp-v6-http');
await mkdir(evidenceRoot, { recursive: true });
const publicOrigin = 'https://amusement-parks.fun';
const version = JSON.parse(await readFile('release-version.json', 'utf8')).version;
const report = { scope: 'compiled Express SSR on isolated CI; anonymous GET only; not a deployment proof', version, sourceHeadSha: process.env.V6_SOURCE_HEAD_SHA, checks: [], requests: [] };
const portProbe = createServer();
portProbe.listen(0, '127.0.0.1');
await once(portProbe, 'listening');
const port = portProbe.address().port;
await new Promise((yes, no) => portProbe.close(error => error ? no(error) : yes()));
let serverLog = '';
let serverError;
let lastDocument;
const server = spawn(process.execPath, ['--max-old-space-size=768', 'dist/amusement-park/server/server.mjs'], {
  stdio: ['ignore', 'pipe', 'pipe'],
  env: {
    ...process.env, PORT: String(port),
    SSR_ALLOWED_HOSTS: 'localhost;127.0.0.1;amusement-parks.fun',
    SSR_API_INTERNAL_URL: `${publicOrigin}/api`,
    SSR_RENDER_MAX_CONCURRENCY: '1', SSR_PAGE_CACHE_MAX_ENTRIES: '64',
    SSR_DISK_PAGE_CACHE_ENABLED: 'false', SSR_TECHNICAL_STATS_PERSISTENCE_ENABLED: 'false',
    SSR_SEO_STATIC_SNAPSHOT_ENABLED: 'false',
  },
});
const serverClosed = new Promise(yes => server.once('close', yes));
server.on('error', error => { serverError = error; });
for (const stream of [server.stdout, server.stderr]) {
  stream.on('data', bytes => { serverLog = (serverLog + bytes.toString()).slice(-150000); });
}
const userAgents = {
  bing: 'Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)',
  google: 'Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)',
  browser: 'Mozilla/5.0 APF-V6-Anonymous-CI-Review',
};
async function get(path, agent = 'bing') {
  const started = performance.now();
  const response = await fetch(`http://127.0.0.1:${port}${path}`, {
    method: 'GET', redirect: 'manual', signal: AbortSignal.timeout(60000),
    headers: { accept: 'text/html', host: 'amusement-parks.fun', 'x-forwarded-proto': 'https', 'user-agent': userAgents[agent] },
  });
  const html = await response.text();
  lastDocument = { path, html };
  const record = { path, agent, status: response.status, milliseconds: Math.round(performance.now() - started), htmlBytes: Buffer.byteLength(html), headers: Object.fromEntries(response.headers) };
  report.requests.push(record);
  return { response, html, record };
}
function attribute(tag, name) {
  return new RegExp(`\\s${name}\\s*=\\s*(["'])(.*?)\\1`, 'i').exec(tag)?.[2]?.replaceAll('&amp;', '&') ?? '';
}
function meta(html, name) {
  const tag = (html.match(/<meta\b[^>]*>/gi) ?? []).find(tag => attribute(tag, 'name') === name || attribute(tag, 'property') === name);
  return tag ? attribute(tag, 'content') : '';
}
function check(ok, label) {
  assert.ok(ok, label);
  report.checks.push(label);
}
const park = '/fr/park/4e0155db-9dbf-4755-a32f-0cc8950d61c4/le-fleury';
try {
  let healthy = false;
  for (let attempt = 0; attempt < 100; attempt++) {
    if (serverError) throw serverError;
    if (server.exitCode !== null) throw new Error('CI SSR process exited before readiness.');
    try {
      const health = await fetch(`http://127.0.0.1:${port}/healthz`, { signal: AbortSignal.timeout(1000) });
      if (health.ok && (await health.text()).trim() === 'ok') { healthy = true; break; }
    } catch { /* process may still be loading the compiled bundle */ }
    await new Promise(yes => setTimeout(yes, 200));
  }
  check(healthy, 'compiled Express health endpoint is ready');
  const routes = [
    ...['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'].map(lang => ({ path: `/${lang}/home`, status: 200, lang, home: true })),
    { path: '/fr/parks', status: 200, lang: 'fr' },
    { path: park, status: 200, lang: 'fr' },
    { path: `${park}/item/088b092c-808f-4eba-9407-34141caa94dc/rhaegal`, status: 200, lang: 'fr' },
    { path: '/fr/attraction/46a57823-75cd-4ff3-9fde-495ad0204b1b/hasenhorn-coaster', status: 200, lang: 'fr' },
    { path: '/fr/park/94a4c4df-5ab6-4dc3-b756-2de664e947c4/efteling/pricing', status: 200, lang: 'fr' },
    { path: `${park}/pricing`, status: 404, lang: 'fr', empty: true },
    { path: '/fr/not-found', status: 404, lang: 'fr' },
    { path: '/fr/apf-v6-nonexistent-route', status: 404, lang: 'fr' },
  ];
  for (const route of routes) {
    const { response, html, record } = await get(route.path);
    check(response.status === route.status, `${route.path}: HTTP ${route.status}`);
    check(response.headers.get('x-amusementpark-build-version') === version, `${route.path}: current build version`);
    check(Boolean(response.headers.get('content-security-policy')), `${route.path}: CSP retained`);
    check(/<title[^>]*>[^<]{8,}<\/title>/i.test(html), `${route.path}: useful title`);
    check(meta(html, 'description').length >= 30, `${route.path}: useful description`);
    check(new RegExp(`<html\\b[^>]*\\blang=["']${route.lang}["']`, 'i').test(html), `${route.path}: HTML language`);
    check(meta(html, 'og:locale').startsWith(`${route.lang}_`), `${route.path}: localized Open Graph`);
    check(!/<script\b(?![^>]*\btype=["']application\/ld\+json["'])/i.test(html), `${route.path}: Bing has no executable scripts`);
    if (route.status === 404) {
      check(/noindex/i.test(`${response.headers.get('x-robots-tag')} ${meta(html, 'robots')}`), `${route.path}: missing content is not indexable`);
    } else {
      const canonicalTag = (html.match(/<link\b[^>]*>/gi) ?? []).find(tag => attribute(tag, 'rel') === 'canonical');
      const canonical = canonicalTag ? attribute(canonicalTag, 'href') : '';
      check(canonical === `${publicOrigin}${route.path}`, `${route.path}: canonical public URL`);
      check(/<h1\b[^>]*>[\s\S]+?<\/h1>/i.test(html), `${route.path}: initial SSR heading`);
      const jsonLd = [...html.matchAll(/<script\b[^>]*\btype=["']application\/ld\+json["'][^>]*>([\s\S]*?)<\/script>/gi)];
      for (const [, json] of jsonLd) JSON.parse(json);
      record.jsonLdBlocks = jsonLd.length;
      // Existing Home SEO deliberately emits no JSON-LD (verified on V5 and in
      // SeoService.applyHomeSeo). Do not invent structured data during a UI-only
      // migration; entity/directory pages still require their actual blocks.
      check(route.home ? jsonLd.length === 0 : jsonLd.length > 0,
        `${route.path}: existing structured-data contract`);
    }
    if (route.home) {
      const alternates = (html.match(/<link\b[^>]*>/gi) ?? []).filter(tag => attribute(tag, 'rel') === 'alternate');
      check(['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'].every(lang => alternates.some(tag => attribute(tag, 'hreflang') === lang && attribute(tag, 'href') === `${publicOrigin}/${lang}/home`)), `${route.path}: eight real language alternates`);
    }
    console.log(`HTTP SSR ${route.path}: ${response.status}`);
  }
  // The same cached document must keep full Google HTML and no-JS Bing HTML.
  for (const agent of ['google', 'bing']) {
    const { response, html } = await get('/fr/home', agent);
    check(response.headers.get('x-amusementpark-ssr-cache') === 'HIT', `${agent}: warm public cache`);
    if (agent === 'google') {
      check(/<script\b[^>]*\bsrc=["'][^"']+\.m?js/i.test(html), 'Google cached SSR retains executable assets');
      check(/<script\b[^>]*\bid=["']ng-state["']/i.test(html), 'Google cached SSR retains TransferState');
      check(/\bclass=["'][^"']+/i.test(html), 'Google cached SSR retains presentation');
    } else {
      check(!/<script\b(?![^>]*\btype=["']application\/ld\+json["'])/i.test(html), 'Bing cache remains script-free after Google');
    }
  }
  for (const path of ['/fr/admin', '/fr/profile', '/fr/park-fit', '/fr/forgot-password']) {
    const { response, html } = await get(path, 'browser');
    check(response.status === 200, `${path}: existing CSR HTTP status`);
    check(/noindex/i.test(`${response.headers.get('x-robots-tag')} ${meta(html, 'robots')}`), `${path}: private/CSR noindex`);
    check(response.headers.get('x-amusementpark-ssr-fallback') !== null, `${path}: user-specific route remains CSR`);
  }
  // A synthetic legacy ID exercises only URL normalization, never entity data.
  const legacy = `${park}/video/s/00000000-0000-0000-0000-000000000000/ci-route-shape?language=fr`;
  const redirect = await get(legacy);
  check(redirect.response.status === 301, 'legacy video path preserves permanent redirect');
  check(redirect.response.headers.get('location') === legacy.replace('/video/s/', '/videos/'), 'legacy video redirect preserves path and query');
  const servedVersion = await get('/version.json', 'browser');
  check(JSON.parse(servedVersion.html).version === version, 'served version matches the tested bundle');
  report.passed = true;
  console.log(`Public HTTP SSR: ${report.requests.length} serial GETs, ${report.checks.length} checks passed.`);
} catch (error) {
  report.passed = false;
  report.error = error.stack;
  if (lastDocument) {
    report.failedDocumentPath = lastDocument.path;
    await writeFile(join(evidenceRoot, 'failed-http-response.html'), lastDocument.html);
  }
  throw error;
} finally {
  // Stop only the process this test owns, even if assertions or startup fail.
  if (server.exitCode === null && server.signalCode === null) server.kill('SIGTERM');
  const deadline = setTimeout(() => server.kill('SIGKILL'), 10000);
  try { await serverClosed; } finally { clearTimeout(deadline); }
  await writeFile(join(evidenceRoot, 'http-ssr-report.json'), JSON.stringify(report, null, 2));
  await writeFile(join(evidenceRoot, 'http-ssr-server.log'), serverLog);
}
