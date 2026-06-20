import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CanonicalUrlService {
  private static readonly fallbackPublicBaseUrl: string = 'https://amusement-parks.fun/';

  private readonly baseUrl: string = this.normalizeBaseUrl(environment.baseUrl ?? CanonicalUrlService.fallbackPublicBaseUrl);

  buildAbsoluteUrl(pathOrUrl: string): string {
    const normalizedPath: string = this.normalizePath(pathOrUrl);

    if (normalizedPath === '/') {
      return this.baseUrl;
    }

    return `${this.baseUrl}${normalizedPath.slice(1)}`;
  }

  buildCanonicalFromCurrentUrl(url: string): string {
    return this.buildAbsoluteUrl(url);
  }

  replaceLanguage(url: string, language: string): string {
    const normalizedPath: string = this.normalizePath(url);
    const segments: string[] = normalizedPath.split('/').filter((segment: string) => !!segment);

    if (segments.length === 0) {
      return `/${language}/home`;
    }

    segments[0] = language;
    return `/${segments.join('/')}`;
  }

  private normalizeBaseUrl(value: string): string {
    const trimmedValue: string = value.trim();

    if (!trimmedValue) {
      return CanonicalUrlService.fallbackPublicBaseUrl;
    }

    try {
      const candidateUrl: string = /^https?:\/\//i.test(trimmedValue) ? trimmedValue : `https://${trimmedValue}`;
      const parsedUrl: URL = new URL(candidateUrl);

      if (environment.production && parsedUrl.protocol !== 'https:') {
        parsedUrl.protocol = 'https:';
      }

      if (environment.production && this.isLocalHost(parsedUrl.hostname)) {
        return CanonicalUrlService.fallbackPublicBaseUrl;
      }

      return `${parsedUrl.protocol}//${parsedUrl.host}/`;
    } catch {
      return CanonicalUrlService.fallbackPublicBaseUrl;
    }
  }

  private isLocalHost(hostname: string): boolean {
    const normalizedHostname: string = hostname.trim().toLowerCase();
    return normalizedHostname === 'localhost' || normalizedHostname === '127.0.0.1' || normalizedHostname === '::1';
  }

  private normalizePath(value: string): string {
    const trimmedValue: string = value.trim();

    if (!trimmedValue) {
      return '/';
    }

    if (/^https?:\/\//i.test(trimmedValue)) {
      const parsedUrl: URL = new URL(trimmedValue);
      return this.normalizePath(`${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`);
    }

    const withoutHash: string = trimmedValue.split('#')[0] ?? '';
    const withoutQuery: string = withoutHash.split('?')[0] ?? '';
    const withLeadingSlash: string = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;
    const normalizedSlashes: string = withLeadingSlash.replace(/\/+/g, '/');

    const normalizedPath: string = normalizedSlashes.length > 1 && normalizedSlashes.endsWith('/')
      ? normalizedSlashes.slice(0, -1)
      : normalizedSlashes;

    return this.normalizeLegacyPublicRoute(normalizedPath);
  }

  private normalizeLegacyPublicRoute(path: string): string {
    const segments: string[] = path.split('/').filter((segment: string): boolean => segment.length > 0);

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

    return path;
  }
}
