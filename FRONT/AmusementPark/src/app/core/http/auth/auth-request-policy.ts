const API_PATH_PREFIX = '/api';

const ANONYMOUS_POST_PATHS: readonly string[] = [
  '/auth/login',
  '/users',
  '/users/confirm-email',
  '/users/resend-confirmation',
  '/users/forgot-password',
  '/users/reset-password',
  '/auth/refresh-token',
  '/auth/logout'
];

const ANONYMOUS_POST_PREFIX_PATHS: readonly string[] = [
  '/auth/external'
];

const ANONYMOUS_GET_URL_FRAGMENTS: readonly string[] = [
  'google-response'
];

export function shouldSkipAuthorizationHeader(url: string, method: string = 'GET'): boolean {
  const path: string = extractPath(url);
  const normalizedMethod: string = method.toUpperCase();

  if (normalizedMethod === 'POST') {
    return ANONYMOUS_POST_PATHS.some((anonymousPath: string) => matchesApiPath(path, anonymousPath))
      || ANONYMOUS_POST_PREFIX_PATHS.some((anonymousPath: string) => matchesApiPathPrefix(path, anonymousPath));
  }

  if (normalizedMethod === 'GET') {
    return ANONYMOUS_GET_URL_FRAGMENTS.some((fragment: string) => url.includes(fragment));
  }

  return false;
}

function matchesApiPath(path: string, anonymousPath: string): boolean {
  return path === anonymousPath || path === `${API_PATH_PREFIX}${anonymousPath}`;
}

function matchesApiPathPrefix(path: string, anonymousPath: string): boolean {
  return matchesApiPath(path, anonymousPath)
    || path.startsWith(`${anonymousPath}/`)
    || path.startsWith(`${API_PATH_PREFIX}${anonymousPath}/`);
}

function extractPath(url: string): string {
  try {
    return new URL(url, 'http://localhost').pathname;
  } catch {
    return url;
  }
}
