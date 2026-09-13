import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { SharedProfileComparison } from '@app/models/sharing/profile-comparison.models';
import { ProfileComparisonsApiService } from '@data-access/sharing/profile-comparisons-api.service';

export interface SharedProfileComparisonPort {
  getShared(shareId: string): Observable<SharedProfileComparison>;
}

export const SHARED_PROFILE_COMPARISON_PORT =
  new InjectionToken<SharedProfileComparisonPort>(
    'SHARED_PROFILE_COMPARISON_PORT',
    {
      providedIn: 'root',
      factory: (): SharedProfileComparisonPort => {
        const apiService: ProfileComparisonsApiService = inject(
          ProfileComparisonsApiService,
        );
        return {
          getShared: (shareId: string): Observable<SharedProfileComparison> =>
            apiService.getShared(shareId),
        };
      },
    },
  );
