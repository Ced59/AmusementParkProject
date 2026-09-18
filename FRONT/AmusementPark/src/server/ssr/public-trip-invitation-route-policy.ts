export function isPublicTripInvitationRoute(path: string): boolean {
  return /^\/[a-z]{2}\/trip-invitations\/[^/]+\/?$/i.test(path);
}
