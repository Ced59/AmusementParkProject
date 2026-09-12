import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface SharedPassportProfilePort {
  getSharedPassportProfile(shareId: string): Observable<SharedPassportProfile>;
}

export const SHARED_PASSPORT_PROFILE_PORT = new InjectionToken<SharedPassportProfilePort>(
  'SHARED_PASSPORT_PROFILE_PORT',
  {
    providedIn: 'root',
    factory: (): SharedPassportProfilePort => {
      const apiService: SharePublicationsApiService = inject(SharePublicationsApiService);
      return {
        getSharedPassportProfile: (shareId: string): Observable<SharedPassportProfile> =>
          apiService.getSharedPassportProfile(shareId)
      };
    }
  }
);
