import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';

export interface AdminParkItemsStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'getParkItemsPaginated' | 'updateParkItemsBulkAdministration' | 'updateParkItemsBulkFields' | 'previewParkItemsBulkCreate' | 'applyParkItemsBulkCreate'> {
}

export const ADMIN_PARK_ITEMS_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<AdminParkItemsStateParkItemsApiServicePort>('ADMIN_PARK_ITEMS_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface AdminParkItemsStateParkZonesApiServicePort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export const ADMIN_PARK_ITEMS_STATE_PARK_ZONES_API_SERVICE_PORT = new InjectionToken<AdminParkItemsStateParkZonesApiServicePort>('ADMIN_PARK_ITEMS_STATE_PARK_ZONES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});
