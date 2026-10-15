import { mkdir, writeFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { homeEvidenceApiPaths, readHomeEvidenceFixtures } from './home-network-evidence.mjs';

if (process.env.CI !== 'true' || process.platform !== 'linux') throw new Error('The baseline renderer is restricted to isolated Linux CI.');
const baselineRoot = resolve(process.env.V6_BASELINE_DIST_DIR);
const evidenceRoot = resolve(process.env.V6_SSR_EVIDENCE_DIR);
const publicOrigin = 'https://amusement-parks.fun';
const nativeFetch = globalThis.fetch;
const records = new Map();
let entry;
globalThis.fetch = async (input, options = {}) => {
  const request = new Request(input, options);
  const url = new URL(request.url);
  const target = url.origin === 'http://api:8080' ? new URL(`/api${url.pathname}${url.search}`, publicOrigin) : url;
  if (request.method !== 'GET' || target.origin !== publicOrigin || !homeEvidenceApiPaths.has(target.pathname)) {
    throw new Error('Baseline renderer rejected a non-home or mutating request.');
  }
  const headers = new Headers(request.headers);
  for (const name of ['authorization', 'cookie', 'X-AmusementPark-Internal-SSR']) headers.delete(name);
  const response = await nativeFetch(target, { method: 'GET', headers, signal: AbortSignal.timeout(20000) });
  const bytes = Buffer.from(await response.clone().arrayBuffer());
  const key = `${target.pathname}${target.search}`;
  const record = { key, status: response.status, contentType: response.headers.get('content-type'), bodyBase64: bytes.toString('base64') };
  readHomeEvidenceFixtures([record]);
  records.set(key, record);
  return response;
};
try {
  await mkdir(evidenceRoot, { recursive: true });
  entry = await import(pathToFileURL(join(baselineRoot, 'server/main.server.mjs')));
  const app = entry.ɵgetOrCreateAngularServerApp();
  const response = await app.handle(new Request(`${publicOrigin}/fr/home`, { signal: AbortSignal.timeout(45000) }));
  const html = await response?.text();
  if (!html?.includes('home-search-input') || !html.includes('ng-state')) throw new Error('Baseline home SSR or TransferState is missing.');
  const clientRecords = [];
  for (const record of records.values()) {
    // Production intentionally refreshes random parks without TransferState.
    // Preserve that second real population in both browser passes, not a fixed
    // single population which would hide the cost of replacing hero images.
    if (new URL(record.key, publicOrigin).pathname === '/api/parks/random-visible') {
      const refreshed = await nativeFetch(new URL(record.key, publicOrigin), { signal: AbortSignal.timeout(20000) });
      clientRecords.push({ key: record.key, status: refreshed.status, contentType: refreshed.headers.get('content-type'), bodyBase64: Buffer.from(await refreshed.arrayBuffer()).toString('base64') });
    } else clientRecords.push(record);
  }
  readHomeEvidenceFixtures(clientRecords);
  await writeFile(join(evidenceRoot, 'home-baseline.html'), html);
  await writeFile(join(evidenceRoot, 'home-public-fixtures.json'), JSON.stringify({ baseSha: process.env.V6_BASELINE_SHA, ssr: [...records.values()], client: clientRecords }, null, 2));
  console.log(`Baseline home: ${records.size} successful anonymous API fixtures; source ${process.env.V6_BASELINE_SHA}.`);
} finally {
  entry?.ɵdestroyAngularServerApp();
  globalThis.fetch = nativeFetch;
}
