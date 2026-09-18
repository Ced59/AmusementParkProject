export function isPublicTripInvitationRoute(path: string): boolean {
  return /^\/[a-z]{2}\/trip-invitations\/[^/]+\/?$/i.test(path);
}

export function sanitizePublicTripInvitationUrl(url: string): string {
  return url.replace(
    /^(\/[a-z]{2}\/trip-invitations\/)[^/?]+\/?(?:\?.*)?$/i,
    '$1[REDACTED]',
  );
}

export function resolveTripInvitationCsrCacheControl(path: string, fallback: string): string {
  return isPublicTripInvitationRoute(path)
    ? 'private, no-store, max-age=0'
    : fallback;
}
