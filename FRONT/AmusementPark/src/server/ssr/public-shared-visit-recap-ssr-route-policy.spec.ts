import { isPublicSharedVisitRecapSsrRoute } from './public-shared-visit-recap-ssr-route-policy';

describe('public shared visit recap SSR route policy', () => {
  it('recognizes only a localized opaque visit recap link', () => {
    expect(isPublicSharedVisitRecapSsrRoute('/fr/passport/shared/visits/opaque-token')).toBe(true);
    expect(isPublicSharedVisitRecapSsrRoute('/en/passport/shared/visits/opaque-token/')).toBe(true);
    expect(isPublicSharedVisitRecapSsrRoute('/fr/passport/shared/visits')).toBe(false);
    expect(isPublicSharedVisitRecapSsrRoute('/fr/passport/local/draft-1')).toBe(false);
  });
});
