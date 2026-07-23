import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

import { SEO_DEFAULT_LANGUAGE } from './seo-languages';

@Injectable({
  providedIn: 'root'
})
export class SeoRoutePolicyService {
  constructor(@Inject(DOCUMENT) private readonly document: Document) {
  }

  resolveLanguage(url: string): string {
    const firstSegment: string | undefined = this.getPathSegments(url)[0];
    return firstSegment?.trim() || SEO_DEFAULT_LANGUAGE;
  }

  resolveStaticRouteKey(url: string): string | null {
    const routeSegment: string = this.getPathSegments(url)[1] ?? 'home';
    const routeKeys: Readonly<Record<string, string>> = {
      home: 'home',
      parks: 'parks',
      sitemap: 'sitemap',
      rankings: 'rankings',
      technical: 'technical',
      manufacturers: 'manufacturers',
      about: 'about',
      contact: 'contact',
      versions: 'versions',
      privacy: 'privacy',
      'not-found': 'notFound'
    };

    return routeSegment === '' ? 'home' : routeKeys[routeSegment] ?? null;
  }

  isAdminRoute(url: string): boolean {
    return /^\/[a-z]{2}\/admin(?:\/|$)/i.test(this.normalizePath(url));
  }

  isAccountRoute(url: string): boolean {
    return /^\/[a-z]{2}\/(?:profile|confirm-account|forgot-password|reset-password)(?:\/|$)/i.test(this.normalizePath(url));
  }

  isFilteredPublicParkRoute(url: string): boolean {
    if (!this.hasQueryString(url)) {
      return false;
    }

    const path: string = this.normalizePath(url);
    const filterablePatterns: ReadonlyArray<RegExp> = [
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/items\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zones\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/weather\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/opening-hours\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/images\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/images\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/?$/i,
      /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/?$/i
    ];

    return filterablePatterns.some((pattern: RegExp): boolean => pattern.test(path));
  }

  hasQueryString(url: string): boolean {
    const trimmedUrl: string = url?.trim() ?? '';

    try {
      return new URL(trimmedUrl || '/', this.baseUrl()).search.length > 0;
    } catch {
      return trimmedUrl.includes('?');
    }
  }

  getPathSegments(url: string): string[] {
    return this.normalizePath(url)
      .split('/')
      .filter((segment: string): boolean => !!segment);
  }

  private normalizePath(url: string): string {
    const rawUrl: string = url?.trim() ?? '';

    if (!rawUrl) {
      return '/';
    }

    try {
      return new URL(rawUrl, this.baseUrl()).pathname.replace(/\/+/g, '/') || '/';
    } catch {
      const withoutHash: string = rawUrl.split('#')[0] ?? '';
      const withoutQuery: string = withoutHash.split('?')[0] ?? '';
      const withLeadingSlash: string = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;
      return withLeadingSlash.replace(/\/+/g, '/');
    }
  }

  private baseUrl(): string {
    const documentOrigin: string | undefined = this.document.location?.origin;
    return documentOrigin && documentOrigin !== 'null' ? documentOrigin : 'https://amusement-parks.fun';
  }
}
