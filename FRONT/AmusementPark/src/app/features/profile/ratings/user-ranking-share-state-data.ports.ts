import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { UserRankingShareSettings } from '@app/models/ratings/rating.models';
import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface UserRankingSharePort {
  getMyShareSettings(): Observable<UserRankingShareSettings>;
  setMyShareVisibility(isPublic: boolean): Observable<UserRankingShareSettings>;
  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview>;
  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings>;
}

export const USER_RANKING_SHARE_PORT = new InjectionToken<UserRankingSharePort>('USER_RANKING_SHARE_PORT', {
  providedIn: 'root',
  factory: (): UserRankingSharePort => {
    const ratingsApiService: RatingsApiService = inject(RatingsApiService);
    const sharePublicationsApiService: SharePublicationsApiService = inject(SharePublicationsApiService);
    return {
      getMyShareSettings: (): Observable<UserRankingShareSettings> => ratingsApiService.getMyShareSettings(),
      setMyShareVisibility: (isPublic: boolean): Observable<UserRankingShareSettings> => {
        return ratingsApiService.setMyShareVisibility(isPublic);
      },
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => {
        return sharePublicationsApiService.preview(request);
      },
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => {
        return sharePublicationsApiService.publish(request);
      }
    };
  }
});
