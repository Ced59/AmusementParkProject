import { inject, InjectionToken } from '@angular/core';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';

export interface AdminOperatorsStateParkOperatorsApiServicePort extends Pick<ParkOperatorsApiService, 'getAllParkOperators' | 'updateParkOperatorsBulkReviewStatus'> {
}

export const ADMIN_OPERATORS_STATE_PARK_OPERATORS_API_SERVICE_PORT = new InjectionToken<AdminOperatorsStateParkOperatorsApiServicePort>('ADMIN_OPERATORS_STATE_PARK_OPERATORS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkOperatorsApiService)
});
