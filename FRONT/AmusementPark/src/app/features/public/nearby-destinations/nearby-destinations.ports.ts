import { InjectionToken, inject } from '@angular/core';
import { NearbyDestinationsApiService } from '@data-access/parks/nearby-destinations-api.service';

export type NearbyDestinationsPort = Pick<NearbyDestinationsApiService, 'getNearby'>;

export const NEARBY_DESTINATIONS_PORT = new InjectionToken<NearbyDestinationsPort>('NEARBY_DESTINATIONS_PORT', {
  providedIn: 'root',
  factory: () => inject(NearbyDestinationsApiService)
});
