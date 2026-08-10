import {
  isCriticalPublicPricingSsrRoute,
  isPublicPricingSsrRoute,
  resolvePricingAwarePageCacheExpiration,
} from './public-pricing-ssr-policy';

describe('public pricing SSR policy', () => {
  it('includes localized park pricing pages in the SSR cache whitelist', () => {
    expect(isPublicPricingSsrRoute('/fr/park/park-1/parc-test/pricing')).toBe(true);
    expect(isPublicPricingSsrRoute('/en/park/park-1/test-park/pricing/')).toBe(true);
    expect(isPublicPricingSsrRoute('/fr/park/park-1/parc-test/pricing/extra')).toBe(false);
  });

  it('treats pricing as critical only without a blocking query string', () => {
    const path: string = '/fr/park/park-1/parc-test/pricing';

    expect(isCriticalPublicPricingSsrRoute(path, false)).toBe(true);
    expect(isCriticalPublicPricingSsrRoute(path, true)).toBe(false);
  });

  it('expires park detail and pricing HTML at the next UTC date rollover', () => {
    const nowMs: number = Date.UTC(2026, 7, 9, 23, 50);
    const nextUtcDayMs: number = Date.UTC(2026, 7, 10);
    const oneDayMs: number = 24 * 60 * 60 * 1_000;

    expect(resolvePricingAwarePageCacheExpiration(
      'https://amusement-parks.fun/fr/park/park-1/parc-test',
      nowMs,
      oneDayMs,
    )).toBe(nextUtcDayMs);
    expect(resolvePricingAwarePageCacheExpiration(
      '/fr/park/park-1/parc-test/pricing?source=direct',
      nowMs,
      oneDayMs,
    )).toBe(nextUtcDayMs);
  });

  it('keeps the configured TTL for unrelated pages', () => {
    const nowMs: number = Date.UTC(2026, 7, 9, 23, 50);
    const ttlMs: number = 24 * 60 * 60 * 1_000;

    expect(resolvePricingAwarePageCacheExpiration('/fr/about', nowMs, ttlMs)).toBe(nowMs + ttlMs);
  });
});
