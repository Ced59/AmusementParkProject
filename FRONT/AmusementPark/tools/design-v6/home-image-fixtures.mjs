import { createHash } from 'node:crypto';
import { homeEvidenceApiPaths } from './home-network-evidence.mjs';

const publicOrigin = 'https://amusement-parks.fun';
const widths = [64, 320, 480, 640];
const maximumBytes = 16 * 1024 * 1024;

export function homeImageFixturePlan(homeFixtures) {
  if (!(homeFixtures instanceof Map) || homeFixtures.size > 16) throw new Error('Invalid bounded home fixture input.');
  const ids = new Set();
  for (const [key, record] of homeFixtures) {
    const url = new URL(key, publicOrigin);
    if (!key.startsWith('/api/') || url.origin !== publicOrigin || !homeEvidenceApiPaths.has(url.pathname)
        || record.status !== 200 || !Buffer.isBuffer(record.bytes) || record.bytes.length > 2097152) {
      throw new Error('Only the exact successful anonymous home population can identify images.');
    }
    const body = JSON.parse(record.bytes.toString('utf8'));
    if (url.pathname === '/api/public-stats/home') continue;
    if (!Array.isArray(body) || body.length > 10) throw new Error('Unexpected home image population.');
    const field = url.pathname === '/api/history/articles/latest' ? 'mainImageId' : 'currentLogoImageId';
    for (const item of body) {
      const id = item[field];
      if (id === null || id === undefined) continue;
      if (typeof id !== 'string' || !/^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})$/i.test(id)) {
        throw new Error('Invalid public home image identifier.');
      }
      ids.add(id);
      if (ids.size > 16) throw new Error('Home image population exceeds its bound.');
    }
  }
  return [...ids].sort().flatMap(id => widths.map(width => `/api/images/binary/${id}?width=${width}&v=${width <= 192 ? 3 : 2}`));
}

// Collection happens serially BEFORE the isolated account browser. That browser
// never forwards a request, and no placeholder or invented image is substituted.
export async function captureHomeImageFixtures(homeFixtures, fetcher, { ci = process.env.CI, platform = process.platform } = {}) {
  if (ci !== 'true' || platform !== 'linux') throw new Error('Public home image collection is restricted to isolated Linux CI.');
  const fixtures = new Map();
  let totalBytes = 0;
  for (const key of homeImageFixturePlan(homeFixtures)) {
    const response = await fetcher(new URL(key, publicOrigin), {
      method: 'GET', redirect: 'error', headers: { accept: 'image/webp,image/png,image/jpeg' }, signal: AbortSignal.timeout(20000),
    });
    const contentType = response.headers.get('content-type')?.split(';')[0];
    const bytes = Buffer.from(await response.arrayBuffer());
    totalBytes += bytes.length;
    if (response.status !== 200 || !['image/webp', 'image/png', 'image/jpeg'].includes(contentType)
        || bytes.length === 0 || bytes.length > 1048576 || totalBytes > maximumBytes) {
      throw new Error(`Public home image fixture is unavailable or exceeds its bound: ${key}`);
    }
    fixtures.set(key, { status: 200, contentType, bytes, sha256: createHash('sha256').update(bytes).digest('hex') });
  }
  return fixtures;
}

export function homeImageFixtureResponse(request, imageFixtures) {
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== 'https://apf-account-layout.invalid' || url.hash
      || !(imageFixtures instanceof Map) || imageFixtures.size > 64) return null;
  const match = /^\/api\/images\/binary\/(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})\?width=(64|320|480|640)&v=([23])$/i.exec(url.pathname + url.search);
  if (!match || Number(match[2]) !== (Number(match[1]) <= 192 ? 3 : 2)) return null;
  const record = imageFixtures.get(url.pathname + url.search);
  return record?.status === 200 && ['image/webp', 'image/png', 'image/jpeg'].includes(record.contentType)
    && Buffer.isBuffer(record.bytes) && record.bytes.length > 0 && record.bytes.length <= 1048576 ? record : null;
}
