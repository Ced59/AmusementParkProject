import { inject, InjectionToken } from '@angular/core';
import { ParkOpeningHoursSchedule } from '@app/models/parks/park-opening-hours';
import { ParkPricing } from '@app/models/parks/park-pricing';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkEditStateParksApiServicePort extends Pick<ParksApiService, 'createPark' | 'getParkById' | 'updatePark' | 'getAdminParkOpeningHours' | 'upsertAdminParkOpeningHours' | 'getAdminParkPricing' | 'upsertAdminParkPricing'> {
  getAdminParkOpeningHours(id: string): ReturnType<ParksApiService['getAdminParkOpeningHours']>;
  upsertAdminParkOpeningHours(id: string, schedule: ParkOpeningHoursSchedule): ReturnType<ParksApiService['upsertAdminParkOpeningHours']>;
  getAdminParkPricing(id: string): ReturnType<ParksApiService['getAdminParkPricing']>;
  upsertAdminParkPricing(id: string, pricing: ParkPricing): ReturnType<ParksApiService['upsertAdminParkPricing']>;
}

export const ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParkEditStateParksApiServicePort>('ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
