import { inject, InjectionToken } from '@angular/core';

import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { PassportRideOccurrencesApiService } from '@data-access/passport/passport-ride-occurrences-api.service';
import { PassportVisitsApiService } from '@data-access/passport/passport-visits-api.service';

export interface ParkItemPassportRideVisitsPort extends Pick<PassportVisitsApiService, 'listVisits'> {
}

export interface ParkItemPassportRideOccurrencesPort extends Pick<
  PassportRideOccurrencesApiService,
  'evaluateVisitTargets' | 'addBatch' | 'upsertAssessment'
> {
}

export interface ParkItemPassportRideOperationIdPort extends Pick<PassportOperationIdService, 'create'> {
}

export const PARK_ITEM_PASSPORT_RIDE_VISITS_PORT = new InjectionToken<ParkItemPassportRideVisitsPort>(
  'PARK_ITEM_PASSPORT_RIDE_VISITS_PORT',
  { providedIn: 'root', factory: () => inject(PassportVisitsApiService) }
);

export const PARK_ITEM_PASSPORT_RIDE_OCCURRENCES_PORT = new InjectionToken<ParkItemPassportRideOccurrencesPort>(
  'PARK_ITEM_PASSPORT_RIDE_OCCURRENCES_PORT',
  { providedIn: 'root', factory: () => inject(PassportRideOccurrencesApiService) }
);

export const PARK_ITEM_PASSPORT_RIDE_OPERATION_ID_PORT = new InjectionToken<ParkItemPassportRideOperationIdPort>(
  'PARK_ITEM_PASSPORT_RIDE_OPERATION_ID_PORT',
  { providedIn: 'root', factory: () => inject(PassportOperationIdService) }
);
