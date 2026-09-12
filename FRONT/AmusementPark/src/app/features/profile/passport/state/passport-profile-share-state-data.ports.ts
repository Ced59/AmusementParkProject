import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PassportProfileShareSelection,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface PassportProfileSharePort {
  getSettings(): Observable<SharePublicationSettings>;
  getSelection(): Observable<PassportProfileShareSelection>;
  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview>;
  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings>;
  revoke(): Observable<SharePublicationSettings>;
}

export const PASSPORT_PROFILE_SHARE_PORT = new InjectionToken<PassportProfileSharePort>(
  'PASSPORT_PROFILE_SHARE_PORT',
  {
    providedIn: 'root',
    factory: (): PassportProfileSharePort => {
      const apiService: SharePublicationsApiService = inject(SharePublicationsApiService);
      return {
        getSettings: (): Observable<SharePublicationSettings> => apiService.getPassportProfileSettings(),
        getSelection: (): Observable<PassportProfileShareSelection> => apiService.getPassportProfileSelection(),
        preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => apiService.preview(request),
        publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => apiService.publish(request),
        revoke: (): Observable<SharePublicationSettings> => apiService.revokePassportProfile()
      };
    }
  }
);
