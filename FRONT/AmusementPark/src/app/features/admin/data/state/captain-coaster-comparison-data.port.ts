import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterDuplicateResolutionRequest,
  ComparisonFilters
} from '@app/models/admin/data/data-management.models';
import { DataSourcesApiService } from '@data-access/admin/data-sources-api.service';

export interface CaptainCoasterComparisonDataPort {
  getComparisonResults(
    sessionId: string | null | undefined,
    filters: ComparisonFilters,
    page: number,
    pageSize: number
  ): Observable<CaptainCoasterComparisonPagedResponse>;

  applySelectedIds(ids: string[], duplicateResolutions: CaptainCoasterDuplicateResolutionRequest[]): Observable<{ appliedCount: number }>;

  applyAll(sessionId: string | null, entityTypeFilter: string | null, changeTypeFilter: string | null): Observable<{ appliedCount: number }>;
}

export const CAPTAIN_COASTER_COMPARISON_DATA_PORT = new InjectionToken<CaptainCoasterComparisonDataPort>('CAPTAIN_COASTER_COMPARISON_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(DataSourcesApiService)
});
