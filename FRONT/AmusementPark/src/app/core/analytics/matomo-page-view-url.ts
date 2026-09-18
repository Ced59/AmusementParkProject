export interface MatomoPageViewData {
  readonly url: string;
  readonly title: string;
}

export function createMatomoPageViewData(
  pageUrl: string,
  documentTitle: string
): MatomoPageViewData {
  const sanitizedUrl: string = sanitizeMatomoPageViewUrl(pageUrl);
  const pathname: string = new URL(sanitizedUrl).pathname;

  if (pathname.endsWith('/product/passport')) {
    return { url: sanitizedUrl, title: 'Passport' };
  }

  if (pathname.includes('/product/share/')) {
    return { url: sanitizedUrl, title: 'Shared experience' };
  }

  return { url: sanitizedUrl, title: documentTitle || 'AmusementPark' };
}

export function sanitizeMatomoPageViewUrl(pageUrl: string): string {
  const url: URL = new URL(pageUrl);
  url.search = '';
  url.hash = '';

  const segments: string[] = url.pathname.split('/').filter((segment: string): boolean => segment.length > 0);
  const language: string | null = segments.length > 0 ? segments[0] : null;
  const localizedPath: string = segments.slice(1).join('/').toLowerCase();
  const isPrivatePassportPath: boolean = localizedPath === 'profile/passport'
    || localizedPath.startsWith('profile/passport/')
    || localizedPath.startsWith('profile/visits/')
    || localizedPath === 'passport/local'
    || localizedPath.startsWith('passport/local/');
  const publicShareProductPath: string | null = resolvePublicShareProductPath(localizedPath);

  if (language && isPrivatePassportPath) {
    url.pathname = `/${encodeURIComponent(language)}/product/passport`;
  } else if (language && publicShareProductPath) {
    url.pathname = `/${encodeURIComponent(language)}/${publicShareProductPath}`;
  }

  return url.toString();
}

function resolvePublicShareProductPath(localizedPath: string): string | null {
  if (localizedPath.startsWith('rankings/shared/')) {
    return 'product/share/personal-ranking';
  }
  if (localizedPath.startsWith('passport/shared/visits/')) {
    return 'product/share/visit-recap';
  }
  if (localizedPath.startsWith('passport/shared/years/')) {
    return 'product/share/year-recap';
  }
  if (localizedPath.startsWith('passport/shared/profiles/')) {
    return 'product/share/passport-profile';
  }
  if (localizedPath.startsWith('passport/shared/comparisons/')) {
    return 'product/share/profile-comparison';
  }
  if (localizedPath.startsWith('trip-invitations/')) {
    return 'product/share/trip-invitation';
  }

  return null;
}
