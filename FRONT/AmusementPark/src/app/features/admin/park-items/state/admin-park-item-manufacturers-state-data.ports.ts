import { inject, InjectionToken } from '@angular/core';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';

export interface AdminParkItemManufacturersStateManufacturersApiServicePort extends Pick<ManufacturersApiService, 'getAttractionManufacturers'> {
}

export const ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT = new InjectionToken<AdminParkItemManufacturersStateManufacturersApiServicePort>('ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});
