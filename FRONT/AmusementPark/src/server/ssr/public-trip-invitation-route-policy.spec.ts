import { isPublicTripInvitationRoute } from './public-trip-invitation-route-policy';

describe('public trip invitation route policy', () => {
  it('recognizes only a localized opaque invitation link', () => {
    expect(isPublicTripInvitationRoute('/fr/trip-invitations/opaque-token')).toBe(true);
    expect(isPublicTripInvitationRoute('/en/trip-invitations/opaque-token/')).toBe(true);
    expect(isPublicTripInvitationRoute('/fr/trip-invitations')).toBe(false);
    expect(isPublicTripInvitationRoute('/fr/trip-invitations/token/extra')).toBe(false);
  });
});
