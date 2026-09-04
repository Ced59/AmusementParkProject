import { inject, InjectionToken } from '@angular/core';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';

export interface PassportAnonymousDraftAttractionsPort extends Pick<
  ParkItemsApiService,
  'getParkItemsByParkIdPage'
> {
}

export const PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT =
  new InjectionToken<PassportAnonymousDraftAttractionsPort>(
    'PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT',
    { providedIn: 'root', factory: () => inject(ParkItemsApiService) }
  );
