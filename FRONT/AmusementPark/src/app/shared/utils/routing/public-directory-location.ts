import type { ParamMap } from '@angular/router';

export const PUBLIC_PARKS_PAGE_SIZE = 9;
export const PUBLIC_MANUFACTURERS_PAGE_SIZE = 24;
export type PublicDirectoryKind = 'parks' | 'manufacturers';

export interface PublicDirectoryLocation {
  readonly page: number;
  readonly isValid: boolean;
  readonly isIndexable: boolean;
}

export function resolvePublicDirectoryLocation(params: Pick<ParamMap, 'keys' | 'get' | 'getAll'>): PublicDirectoryLocation {
  const rawPage: string = params.get('page') ?? '1';
  const page: number = Number(rawPage);
  const isValid: boolean = params.getAll('page').length <= 1
    && /^[1-9][0-9]{0,5}$/.test(rawPage)
    && Number.isSafeInteger(page);

  return { page, isValid, isIndexable: isValid && params.keys.every(key => key === 'page') };
}

export function buildPublicDirectoryPagePath(language: string, directory: PublicDirectoryKind, page: number): string {
  return `/${language}/${directory}${page > 1 ? `?page=${page}` : ''}`;
}
