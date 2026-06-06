import { inject, InjectionToken } from '@angular/core';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';

export interface AdminManufacturersStateManufacturersApiServicePort extends Pick<ManufacturersApiService, 'getAllAttractionManufacturers' | 'updateAttractionManufacturersBulkReviewStatus'> {
}

export const ADMIN_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT = new InjectionToken<AdminManufacturersStateManufacturersApiServicePort>('ADMIN_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});
