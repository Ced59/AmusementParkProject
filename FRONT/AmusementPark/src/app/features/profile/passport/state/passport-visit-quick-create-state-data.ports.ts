import { inject, InjectionToken } from '@angular/core';

import { ParksApiService } from '@data-access/parks/parks-api.service';
import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { PassportVisitsApiService } from '@data-access/passport/passport-visits-api.service';

export interface PassportVisitQuickCreateApiPort extends Pick<PassportVisitsApiService, 'createVisit'> {
}

export interface PassportVisitQuickCreateParksPort extends Pick<ParksApiService, 'searchParks'> {
}

export interface PassportVisitOperationIdPort extends Pick<PassportOperationIdService, 'create'> {
}

export const PASSPORT_VISIT_QUICK_CREATE_API_PORT = new InjectionToken<PassportVisitQuickCreateApiPort>(
  'PASSPORT_VISIT_QUICK_CREATE_API_PORT',
  { providedIn: 'root', factory: () => inject(PassportVisitsApiService) }
);

export const PASSPORT_VISIT_QUICK_CREATE_PARKS_PORT = new InjectionToken<PassportVisitQuickCreateParksPort>(
  'PASSPORT_VISIT_QUICK_CREATE_PARKS_PORT',
  { providedIn: 'root', factory: () => inject(ParksApiService) }
);

export const PASSPORT_VISIT_OPERATION_ID_PORT = new InjectionToken<PassportVisitOperationIdPort>(
  'PASSPORT_VISIT_OPERATION_ID_PORT',
  { providedIn: 'root', factory: () => inject(PassportOperationIdService) }
);
