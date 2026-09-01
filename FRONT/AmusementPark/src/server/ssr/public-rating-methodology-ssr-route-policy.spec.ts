import { isPublicRatingMethodologySsrRoute } from './public-rating-methodology-ssr-route-policy';

describe('public rating methodology SSR route policy', () => {
  it('accepts the current and historical localized methodology routes', () => {
    expect(isPublicRatingMethodologySsrRoute('/fr/rankings/methodology')).toBe(true);
    expect(isPublicRatingMethodologySsrRoute('/en/rankings/methodology/ratings-2026-01/')).toBe(true);
  });

  it('rejects rankings and malformed methodology routes', () => {
    expect(isPublicRatingMethodologySsrRoute('/fr/rankings')).toBe(false);
    expect(isPublicRatingMethodologySsrRoute('/fr/rankings/methodology/ratings-2026-01/extra')).toBe(false);
  });
});
