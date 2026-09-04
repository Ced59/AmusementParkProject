import { inject, InjectionToken } from '@angular/core';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { PassportRideOccurrencesApiService } from '@data-access/passport/passport-ride-occurrences-api.service';
import { PassportVisitsApiService } from '@data-access/passport/passport-visits-api.service';

export interface PassportAnonymousImportVisitsPort extends Pick<
  PassportVisitsApiService,
  'createVisit' | 'getVisit' | 'listVisits' | 'updateVisit'
> {
}

export interface PassportAnonymousImportOccurrencesPort extends Pick<
  PassportRideOccurrencesApiService,
  'importBatch' | 'list'
> {
}

export interface PassportAnonymousImportParkItemsPort extends Pick<
  ParkItemsApiService,
  'getParkItemsByParkIdPage'
> {
}

export const PASSPORT_ANONYMOUS_IMPORT_VISITS_PORT =
  new InjectionToken<PassportAnonymousImportVisitsPort>(
    'PASSPORT_ANONYMOUS_IMPORT_VISITS_PORT',
    { providedIn: 'root', factory: () => inject(PassportVisitsApiService) }
  );

export const PASSPORT_ANONYMOUS_IMPORT_OCCURRENCES_PORT =
  new InjectionToken<PassportAnonymousImportOccurrencesPort>(
    'PASSPORT_ANONYMOUS_IMPORT_OCCURRENCES_PORT',
    { providedIn: 'root', factory: () => inject(PassportRideOccurrencesApiService) }
  );

export const PASSPORT_ANONYMOUS_IMPORT_PARK_ITEMS_PORT =
  new InjectionToken<PassportAnonymousImportParkItemsPort>(
    'PASSPORT_ANONYMOUS_IMPORT_PARK_ITEMS_PORT',
    { providedIn: 'root', factory: () => inject(ParkItemsApiService) }
  );
