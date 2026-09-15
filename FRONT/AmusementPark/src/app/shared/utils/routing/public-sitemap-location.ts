import type { ParamMap } from '@angular/router';

export interface PublicSitemapLocation {
  readonly nodeIds: readonly string[];
  readonly page: number;
  readonly isValid: boolean;
}

export const PUBLIC_SITEMAP_PAGE_SIZE = 100;

export function resolvePublicSitemapLocation(params: Pick<ParamMap, 'keys' | 'get' | 'getAll'>): PublicSitemapLocation {
  const rawNode: string = params.get('node') ?? '';
  const rawPage: string = params.get('page') ?? '1';
  const nodeIds: readonly string[] = rawNode.length > 0 ? rawNode.split('/') : [];
  const page: number = Number(rawPage);
  const isValid: boolean = params.keys.every((key: string): boolean => key === 'node' || key === 'page')
    && params.getAll('node').length <= 1
    && params.getAll('page').length <= 1
    && nodeIds.length <= 6
    && nodeIds.every((nodeId: string): boolean => /^[a-z][a-z-]*(?::[a-zA-Z0-9_-]{1,80})?$/.test(nodeId))
    && /^[1-9][0-9]{0,5}$/.test(rawPage)
    && Number.isSafeInteger(page);

  return { nodeIds, page, isValid };
}

export function buildPublicSitemapQuery(nodeIds: readonly string[], page: number = 1): Record<string, string | number> {
  return {
    ...(nodeIds.length > 0 ? { node: nodeIds.join('/') } : {}),
    ...(page > 1 ? { page } : {})
  };
}
