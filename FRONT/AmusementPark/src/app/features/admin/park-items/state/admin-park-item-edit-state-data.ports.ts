import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkItemEditStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'createParkItem' | 'getParkItemById' | 'updateParkItem'> {
}

export const ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<AdminParkItemEditStateParkItemsApiServicePort>('ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface AdminParkItemEditStateParksApiServicePort extends Pick<ParksApiService, 'getParksPaginated'> {
}

export const ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminParkItemEditStateParksApiServicePort>('ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
