import { randomUUID } from 'node:crypto';

export const RATING_RANKING_PAGE_GROUP = 'rating-rankings';

export interface SsrPageCacheGenerationStamp {
  readonly allPages: number;
  readonly ratingRankingPages: string | null;
}

export function isRatingRankingDependentCacheKey(cacheKey: string): boolean {
  const path = extractNormalizedPath(cacheKey);
  if (path === null) {
    return false;
  }

  return /^\/[^/]+\/rankings(?:\/|$)/i.test(path)
    || /^\/[^/]+\/park\/[^/]+\/[^/]+$/i.test(path)
    || /^\/[^/]+\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+$/i.test(path);
}

export class SsrPageCacheGenerationTracker {
  private allPagesGeneration = 0;
  private ratingRankingPagesGeneration = randomUUID();

  capture(cacheKey: string): SsrPageCacheGenerationStamp {
    return {
      allPages: this.allPagesGeneration,
      ratingRankingPages: isRatingRankingDependentCacheKey(cacheKey)
        ? this.ratingRankingPagesGeneration
        : null,
    };
  }

  canStore(cacheKey: string, stamp: SsrPageCacheGenerationStamp): boolean {
    if (stamp.allPages !== this.allPagesGeneration) {
      return false;
    }

    return !isRatingRankingDependentCacheKey(cacheKey)
      || stamp.ratingRankingPages === this.ratingRankingPagesGeneration;
  }

  isStoredEntryCurrent(cacheKey: string, ratingRankingGeneration: string | undefined): boolean {
    return !isRatingRankingDependentCacheKey(cacheKey)
      || ratingRankingGeneration === this.ratingRankingPagesGeneration;
  }

  invalidateAll(): void {
    this.allPagesGeneration += 1;
    this.ratingRankingPagesGeneration = randomUUID();
  }

  invalidateRatingRankingPages(): void {
    this.ratingRankingPagesGeneration = randomUUID();
  }
}

function extractNormalizedPath(cacheKey: string): string | null {
  let path: string;

  try {
    path = new URL(cacheKey, 'https://amusement-parks.fun').pathname;
  } catch {
    return null;
  }

  return path.length > 1 && path.endsWith('/') ? path.slice(0, -1) : path;
}
