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
  const path: string = extractPath(url);

  return ANONYMOUS_AUTH_ROUTE_PATTERNS.some((pattern: string) => {
    if (pattern === '/auth/external/') {
      return path.includes(pattern);
    }

    if (pattern.startsWith('/')) {
      return path.endsWith(pattern);
    }

    return url.includes(pattern);
  });
}

function extractPath(url: string): string {
  try {
    return new URL(url, 'http://localhost').pathname;
  } catch {
    return url;
  }
}
