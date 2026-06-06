import { inject, InjectionToken } from '@angular/core';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';

export interface AdminParkZonesStateParkZonesApiServicePort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export const ADMIN_PARK_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT = new InjectionToken<AdminParkZonesStateParkZonesApiServicePort>('ADMIN_PARK_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});
