import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  VisitRecapShareCandidates
} from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface VisitRecapSharePort {
  getSettings(visitId: string): Observable<SharePublicationSettings>;
  getCandidates(visitId: string, includeMissedItems: boolean): Observable<VisitRecapShareCandidates>;
  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview>;
  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings>;
  revoke(visitId: string): Observable<SharePublicationSettings>;
}

export const VISIT_RECAP_SHARE_PORT = new InjectionToken<VisitRecapSharePort>('VISIT_RECAP_SHARE_PORT', {
  providedIn: 'root',
  factory: (): VisitRecapSharePort => {
    const apiService: SharePublicationsApiService = inject(SharePublicationsApiService);
    return {
      getSettings: (visitId: string): Observable<SharePublicationSettings> => apiService.getVisitSettings(visitId),
      getCandidates: (visitId: string, includeMissedItems: boolean): Observable<VisitRecapShareCandidates> =>
        apiService.getVisitCandidates(visitId, includeMissedItems),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => apiService.preview(request),
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => apiService.publish(request),
      revoke: (visitId: string): Observable<SharePublicationSettings> => apiService.revokeVisit(visitId)
    };
  }
});
