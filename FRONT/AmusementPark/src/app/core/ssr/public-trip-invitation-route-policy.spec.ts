import {
  isPublicTripInvitationRoute,
  sanitizePublicTripInvitationUrl,
} from './public-trip-invitation-route-policy';

describe('public trip invitation route policy', () => {
  it('recognizes only a localized opaque invitation link', () => {
    expect(isPublicTripInvitationRoute('/fr/trip-invitations/opaque-token')).toBe(true);
    expect(isPublicTripInvitationRoute('/en/trip-invitations/opaque-token/')).toBe(true);
    expect(isPublicTripInvitationRoute('/fr/trip-invitations')).toBe(false);
    expect(isPublicTripInvitationRoute('/fr/trip-invitations/token/extra')).toBe(false);
  });

  it('replaces the bearer token and query with a stable diagnostic path', () => {
    expect(
      sanitizePublicTripInvitationUrl('/fr/trip-invitations/opaque-secret?from=email'),
    ).toBe('/fr/trip-invitations/[REDACTED]');
    expect(
      sanitizePublicTripInvitationUrl('/en/trip-invitations/another-secret/?from=email'),
    ).toBe('/en/trip-invitations/[REDACTED]');
    expect(sanitizePublicTripInvitationUrl('/fr/parks?page=2')).toBe('/fr/parks?page=2');
  });
});
