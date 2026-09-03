import { inject, InjectionToken } from '@angular/core';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { PassportRideOccurrencesApiService } from '@data-access/passport/passport-ride-occurrences-api.service';
import { PassportVisitsApiService } from '@data-access/passport/passport-visits-api.service';

export interface PassportVisitEditorVisitsPort extends Pick<
  PassportVisitsApiService,
  'getVisit' | 'upsertParkAssessment' | 'deleteParkAssessment'
> {
}

export interface PassportVisitEditorOccurrencesPort extends Pick<
  PassportRideOccurrencesApiService,
  'list' | 'addBatch' | 'update' | 'delete' | 'reorder'
> {
}

export interface PassportVisitEditorParksPort extends Pick<ParksApiService, 'getParkById'> {
}

export interface PassportVisitEditorZonesPort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export interface PassportVisitEditorAttractionsPort extends Pick<ParkItemsApiService, 'getParkItemsByParkIdPage'> {
}

export interface PassportVisitEditorOperationIdPort extends Pick<PassportOperationIdService, 'create'> {
}

export const PASSPORT_VISIT_EDITOR_VISITS_PORT = new InjectionToken<PassportVisitEditorVisitsPort>(
  'PASSPORT_VISIT_EDITOR_VISITS_PORT',
  { providedIn: 'root', factory: () => inject(PassportVisitsApiService) }
);

export const PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT = new InjectionToken<PassportVisitEditorOccurrencesPort>(
  'PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT',
  { providedIn: 'root', factory: () => inject(PassportRideOccurrencesApiService) }
);

export const PASSPORT_VISIT_EDITOR_PARKS_PORT = new InjectionToken<PassportVisitEditorParksPort>(
  'PASSPORT_VISIT_EDITOR_PARKS_PORT',
  { providedIn: 'root', factory: () => inject(ParksApiService) }
);

export const PASSPORT_VISIT_EDITOR_ZONES_PORT = new InjectionToken<PassportVisitEditorZonesPort>(
  'PASSPORT_VISIT_EDITOR_ZONES_PORT',
  { providedIn: 'root', factory: () => inject(ParkZonesApiService) }
);

export const PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT = new InjectionToken<PassportVisitEditorAttractionsPort>(
  'PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT',
  { providedIn: 'root', factory: () => inject(ParkItemsApiService) }
);

export const PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT = new InjectionToken<PassportVisitEditorOperationIdPort>(
  'PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT',
  { providedIn: 'root', factory: () => inject(PassportOperationIdService) }
);
