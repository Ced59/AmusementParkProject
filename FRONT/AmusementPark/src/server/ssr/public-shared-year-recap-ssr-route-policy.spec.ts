import { isPublicSharedYearRecapSsrRoute } from './public-shared-year-recap-ssr-route-policy';

describe('public shared year recap SSR route policy', () => {
  it('recognizes only a localized opaque year recap link', () => {
    expect(isPublicSharedYearRecapSsrRoute('/fr/passport/shared/years/opaque-token')).toBe(true);
    expect(isPublicSharedYearRecapSsrRoute('/en/passport/shared/years/opaque-token/')).toBe(true);
    expect(isPublicSharedYearRecapSsrRoute('/fr/passport/shared/years')).toBe(false);
    expect(isPublicSharedYearRecapSsrRoute('/fr/passport/shared/visits/opaque-token')).toBe(false);
  });
});
