import type { ParamMap } from '@angular/router';

export const PUBLIC_PARKS_PAGE_SIZE = 9;

export interface PublicParksLocation {
  readonly page: number;
  readonly isValid: boolean;
  readonly isIndexable: boolean;
}

export function resolvePublicParksLocation(params: Pick<ParamMap, 'keys' | 'get' | 'getAll'>): PublicParksLocation {
  const rawPage: string = params.get('page') ?? '1';
  const page: number = Number(rawPage);
  const isValid: boolean = params.getAll('page').length <= 1
    && /^[1-9][0-9]{0,5}$/.test(rawPage)
    && Number.isSafeInteger(page);

  return { page, isValid, isIndexable: isValid && params.keys.every(key => key === 'page') };
}

export function buildPublicParksPagePath(language: string, page: number): string {
  return `/${language}/parks${page > 1 ? `?page=${page}` : ''}`;
}
