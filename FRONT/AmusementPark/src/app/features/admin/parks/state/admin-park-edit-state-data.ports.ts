import { inject, InjectionToken } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkEditStateParksApiServicePort extends Pick<ParksApiService, 'createPark' | 'getParkById' | 'updatePark'> {
}

export const ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParkEditStateParksApiServicePort>('ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
