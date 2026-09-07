import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { SharedVisitRecap } from '@app/models/sharing/share-publication.models';
import { SharePublicationsApiService } from '@data-access/sharing/share-publications-api.service';

export interface SharedVisitRecapPort {
  getSharedVisit(shareId: string): Observable<SharedVisitRecap>;
}

export const SHARED_VISIT_RECAP_PORT = new InjectionToken<SharedVisitRecapPort>('SHARED_VISIT_RECAP_PORT', {
  providedIn: 'root',
  factory: (): SharedVisitRecapPort => inject(SharePublicationsApiService)
});
