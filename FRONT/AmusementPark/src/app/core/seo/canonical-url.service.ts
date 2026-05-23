import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CanonicalUrlService {
  private readonly baseUrl: string = this.normalizeBaseUrl(environment.baseUrl ?? 'https://amusement-parks.fun/');

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
      return 'https://amusement-parks.fun/';
    }

    return trimmedValue.endsWith('/') ? trimmedValue : `${trimmedValue}/`;
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

    if (normalizedSlashes.length > 1 && normalizedSlashes.endsWith('/')) {
      return normalizedSlashes.slice(0, -1);
    }

    return normalizedSlashes;
  }
}
