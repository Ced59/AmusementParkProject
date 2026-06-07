import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';

export interface AdminParkItemWorkbenchStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'createParkItem' | 'getParkItemsPaginated' | 'updateParkItemsBulkAdministration'> {
}

export const ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<AdminParkItemWorkbenchStateParkItemsApiServicePort>('ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});
