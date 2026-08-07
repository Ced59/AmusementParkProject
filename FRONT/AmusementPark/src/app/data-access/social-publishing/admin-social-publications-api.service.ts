import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublicationDraft,
  SocialPublicationSynchronizationResult,
  SocialPublishingOverview,
  UpdateSocialPublicationRequest
} from '@app/models/social-publishing/social-publishing.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminSocialPublicationsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/social-publications`;

  constructor(private readonly http: HttpClient) {
  }

  getOverview(limit: number = 25): Observable<SocialPublishingOverview> {
    const params: HttpParams = new HttpParams().set('limit', limit.toString());
    return this.http.get<SocialPublishingOverview>(this.baseUrl, { params });
  }

  publish(request: PublishSocialLinkRequest): Observable<SocialPublication> {
    return this.http.post<SocialPublication>(this.baseUrl, request);
  }

  getDraft(url: string, imagePage: number = 1, imagePageSize: number = 6): Observable<SocialPublicationDraft> {
    const params: HttpParams = new HttpParams()
      .set('url', url)
      .set('page', imagePage.toString())
      .set('size', imagePageSize.toString());
    return this.http.get<SocialPublicationDraft>(`${this.baseUrl}/draft`, { params });
  }

  retry(publicationId: string): Observable<SocialPublication> {
    const encodedPublicationId: string = encodeURIComponent(publicationId);
    return this.http.post<SocialPublication>(`${this.baseUrl}/${encodedPublicationId}/retry`, {});
  }

  update(publicationId: string, request: UpdateSocialPublicationRequest): Observable<SocialPublication> {
    const encodedPublicationId: string = encodeURIComponent(publicationId);
    return this.http.put<SocialPublication>(`${this.baseUrl}/${encodedPublicationId}`, request);
  }

  delete(publicationId: string): Observable<SocialPublication> {
    const encodedPublicationId: string = encodeURIComponent(publicationId);
    return this.http.delete<SocialPublication>(`${this.baseUrl}/${encodedPublicationId}`);
  }

  synchronize(limit: number = 25): Observable<SocialPublicationSynchronizationResult> {
    const params: HttpParams = new HttpParams().set('limit', limit.toString());
    return this.http.post<SocialPublicationSynchronizationResult>(`${this.baseUrl}/synchronize`, {}, { params });
  }
}
