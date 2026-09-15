import { LANGUAGES, LanguageOption } from '../../shared/models/localization';
import { resolvePublicSitemapLocation } from '../../shared/utils/routing/public-sitemap-location';
import { resolvePublicDirectoryLocation } from '../../shared/utils/routing/public-directory-location';

const SUPPORTED_ROUTE_LANGUAGES: ReadonlySet<string> = new Set<string>(
  LANGUAGES.map((language: LanguageOption): string => language.value)
);

export function resolveSsrRouteStatusCode(url: string): number {
  return isSsrNotFoundRoute(url) ? 404 : 200;
}

export function shouldApplyNoindexFollowHeader(url: string): boolean {
  return resolveXRobotsTagHeader(url) !== null;
}

export function resolveXRobotsTagHeader(url: string, statusCode: number = 200, isCsrFallback: boolean = false): string | null {
  const path: string = normalizeSsrPath(url);

  if (isParkFitRoute(path)
      || isSharedUserRankingRoute(path)
      || isSharedVisitRecapRoute(path)
      || isSharedYearRecapRoute(path)
      || isSharedPassportProfileRoute(path)
      || isSharedProfileComparisonRoute(path)) {
    return 'noindex, nofollow, noarchive';
  }

  return isSsrNotFoundRoute(url) || isKnownPrivateClientRoute(path) || isNoindexPublicPageRoute(url, statusCode, isCsrFallback)
    ? 'noindex, follow'
    : null;
}

export function isSsrNotFoundRoute(url: string): boolean {
  const path: string = normalizeSsrPath(url);

  if (path === '/') {
    return false;
  }

  if (isUnsupportedLanguageRedirectPath(path)) {
    return false;
  }

  if (isExplicitNotFoundPath(path)) {
    return true;
  }

  return !isKnownLocalizedPageRoute(path);
}

function isKnownLocalizedPageRoute(path: string): boolean {
  if (!hasSupportedLanguagePrefix(path)) {
    return false;
  }

  return isKnownPublicPageRoute(path) || isKnownPrivateClientRoute(path);
}

function isKnownPublicPageRoute(path: string): boolean {
  return /^\/[a-z]{2}\/?$/i.test(path)
    || /^\/[a-z]{2}\/(?:home|parks|sitemap|rankings|manufacturers|about|contact|versions|privacy)\/?$/i.test(path)
    || /^\/[a-z]{2}\/rankings\/methodology(?:\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/technical(?:\/[^/]+)?\/?$/i.test(path)
    || isSharedUserRankingRoute(path)
    || isSharedVisitRecapRoute(path)
    || isSharedYearRecapRoute(path)
    || isSharedPassportProfileRoute(path)
    || isSharedProfileComparisonRoute(path)
    || /^\/[a-z]{2}\/park-(?:operator|founder|manufacturer)\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/attraction\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/attraction\/[^/]+\/[^/]+\/history(?:\/page\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+(?:\/images|\/videos|\/map|\/zones|\/weather|\/opening-hours|\/pricing|\/items|\/comments)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/history(?:\/[^/]+\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/video\/(?:s\/)?[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+(?:\/images|\/videos|\/comments)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/history(?:\/[^/]+\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/video\/(?:s\/)?[^/]+\/[^/]+\/?$/i.test(path);
}

function isSharedUserRankingRoute(path: string): boolean {
  return /^\/[a-z]{2}\/rankings\/shared\/[^/]+\/?$/i.test(path);
}

function isSharedVisitRecapRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/visits\/[^/]+\/?$/i.test(path);
}

function isSharedYearRecapRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/years\/[^/]+\/?$/i.test(path);
}

function isSharedPassportProfileRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/profiles\/[^/]+\/?$/i.test(path);
}

function isSharedProfileComparisonRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/comparisons\/[^/]+\/?$/i.test(path);
}

function isParkFitRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park-fit(?:\/(?:results|compare))?\/?$/i.test(path);
}

function isKnownPrivateClientRoute(path: string): boolean {
  return isParkFitRoute(path)
    || /^\/[a-z]{2}\/admin(?:\/.*)?$/i.test(path)
    || /^\/[a-z]{2}\/passport\/local(?:\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/(?:profile|confirm-account|forgot-password|reset-password)(?:\/.*)?$/i.test(path);
}

function isNoindexPublicPageRoute(url: string, statusCode: number, isCsrFallback: boolean): boolean {
  const path: string = normalizeSsrPath(url);

  if (hasSupportedLanguagePrefix(path) && /^\/[a-z]{2}\/(?:parks|manufacturers)$/i.test(path)) {
    try {
      const params: URLSearchParams = new URL(url, 'https://amusement-parks.fun').searchParams;
      const location = resolvePublicDirectoryLocation({
        keys: Array.from(params.keys()), get: key => params.get(key), getAll: key => params.getAll(key)
      });
      if (location.isIndexable) {
        // Preserve the metadata validated by the loaded directory, never infer it from a page number.
        return statusCode !== 200 || isCsrFallback;
      }
    } catch {
      return true;
    }
  }

  if (hasSupportedLanguagePrefix(path) && /^\/[a-z]{2}\/sitemap$/i.test(path)) {
    try {
      const params: URLSearchParams = new URL(url, 'https://amusement-parks.fun').searchParams;
      const location = resolvePublicSitemapLocation({
        keys: Array.from(params.keys()),
        get: (key: string): string | null => params.get(key),
        getAll: (key: string): string[] => params.getAll(key)
      });
      if (location.isValid) {
        // Syntax alone never grants indexability: keep the loaded Angular meta directives.
        return statusCode !== 200 || isCsrFallback;
      }
    } catch {
      return true;
    }
  }

  return hasQueryString(url) && (path === '/' || isKnownPublicPageRoute(path));
}

function isExplicitNotFoundPath(path: string): boolean {
  return /^\/[a-z]{2}\/not-found\/?$/i.test(path) && hasSupportedLanguagePrefix(path);
}

function hasSupportedLanguagePrefix(path: string): boolean {
  const language: string | null = getFirstPathSegment(path);

  return language !== null && SUPPORTED_ROUTE_LANGUAGES.has(language.toLowerCase());
}

function isUnsupportedLanguageRedirectPath(path: string): boolean {
  const language: string | null = getFirstPathSegment(path);

  return language !== null
    && /^[a-z]{2}$/i.test(language)
    && !SUPPORTED_ROUTE_LANGUAGES.has(language.toLowerCase());
}

function getFirstPathSegment(path: string): string | null {
  const firstSegment: string | undefined = path
    .split('/')
    .filter((segment: string): boolean => segment.length > 0)[0];

  return firstSegment ?? null;
}

function hasQueryString(url: string): boolean {
  return url.includes('?');
}

function normalizeSsrPath(url: string): string {
  const rawUrl: string = url.trim();

  if (!rawUrl) {
    return '/';
  }

  try {
    const parsedUrl: URL = new URL(rawUrl, 'https://amusement-parks.fun');
    return normalizePathSlashes(parsedUrl.pathname);
  } catch {
    const withoutHash: string = rawUrl.split('#')[0] ?? '';
    const withoutQuery: string = withoutHash.split('?')[0] ?? '';
    const withLeadingSlash: string = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;

    return normalizePathSlashes(withLeadingSlash);
  }
}

function normalizePathSlashes(path: string): string {
  const normalizedPath: string = path.replace(/\/+/g, '/');

  if (!normalizedPath) {
    return '/';
  }

  if (normalizedPath.length > 1 && normalizedPath.endsWith('/')) {
    return normalizedPath.slice(0, -1);
  }

  return normalizedPath;
}
