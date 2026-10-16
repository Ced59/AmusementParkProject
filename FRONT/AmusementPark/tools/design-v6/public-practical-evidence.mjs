export function publicPracticalReadEvidence(target, status, bytes) {
  const url = new URL(target, 'https://amusement-parks.fun');
  if (!/^\/api\/parks\/[0-9a-f-]{36}\/weather\/historical-comparisons$/i.test(url.pathname) || status !== 200) return undefined;
  if (bytes.byteLength > 2 * 1024 * 1024) throw new Error('Public practical evidence exceeds its response bound.');
  const parsed = JSON.parse(Buffer.from(bytes).toString('utf8'));
  const data = parsed.data ?? parsed;
  if (!Array.isArray(data.years) || data.years.length > 20) throw new Error('Invalid bounded weather history evidence.');
  return { kind: 'weather-history', yearsCount: data.years.length };
}
