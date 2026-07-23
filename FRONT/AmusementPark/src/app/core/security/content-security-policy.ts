import { SUPPORTED_VIDEO_EMBED_ORIGINS } from './video-embed-policy';

export interface ContentSecurityPolicyOptions {
  readonly allowLocalSources: boolean;
  readonly reportUri: string;
}

export function buildContentSecurityPolicy(options: ContentSecurityPolicyOptions): string {
  const localScriptSources: string[] = options.allowLocalSources
    ? ['http://localhost:*', 'http://matomo.amusement.localhost:*']
    : [];
  const localImageSources: string[] = options.allowLocalSources
    ? ['http://localhost:*', 'http://amusement.localhost:*', 'http://matomo.amusement.localhost:*']
    : [];
  const localConnectSources: string[] = options.allowLocalSources
    ? ['http://localhost:*', 'https://localhost:*', 'http://amusement.localhost:*', 'http://matomo.amusement.localhost:*']
    : [];

  return [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    joinCspDirective('script-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com', 'https://apis.google.com', 'https://matomo.cedric-caudron.com', 'https://www.clarity.ms', 'https://*.clarity.ms', ...localScriptSources]),
    joinCspDirective('style-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com']),
    joinCspDirective('style-src-elem', ["'self'", "'unsafe-inline'", 'https://accounts.google.com']),
    joinCspDirective('font-src', ["'self'", 'data:']),
    joinCspDirective('img-src', ["'self'", 'data:', 'blob:', 'https:', 'https://tile.openstreetmap.org', 'https://*.tile.openstreetmap.org', 'https://*.clarity.ms', ...localImageSources]),
    joinCspDirective('connect-src', ["'self'", 'https://accounts.google.com', 'https://www.googleapis.com', 'https://matomo.cedric-caudron.com', 'https://www.clarity.ms', 'https://*.clarity.ms', ...localConnectSources]),
    joinCspDirective('frame-src', ["'self'", 'https://accounts.google.com', ...SUPPORTED_VIDEO_EMBED_ORIGINS]),
    "worker-src 'self' blob:",
    "media-src 'self' blob: data:",
    "manifest-src 'self'",
    `report-uri ${options.reportUri}`
  ].join('; ');
}

function joinCspDirective(name: string, sources: readonly string[]): string {
  const uniqueSources: string[] = Array.from(new Set(sources.filter((source: string): boolean => source.length > 0)));
  return `${name} ${uniqueSources.join(' ')}`;
}
