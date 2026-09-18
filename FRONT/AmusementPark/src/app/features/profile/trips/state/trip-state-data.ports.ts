import { inject, InjectionToken } from '@angular/core';

import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { TripPlansApiService } from '@data-access/trips/trip-plans-api.service';
import { TripProgramApiService } from '@data-access/trips/trip-program-api.service';
import { UserCollectionsApiService } from '@data-access/watchlists/user-collections-api.service';

export interface TripPlansDataPort extends Pick<
  TripPlansApiService,
  'listMine' | 'getMine' | 'create' | 'setDates' | 'delete'
> {
}

export interface TripProgramDataPort extends Pick<
  TripProgramApiService,
  'get' | 'addPark' | 'changeParkState' | 'movePark' | 'putDay' | 'deleteDay'
> {
}

export interface TripCollectionsDataPort extends Pick<UserCollectionsApiService, 'listMine'> {
}

export interface TripOperationIdPort extends Pick<PassportOperationIdService, 'create'> {
}

export const TRIP_PLANS_DATA_PORT = new InjectionToken<TripPlansDataPort>(
  'TRIP_PLANS_DATA_PORT',
  { providedIn: 'root', factory: (): TripPlansDataPort => inject(TripPlansApiService) }
);

export const TRIP_PROGRAM_DATA_PORT = new InjectionToken<TripProgramDataPort>(
  'TRIP_PROGRAM_DATA_PORT',
  { providedIn: 'root', factory: (): TripProgramDataPort => inject(TripProgramApiService) }
);

export const TRIP_COLLECTIONS_DATA_PORT = new InjectionToken<TripCollectionsDataPort>(
  'TRIP_COLLECTIONS_DATA_PORT',
  { providedIn: 'root', factory: (): TripCollectionsDataPort => inject(UserCollectionsApiService) }
);

export const TRIP_OPERATION_ID_PORT = new InjectionToken<TripOperationIdPort>(
  'TRIP_OPERATION_ID_PORT',
  { providedIn: 'root', factory: (): TripOperationIdPort => inject(PassportOperationIdService) }
);
