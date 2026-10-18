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

export function homeBrandImageRequestSaving(before, after) {
  if (!Array.isArray(before.resources) || !Array.isArray(after.resources)) return null;
  const brandPath = '/assets/general-icon/logo-amusementpark-ui-36.webp';
  const images = sample => sample.resources.filter(resource => resource.type === 'Image').map(resource => resource.path);
  const beforePaths = images(before);
  const afterPaths = images(after);
  if ([...beforePaths, ...afterPaths].some(path => typeof path !== 'string' || !path.startsWith('/'))
      || beforePaths.length !== before.summary?.byType?.Image?.requests || afterPaths.length !== after.summary?.byType?.Image?.requests
      || beforePaths.filter(path => path === brandPath).length !== 1 || afterPaths.includes(brandPath)
      || beforePaths.length !== afterPaths.length + 1
      || JSON.stringify(beforePaths.filter(path => path !== brandPath).sort()) !== JSON.stringify([...afterPaths].sort())) return null;
  return { type: 'Image', path: brandPath, beforeRequests: beforePaths.length, afterRequests: afterPaths.length, requestsSaved: 1,
    reason: 'Separate original brand pictogram removed by the approved redesign; every other image request path is unchanged.' };
}

export function compareHomeNetworkSamples(samples, widths = [390, 1366], repetitions = 3) {
  const comparisons = [];
  for (const width of widths) {
    for (let sampleNumber = 1; sampleNumber <= repetitions; sampleNumber++) {
      const pair = ['master', 'v6'].map(label => {
        const matches = samples.filter(sample => sample.width === width && sample.sampleNumber === sampleNumber && sample.label === label);
        if (matches.length !== 1) throw new Error('Missing or duplicate paired home network evidence.');
        const sample = matches[0];
        if (!Array.isArray(sample.vitals?.heroNames) || !sample.vitals.heroNames.length
          || !['lcpMs', 'cls', 'domCompleteMs'].every(metric => Number.isFinite(sample.vitals[metric]) && sample.vitals[metric] >= 0)
          || !Number.isInteger(sample.summary?.requests) || sample.summary.requests < 0
          || !Number.isInteger(sample.summary?.decodedBytes) || sample.summary.decodedBytes < 0) {
          throw new Error('Incomplete home timing or resource evidence; no performance verdict is possible.');
        }
        return sample;
      });
      const [before, after] = pair;
      if (JSON.stringify(before.vitals.heroNames) !== JSON.stringify(after.vitals.heroNames)) throw new Error('Home comparison populations differ; the comparison is invalid.');
      comparisons.push({ width, sampleNumber, additionalRequests: after.summary.requests - before.summary.requests,
        decodedBytesDelta: after.summary.decodedBytes - before.summary.decodedBytes,
        before: before.summary, after: after.summary, beforeVitals: before.vitals, afterVitals: after.vitals,
        brandImageSaving: homeBrandImageRequestSaving(before, after) });
    }
  }
  const range = values => {
    const sorted = [...values].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    return { min: sorted[0], median: sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2, max: sorted.at(-1) };
  };
  return { comparisons, aggregates: widths.map(width => {
    const pairs = comparisons.filter(pair => pair.width === width);
    return { width, repetitions, before: Object.fromEntries(['lcpMs', 'cls', 'domCompleteMs'].map(metric => [metric, range(pairs.map(pair => pair.beforeVitals[metric]))])),
      after: Object.fromEntries(['lcpMs', 'cls', 'domCompleteMs'].map(metric => [metric, range(pairs.map(pair => pair.afterVitals[metric]))])),
      additionalRequests: range(pairs.map(pair => pair.additionalRequests)), decodedBytesDelta: range(pairs.map(pair => pair.decodedBytesDelta)) };
  }) };
}

export function homeNetworkAcceptance(report, approval = null) {
  if (!Array.isArray(report.comparisons) || report.comparisons.length === 0) {
    return { status: 'FAIL', exceptions: [], failures: [{ reason: 'missing-comparisons' }] };
  }
  const exceptions = [];
  const failures = [];
  for (const comparison of report.comparisons) {
    const { width, sampleNumber, before, after, additionalRequests } = comparison;
    if (additionalRequests !== after.requests - before.requests) {
      failures.push({ width, sampleNumber, reason: 'inconsistent-request-count' });
      continue;
    }
    if (additionalRequests <= 0) continue;
    // Only these two exact, recorded owner decisions are accepted. An arbitrary
    // larger value in the JSON file cannot silently relax the performance gate.
    const approvedCap = approval?.id === 'P04.6-modal-focus-2026-10-16'
      && approval.approval === 'Oui, accepter cette exception ciblée' ? 3302
      : approval?.id === 'P04.6-modal-focus-fixed-controls-2026-10-17'
        && approval.approval === "Oui et oui et continue jusqu'à merge" ? 4565 : null;
    const approved = approvedCap !== null && approval.approvedBy === 'repository-owner'
      && approval.baselineSha === report.baseSha
      && approval.maximumAdditionalScriptRequests === 2 && approval.maximumScriptDecodedBytesDelta === approvedCap;
    if (!approved) {
      failures.push({ width, sampleNumber, reason: 'no-matching-owner-approval' });
      continue;
    }
    const types = new Set([...Object.keys(before.byType ?? {}), ...Object.keys(after.byType ?? {})]);
    const saving = comparison.brandImageSaving;
    const knownBrandSaving = saving?.type === 'Image' && saving.path === '/assets/general-icon/logo-amusementpark-ui-36.webp'
      && saving.requestsSaved === 1 && saving.beforeRequests === before.byType?.Image?.requests
      && saving.afterRequests === after.byType?.Image?.requests && saving.beforeRequests === saving.afterRequests + 1;
    const extraOtherType = [...types].some(type => type !== 'Script'
      && (after.byType?.[type]?.requests ?? 0) !== (before.byType?.[type]?.requests ?? 0)
      && !(type === 'Image' && knownBrandSaving));
    const scriptRequests = (after.byType?.Script?.requests ?? 0) - (before.byType?.Script?.requests ?? 0);
    const scriptBytes = after.byType?.Script?.decodedBytes - before.byType?.Script?.decodedBytes;
    if (extraOtherType || scriptRequests - (knownBrandSaving ? 1 : 0) !== additionalRequests || scriptRequests !== 2
      || !Number.isInteger(scriptBytes) || scriptBytes > approvedCap) {
      failures.push({ width, sampleNumber, reason: 'outside-approved-modal-focus-bounds', scriptRequests, scriptBytes });
      continue;
    }
    exceptions.push({ width, sampleNumber, approvalId: approval.id, scriptRequests, scriptDecodedBytesDelta: scriptBytes,
      ...(knownBrandSaving ? { presentationRequestSaving: saving } : {}) });
  }
  return { status: failures.length ? 'FAIL' : exceptions.length ? 'PASS_WITH_OWNER_EXCEPTION' : 'PASS', exceptions, failures };
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
