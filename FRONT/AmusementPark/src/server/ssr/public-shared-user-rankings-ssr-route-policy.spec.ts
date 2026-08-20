import { isPublicSharedUserRankingSsrRoute } from './public-shared-user-rankings-ssr-route-policy';

describe('public shared user rankings SSR route policy', () => {
  it('recognizes only localized shared ranking detail routes', () => {
    expect(isPublicSharedUserRankingSsrRoute('/fr/rankings/shared/opaque-token')).toBe(true);
    expect(isPublicSharedUserRankingSsrRoute('/en/rankings/shared/opaque-token/')).toBe(true);
    expect(isPublicSharedUserRankingSsrRoute('/fr/rankings/shared')).toBe(false);
    expect(isPublicSharedUserRankingSsrRoute('/fr/rankings')).toBe(false);
  });
});
