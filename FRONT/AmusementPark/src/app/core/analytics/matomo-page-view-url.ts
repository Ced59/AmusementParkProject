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

  if (language && isPrivatePassportPath) {
    url.pathname = `/${encodeURIComponent(language)}/product/passport`;
  }

  return url.toString();
}
