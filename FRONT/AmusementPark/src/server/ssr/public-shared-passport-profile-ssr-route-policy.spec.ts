import { isPublicSharedPassportProfileSsrRoute } from './public-shared-passport-profile-ssr-route-policy';

describe('public shared passport profile SSR route policy', () => {
  it('recognizes only a localized opaque passport profile link', () => {
    expect(isPublicSharedPassportProfileSsrRoute('/fr/passport/shared/profiles/opaque-token')).toBe(true);
    expect(isPublicSharedPassportProfileSsrRoute('/en/passport/shared/profiles/opaque-token/')).toBe(true);
    expect(isPublicSharedPassportProfileSsrRoute('/fr/passport/shared/profiles')).toBe(false);
    expect(isPublicSharedPassportProfileSsrRoute('/fr/passport/shared/years/opaque-token')).toBe(false);
  });
});
