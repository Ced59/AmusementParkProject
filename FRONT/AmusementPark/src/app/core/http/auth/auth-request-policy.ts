const ANONYMOUS_AUTH_ROUTE_PATTERNS: readonly string[] = [
  '/login',
  '/users',
  '/users/confirm-email',
  '/users/resend-confirmation',
  '/users/forgot-password',
  '/users/reset-password',
  '/refresh-token',
  '/logout',
  '/auth/external/',
  'google-response'
];

export function shouldSkipAuthorizationHeader(url: string): boolean {
  return ANONYMOUS_AUTH_ROUTE_PATTERNS.some((pattern: string) => {
    if (pattern.startsWith('/')) {
      return url.endsWith(pattern);
    }

    return url.includes(pattern);
  });
}
