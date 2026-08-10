export function isPublicPricingSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/pricing\/?$/i.test(path);
}

export function isCriticalPublicPricingSsrRoute(
  path: string,
  hasBlockingQuery: boolean,
): boolean {
  return isPublicPricingSsrRoute(path) && !hasBlockingQuery;
}

export function resolvePricingAwarePageCacheExpiration(
  cacheKey: string,
  nowMs: number,
  defaultTtlMs: number,
): number {
  const defaultExpiration: number = nowMs + Math.max(0, defaultTtlMs);
  const path: string = resolvePath(cacheKey);
  if (!isPublicPricingSsrRoute(path) && !isPublicParkDetailRoute(path)) {
    return defaultExpiration;
  }

  const now: Date = new Date(nowMs);
  if (Number.isNaN(now.getTime())) {
    return defaultExpiration;
  }

  const nextUtcDayMs: number = Date.UTC(
    now.getUTCFullYear(),
    now.getUTCMonth(),
    now.getUTCDate() + 1,
  );
  return Math.min(defaultExpiration, nextUtcDayMs);
}

function isPublicParkDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/?$/i.test(path);
}

function resolvePath(cacheKey: string): string {
  try {
    return new URL(cacheKey, 'https://ssr-cache.local').pathname;
  } catch {
    return cacheKey.split(/[?#]/u, 1)[0] ?? cacheKey;
  }
}
