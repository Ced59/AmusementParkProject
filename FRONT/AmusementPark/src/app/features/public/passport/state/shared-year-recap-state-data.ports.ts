import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface SharedYearRecapPort {
  getSharedYear(shareId: string): Observable<SharedYearRecap>;
}

export const SHARED_YEAR_RECAP_PORT = new InjectionToken<SharedYearRecapPort>('SHARED_YEAR_RECAP_PORT', {
  providedIn: 'root',
  factory: (): SharedYearRecapPort => {
    const apiService: SharePublicationsApiService = inject(SharePublicationsApiService);
    return {
      getSharedYear: (shareId: string): Observable<SharedYearRecap> => apiService.getSharedYear(shareId)
    };
  }
});
