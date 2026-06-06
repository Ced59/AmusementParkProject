import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  GenerateSeoSitemapRequest,
  SeoSitemapGenerationResult,
  SeoSitemapHistoryResponse,
  SeoSitemapOverview,
  SeoSitemapSettings,
  UpdateSeoSitemapSettingsRequest
} from '@app/models/admin/seo/seo-sitemap.models';

@Injectable({
  providedIn: 'root'
})
export class AdminSeoSitemapsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/seo/sitemaps`;

  constructor(private readonly http: HttpClient) {
  }

  getOverview(): Observable<SeoSitemapOverview> {
    return this.http.get<SeoSitemapOverview>(`${this.baseUrl}/overview`);
  }

  getSettings(): Observable<SeoSitemapSettings> {
    return this.http.get<SeoSitemapSettings>(`${this.baseUrl}/settings`);
  }

  updateSettings(request: UpdateSeoSitemapSettingsRequest): Observable<SeoSitemapSettings> {
    return this.http.put<SeoSitemapSettings>(`${this.baseUrl}/settings`, request);
  }

  generate(request: GenerateSeoSitemapRequest): Observable<SeoSitemapGenerationResult> {
    return this.http.post<SeoSitemapGenerationResult>(`${this.baseUrl}/generate`, request);
  }

  getHistory(page: number, size: number): Observable<SeoSitemapHistoryResponse> {
    const params: HttpParams = new HttpParams()
      .set('page', page)
      .set('size', size);

    return this.http.get<SeoSitemapHistoryResponse>(`${this.baseUrl}/history`, { params });
  }
}
