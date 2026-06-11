import { ActivatedRoute, ParamMap } from '@angular/router';

import { LANGUAGES } from '@shared/models/localization';

const SUPPORTED_ROUTE_LANGUAGES: ReadonlySet<string> = new Set<string>(LANGUAGES.map((language) => language.value));

export function resolveLanguageFromActivatedRoute(route: ActivatedRoute, fallback: string = 'en'): string {
  let currentRoute: ActivatedRoute | null = route;

  while (currentRoute !== null) {
    const language: string | null = currentRoute.snapshot?.paramMap?.get('lang') ?? null;
    if (isSupportedRouteLanguage(language)) {
      return language;
    }

    currentRoute = currentRoute.parent ?? null;
  }

  return isSupportedRouteLanguage(fallback) ? fallback : 'en';
}


export function findNearestLanguageActivatedRoute(route: ActivatedRoute): ActivatedRoute | null {
  let currentRoute: ActivatedRoute | null = route;

  while (currentRoute !== null) {
    const language: string | null = currentRoute.snapshot?.paramMap?.get('lang') ?? null;
    if (isSupportedRouteLanguage(language)) {
      return currentRoute;
    }

    currentRoute = currentRoute.parent ?? null;
  }

  return null;
}

export function resolveLanguageFromParamMap(paramMap: ParamMap, fallback: string = 'en'): string {
  const language: string | null = paramMap.get('lang');

  if (isSupportedRouteLanguage(language)) {
    return language;
  }

  return isSupportedRouteLanguage(fallback) ? fallback : 'en';
}

export function resolveLanguageFromUrl(url: string | null | undefined, fallback: string = 'en'): string {
  const normalizedUrl: string = (url ?? '').trim();
  const firstPathSegment: string | undefined = normalizedUrl
    .split(/[?#]/, 1)[0]
    .split('/')
    .filter((segment: string): boolean => segment.length > 0)[0];

  if (isSupportedRouteLanguage(firstPathSegment)) {
    return firstPathSegment;
  }

  return isSupportedRouteLanguage(fallback) ? fallback : 'en';
}

function isSupportedRouteLanguage(language: string | null | undefined): language is string {
  return typeof language === 'string' && SUPPORTED_ROUTE_LANGUAGES.has(language);
}
