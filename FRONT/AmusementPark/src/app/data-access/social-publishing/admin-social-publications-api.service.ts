import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublishingOverview
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

  retry(publicationId: string): Observable<SocialPublication> {
    const encodedPublicationId: string = encodeURIComponent(publicationId);
    return this.http.post<SocialPublication>(`${this.baseUrl}/${encodedPublicationId}/retry`, {});
  }
}
