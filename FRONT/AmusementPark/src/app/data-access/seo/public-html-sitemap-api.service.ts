import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { unwrapCollection } from '@data-access/shared/api-helpers';
import { environment } from '../../../environments/environment';
import { PUBLIC_HTML_SITEMAP_API_ENDPOINTS } from './public-html-sitemap-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class PublicHtmlSitemapApiService {
  constructor(private readonly http: HttpClient) {
  }

  getNodes(language: string, parentNodeId: string | null = null, includeDescendants: boolean = false): Observable<PublicHtmlSitemapNode[]> {
    const url: string = `${environment.apiBaseUrl}${PUBLIC_HTML_SITEMAP_API_ENDPOINTS.nodes}`;
    let params: HttpParams = new HttpParams().set('language', language);

    if (parentNodeId) {
      params = params.set('parentNodeId', parentNodeId);
    }

    if (includeDescendants) {
      params = params.set('includeDescendants', 'true');
    }

    return this.http.get<PublicHtmlSitemapNode[]>(url, { params }).pipe(
      map((response: PublicHtmlSitemapNode[]) => unwrapCollection<PublicHtmlSitemapNode>(response))
    );
  }
}
