import { inject, InjectionToken } from '@angular/core';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';

export interface AdminContextualBlockChildAddParkItemsDataPort extends Pick<ParkItemsApiService, 'createParkItem'> {
}

export interface AdminContextualBlockChildAddParkZonesDataPort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT =
  new InjectionToken<AdminContextualBlockChildAddParkItemsDataPort>('ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT', {
    providedIn: 'root',
    factory: () => inject(ParkItemsApiService)
  });

export const ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT =
  new InjectionToken<AdminContextualBlockChildAddParkZonesDataPort>('ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT', {
    providedIn: 'root',
    factory: () => inject(ParkZonesApiService)
  });
