import { inject, InjectionToken } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkItemLocationStateParksApiServicePort extends Pick<ParksApiService, 'getParkById'> {
}

export const ADMIN_PARK_ITEM_LOCATION_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParkItemLocationStateParksApiServicePort>('ADMIN_PARK_ITEM_LOCATION_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
