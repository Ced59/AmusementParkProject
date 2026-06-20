import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkMapItems } from '@app/models/parks/park-map-items';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface ParkMapParksPort {
  getParkMapItems(id: string, options?: AnonymousHttpOptions): Observable<ParkMapItems>;
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
}

export const PARK_MAP_PARKS_PORT = new InjectionToken<ParkMapParksPort>('PARK_MAP_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
