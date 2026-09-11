import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  SharedVisitRecap,
  SharedYearRecap,
  VisitRecapShareCandidates,
  YearRecapShareSelection
} from '@app/models/sharing/share-publication.models';
import { environment } from '../../../environments/environment';
import { SHARE_PUBLICATIONS_API_ENDPOINTS } from './share-publications-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class SharePublicationsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> {
    const url: string = `${environment.apiBaseUrl}${SHARE_PUBLICATIONS_API_ENDPOINTS.preview}`;
    return this.http.post<SharePublicationPreview>(url, request, this.jsonHttpOptions);
  }

  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings> {
    const url: string = `${environment.apiBaseUrl}${SHARE_PUBLICATIONS_API_ENDPOINTS.publish}`;
    return this.http.post<SharePublicationSettings>(url, request, this.jsonHttpOptions);
  }

  getVisitSettings(visitId: string): Observable<SharePublicationSettings> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.visitSettings(visitId);
    return this.http.get<SharePublicationSettings>(`${environment.apiBaseUrl}${endpoint}`);
  }

  getVisitCandidates(
    visitId: string,
    includeMissedItems: boolean
  ): Observable<VisitRecapShareCandidates> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.visitCandidates(
      visitId,
      includeMissedItems
    );
    return this.http.get<VisitRecapShareCandidates>(`${environment.apiBaseUrl}${endpoint}`);
  }

  revokeVisit(visitId: string): Observable<SharePublicationSettings> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.visitSettings(visitId);
    return this.http.delete<SharePublicationSettings>(`${environment.apiBaseUrl}${endpoint}`);
  }

  getSharedVisit(shareId: string): Observable<SharedVisitRecap> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.sharedVisit(shareId);
    return this.http.get<SharedVisitRecap>(`${environment.apiBaseUrl}${endpoint}`);
  }

  getYearSettings(year: number): Observable<SharePublicationSettings> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.yearSettings(year);
    return this.http.get<SharePublicationSettings>(`${environment.apiBaseUrl}${endpoint}`);
  }

  getYearSelection(year: number): Observable<YearRecapShareSelection> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.yearSelection(year);
    return this.http.get<YearRecapShareSelection>(`${environment.apiBaseUrl}${endpoint}`);
  }

  revokeYear(year: number): Observable<SharePublicationSettings> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.yearSettings(year);
    return this.http.delete<SharePublicationSettings>(`${environment.apiBaseUrl}${endpoint}`);
  }

  getSharedYear(shareId: string): Observable<SharedYearRecap> {
    const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.sharedYear(shareId);
    return this.http.get<SharedYearRecap>(`${environment.apiBaseUrl}${endpoint}`);
  }
}
