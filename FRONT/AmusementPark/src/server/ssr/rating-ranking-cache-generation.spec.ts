import { describe, expect, it } from 'vitest';
import {
  isRatingRankingDependentCacheKey,
  SsrPageCacheGenerationTracker,
} from './rating-ranking-cache-generation';

describe('rating ranking SSR cache generations', () => {
  it.each([
    'https://amusement-parks.fun/fr/rankings',
    'https://amusement-parks.fun/en/rankings/methodology',
    'https://amusement-parks.fun/de/park/park-1/park-name',
    'https://amusement-parks.fun/es/park/park-1/park-name/item/item-1/item-name?source=test',
  ])('recognizes a page whose HTML depends on published ranks: %s', (cacheKey: string) => {
    expect(isRatingRankingDependentCacheKey(cacheKey)).toBe(true);
  });

  it.each([
    'https://amusement-parks.fun/fr',
    'https://amusement-parks.fun/fr/parks',
    'https://amusement-parks.fun/fr/park/park-1/park-name/images',
    'https://amusement-parks.fun/fr/park/park-1/park-name/item/item-1/item-name/history',
  ])('does not include unrelated pages in the rating ranking group: %s', (cacheKey: string) => {
    expect(isRatingRankingDependentCacheKey(cacheKey)).toBe(false);
  });

  it('blocks a ranking-dependent render started before a ranking invalidation', () => {
    const tracker = new SsrPageCacheGenerationTracker();
    const rankingKey = 'https://amusement-parks.fun/fr/rankings';
    const unrelatedKey = 'https://amusement-parks.fun/fr/parks';
    const rankingStamp = tracker.capture(rankingKey);
    const unrelatedStamp = tracker.capture(unrelatedKey);

    tracker.invalidateRatingRankingPages();

    expect(tracker.canStore(rankingKey, rankingStamp)).toBe(false);
    expect(tracker.canStore(unrelatedKey, unrelatedStamp)).toBe(true);
  });

  it('blocks every render started before a full invalidation', () => {
    const tracker = new SsrPageCacheGenerationTracker();
    const cacheKey = 'https://amusement-parks.fun/fr/parks';
    const stamp = tracker.capture(cacheKey);

    tracker.invalidateAll();

    expect(tracker.canStore(cacheKey, stamp)).toBe(false);
  });

  it('rejects stale persisted ranking entries without invalidating unrelated entries', () => {
    const tracker = new SsrPageCacheGenerationTracker();
    const rankingKey = 'https://amusement-parks.fun/fr/park/park-1/park-name';
    const unrelatedKey = 'https://amusement-parks.fun/fr/parks';
    const rankingGeneration = tracker.capture(rankingKey).ratingRankingPages ?? undefined;

    tracker.invalidateRatingRankingPages();

    expect(tracker.isStoredEntryCurrent(rankingKey, rankingGeneration)).toBe(false);
    expect(tracker.isStoredEntryCurrent(rankingKey, undefined)).toBe(false);
    expect(tracker.isStoredEntryCurrent(unrelatedKey, undefined)).toBe(true);
  });

  it('rejects persisted ranking entries created by a previous SSR process', () => {
    const rankingKey = 'https://amusement-parks.fun/fr/rankings';
    const previousProcess = new SsrPageCacheGenerationTracker();
    const persistedGeneration = previousProcess.capture(rankingKey).ratingRankingPages ?? undefined;
    const restartedProcess = new SsrPageCacheGenerationTracker();

    expect(restartedProcess.isStoredEntryCurrent(rankingKey, persistedGeneration)).toBe(false);
  });
});
