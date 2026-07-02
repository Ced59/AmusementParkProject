import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { PublicHtmlSitemapApiService } from '@data-access/seo/public-html-sitemap-api.service';
import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';

export interface PublicSitemapDataPort {
  getNodes(language: string, parentNodeId: string | null, includeDescendants?: boolean): Observable<PublicHtmlSitemapNode[]>;
}

export const PUBLIC_SITEMAP_DATA_PORT = new InjectionToken<PublicSitemapDataPort>('PUBLIC_SITEMAP_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(PublicHtmlSitemapApiService)
});
