export function buildCanonicalVideoRouteRedirectPath(url: string): string | null {
  const parsedUrl: URL | null = parseUrl(url);
  const path: string = normalizePathSlashes(parsedUrl?.pathname ?? url);
  const segments: string[] = path.split('/').filter((segment: string): boolean => segment.length > 0);

  const canonicalPath: string | null = buildCanonicalVideoPath(segments);

  if (canonicalPath === null) {
    return null;
  }

  return `${canonicalPath}${parsedUrl?.search ?? ''}`;
}

function buildCanonicalVideoPath(segments: string[]): string | null {
  if (segments.length === 7
    && segments[1] === 'park'
    && segments[4] === 'video'
    && segments[5] !== 's') {
    return `/${[
      segments[0],
      'park',
      segments[2],
      segments[3],
      'videos',
      segments[5],
      segments[6]
    ].join('/')}`;
  }

  if (segments.length === 8
    && segments[1] === 'park'
    && segments[4] === 'video'
    && segments[5] === 's') {
    return `/${[
      segments[0],
      'park',
      segments[2],
      segments[3],
      'videos',
      segments[6],
      segments[7]
    ].join('/')}`;
  }

  if (segments.length === 10
    && segments[1] === 'park'
    && segments[4] === 'item'
    && segments[7] === 'video'
    && segments[8] !== 's') {
    return `/${[
      segments[0],
      'park',
      segments[2],
      segments[3],
      'item',
      segments[5],
      segments[6],
      'videos',
      segments[8],
      segments[9]
    ].join('/')}`;
  }

  if (segments.length === 11
    && segments[1] === 'park'
    && segments[4] === 'item'
    && segments[7] === 'video'
    && segments[8] === 's') {
    return `/${[
      segments[0],
      'park',
      segments[2],
      segments[3],
      'item',
      segments[5],
      segments[6],
      'videos',
      segments[9],
      segments[10]
    ].join('/')}`;
  }

  return null;
}

function parseUrl(url: string): URL | null {
  try {
    return new URL(url, 'https://amusement-parks.fun');
  } catch {
    return null;
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
