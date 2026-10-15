export const homeEvidenceApiPaths = new Set([
  '/api/history/articles/latest', '/api/public-stats/home', '/api/parks/random-visible',
  '/api/parks/home-latest', '/api/parks/home-featured',
]);

export function readHomeEvidenceFixtures(records) {
  if (!Array.isArray(records) || records.length > 16) throw new Error('Invalid bounded home fixture collection.');
  const fixtures = new Map();
  let totalBytes = 0;
  for (const record of records) {
    const url = new URL(record.key, 'https://amusement-parks.fun');
    if (!record.key.startsWith('/api/') || !homeEvidenceApiPaths.has(url.pathname)
      || url.origin !== 'https://amusement-parks.fun' || fixtures.has(record.key)
      || record.status !== 200 || !record.contentType?.includes('application/json')
      || typeof record.bodyBase64 !== 'string' || record.bodyBase64.length > 2800000) {
      throw new Error('Unexpected home fixture; only successful anonymous home APIs are accepted.');
    }
    const bytes = Buffer.from(record.bodyBase64, 'base64');
    totalBytes += bytes.length;
    if (totalBytes > 8000000) throw new Error('Home fixture exceeds its bounded evidence budget.');
    JSON.parse(bytes.toString('utf8'));
    fixtures.set(record.key, { ...record, bytes });
  }
  return fixtures;
}

export function summarizeHomeResources(resources) {
  const byType = {};
  for (const resource of resources) {
    const bucket = byType[resource.type] ??= { requests: 0, decodedBytes: 0 };
    bucket.requests++;
    bucket.decodedBytes += resource.decodedBytes ?? 0;
  }
  return { requests: resources.length, decodedBytes: Object.values(byType).reduce((sum, item) => sum + item.decodedBytes, 0), byType };
}

// Browser-only diagnostic injection. No app code or persistence is modified.
export const homeVitalsObserver = `(() => {
  window.__apfHomeVitals = { lcpMs: null, cls: 0, interactionDurations: [], eventTimingAvailable: PerformanceObserver.supportedEntryTypes.includes('event') };
  new PerformanceObserver(list => {
    for (const entry of list.getEntries()) window.__apfHomeVitals.lcpMs = entry.startTime;
  }).observe({ type: 'largest-contentful-paint', buffered: true });
  new PerformanceObserver(list => {
    for (const entry of list.getEntries()) if (!entry.hadRecentInput) window.__apfHomeVitals.cls += entry.value;
  }).observe({ type: 'layout-shift', buffered: true });
  if (window.__apfHomeVitals.eventTimingAvailable) new PerformanceObserver(list => {
    for (const entry of list.getEntries()) if (entry.interactionId && window.__apfHomeVitals.interactionDurations.length < 64) window.__apfHomeVitals.interactionDurations.push(entry.duration);
  }).observe({ type: 'event', buffered: true, durationThreshold: 16 });
})();`;
