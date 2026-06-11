import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface ParkDetailParksPort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
}

export const PARK_DETAIL_PARKS_PORT = new InjectionToken<ParkDetailParksPort>('PARK_DETAIL_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
