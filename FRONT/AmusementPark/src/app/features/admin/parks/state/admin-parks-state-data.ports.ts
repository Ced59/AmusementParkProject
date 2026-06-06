import { inject, InjectionToken } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParksStateParksApiServicePort extends Pick<ParksApiService, 'getParksPaginated' | 'searchParks' | 'updateParksBulkAdministration'> {
}

export const ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParksStateParksApiServicePort>('ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
