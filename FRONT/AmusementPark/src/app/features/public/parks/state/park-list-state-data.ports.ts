import { inject, InjectionToken } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface ParkListStateParksApiServicePort extends Pick<ParksApiService, 'getParkById' | 'getParksPaginated' | 'getVisibleParkMapPoints' | 'searchParks'> {
}

export const PARK_LIST_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<ParkListStateParksApiServicePort>('PARK_LIST_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
