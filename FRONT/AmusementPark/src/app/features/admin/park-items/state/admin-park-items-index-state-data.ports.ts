import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkItemsIndexStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'getParkItemsPaginated' | 'updateParkItemsBulkAdministration' | 'getParkItemById' | 'updateParkItem'> {
}

export const ADMIN_PARK_ITEMS_INDEX_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<AdminParkItemsIndexStateParkItemsApiServicePort>('ADMIN_PARK_ITEMS_INDEX_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface AdminParkItemsIndexStateParksApiServicePort extends Pick<ParksApiService, 'getParksPaginated'> {
}

export const ADMIN_PARK_ITEMS_INDEX_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParkItemsIndexStateParksApiServicePort>('ADMIN_PARK_ITEMS_INDEX_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
