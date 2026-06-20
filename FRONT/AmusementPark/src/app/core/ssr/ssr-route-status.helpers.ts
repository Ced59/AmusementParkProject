import { LANGUAGES, LanguageOption } from '../../shared/models/localization';

const SUPPORTED_ROUTE_LANGUAGES: ReadonlySet<string> = new Set<string>(
  LANGUAGES.map((language: LanguageOption): string => language.value)
);

export function resolveSsrRouteStatusCode(url: string): number {
  return isSsrNotFoundRoute(url) ? 404 : 200;
}

export function shouldApplyNoindexFollowHeader(url: string): boolean {
  return isSsrNotFoundRoute(url) || isNoindexPublicPageRoute(url);
}

export function isSsrNotFoundRoute(url: string): boolean {
  const path: string = normalizeSsrPath(url);

  if (path === '/') {
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
    || /^\/[a-z]{2}\/(?:home|parks|rankings|about|contact|versions|privacy)\/?$/i.test(path)
    || /^\/[a-z]{2}\/park-(?:operator|founder|manufacturer)\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+(?:\/images|\/videos|\/map|\/zones|\/weather|\/items)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+(?:\/images|\/videos)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/[^/]+\/[^/]+\/?$/i.test(path);
}

function isKnownPrivateClientRoute(path: string): boolean {
  return /^\/[a-z]{2}\/admin(?:\/.*)?$/i.test(path)
    || /^\/[a-z]{2}\/(?:profile|confirm-account|forgot-password|reset-password)(?:\/.*)?$/i.test(path);
}

function isNoindexPublicPageRoute(url: string): boolean {
  const path: string = normalizeSsrPath(url);

  return isPublicParkMapRoute(path)
    || (isPublicParkItemsRoute(path) && hasQueryString(url))
    || (isPublicParkZonesRoute(path) && hasQueryString(url))
    || (isPublicParkZoneDetailRoute(path) && hasQueryString(url))
    || (isPublicParkImagesRoute(path) && hasQueryString(url))
    || (isPublicParkItemImagesRoute(path) && hasQueryString(url))
    || (isPublicParkVideosRoute(path) && hasQueryString(url))
    || (isPublicParkItemVideosRoute(path) && hasQueryString(url))
    || (isPublicParkWeatherRoute(path) && hasQueryString(url));
}

function isExplicitNotFoundPath(path: string): boolean {
  return /^\/[a-z]{2}\/not-found\/?$/i.test(path) && hasSupportedLanguagePrefix(path);
}

function hasSupportedLanguagePrefix(path: string): boolean {
  const language: string | null = getFirstPathSegment(path);

  return language !== null && SUPPORTED_ROUTE_LANGUAGES.has(language.toLowerCase());
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

function isPublicParkMapRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/map\/?$/i.test(path);
}

function isPublicParkItemsRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/items\/?$/i.test(path);
}

function isPublicParkZonesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zones\/?$/i.test(path);
}

function isPublicParkZoneDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i.test(path);
}

function isPublicParkImagesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/images\/?$/i.test(path);
}

function isPublicParkItemImagesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/images\/?$/i.test(path);
}

function isPublicParkVideosRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/?$/i.test(path);
}

function isPublicParkItemVideosRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/?$/i.test(path);
}

function isPublicParkWeatherRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/weather\/?$/i.test(path);
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
