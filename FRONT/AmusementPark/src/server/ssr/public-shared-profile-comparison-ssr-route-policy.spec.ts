import { isPublicSharedProfileComparisonSsrRoute } from './public-shared-profile-comparison-ssr-route-policy';

describe('public shared profile comparison SSR route policy', () => {
  it('recognizes only a localized opaque profile comparison link', () => {
    expect(
      isPublicSharedProfileComparisonSsrRoute(
        '/fr/passport/shared/comparisons/opaque-token',
      ),
    ).toBe(true);
    expect(
      isPublicSharedProfileComparisonSsrRoute(
        '/en/passport/shared/comparisons/opaque-token/',
      ),
    ).toBe(true);
    expect(
      isPublicSharedProfileComparisonSsrRoute(
        '/fr/passport/shared/comparisons',
      ),
    ).toBe(false);
    expect(
      isPublicSharedProfileComparisonSsrRoute(
        '/fr/passport/shared/profiles/opaque-token',
      ),
    ).toBe(false);
  });
});
