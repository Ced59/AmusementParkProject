import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateLiveTargetMappingCandidateRequest,
  LiveTargetMapping,
  LiveTargetMappingPage,
  LiveTargetMappingQuery,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { AdminLiveTargetMappingsApiService } from '@data-access/admin/admin-live-target-mappings-api.service';

export interface AdminLiveTargetMappingsDataPort {
  search(query: LiveTargetMappingQuery): Observable<LiveTargetMappingPage>;
  createCandidate(request: CreateLiveTargetMappingCandidateRequest): Observable<LiveTargetMapping>;
  review(mappingId: string, request: ReviewLiveTargetMappingRequest): Observable<LiveTargetMapping>;
}

export const ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT =
  new InjectionToken<AdminLiveTargetMappingsDataPort>(
    'ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): AdminLiveTargetMappingsDataPort => inject(AdminLiveTargetMappingsApiService)
    }
  );
