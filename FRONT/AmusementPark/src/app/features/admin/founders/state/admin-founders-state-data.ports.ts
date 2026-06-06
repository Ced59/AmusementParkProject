import { inject, InjectionToken } from '@angular/core';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';

export interface AdminFoundersStateParkFoundersApiServicePort extends Pick<ParkFoundersApiService, 'getAllParkFounders'> {
}

export const ADMIN_FOUNDERS_STATE_PARK_FOUNDERS_API_SERVICE_PORT = new InjectionToken<AdminFoundersStateParkFoundersApiServicePort>('ADMIN_FOUNDERS_STATE_PARK_FOUNDERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkFoundersApiService)
});
