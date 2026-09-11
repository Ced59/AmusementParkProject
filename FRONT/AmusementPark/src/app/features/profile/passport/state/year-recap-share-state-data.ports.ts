import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  YearRecapShareSelection
} from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface YearRecapSharePort {
  getSettings(year: number): Observable<SharePublicationSettings>;
  getSelection(year: number): Observable<YearRecapShareSelection>;
  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview>;
  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings>;
  revoke(year: number): Observable<SharePublicationSettings>;
}

export const YEAR_RECAP_SHARE_PORT = new InjectionToken<YearRecapSharePort>('YEAR_RECAP_SHARE_PORT', {
  providedIn: 'root',
  factory: (): YearRecapSharePort => {
    const apiService: SharePublicationsApiService = inject(SharePublicationsApiService);
    return {
      getSettings: (year: number): Observable<SharePublicationSettings> => apiService.getYearSettings(year),
      getSelection: (year: number): Observable<YearRecapShareSelection> => apiService.getYearSelection(year),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => apiService.preview(request),
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => apiService.publish(request),
      revoke: (year: number): Observable<SharePublicationSettings> => apiService.revokeYear(year)
    };
  }
});
