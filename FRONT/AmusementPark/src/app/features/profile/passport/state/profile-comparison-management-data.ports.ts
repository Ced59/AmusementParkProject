import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ProfileComparisonRevocation,
  ProfileComparisonSummary,
} from '@app/models/sharing/profile-comparison.models';
import { ProfileComparisonsApiService } from '@data-access/sharing/profile-comparisons-api.service';

export interface ProfileComparisonManagementPort {
  listMine(): Observable<ProfileComparisonSummary[]>;
  revoke(shareId: string): Observable<ProfileComparisonRevocation>;
}

export const PROFILE_COMPARISON_MANAGEMENT_PORT =
  new InjectionToken<ProfileComparisonManagementPort>(
    'PROFILE_COMPARISON_MANAGEMENT_PORT',
    {
      providedIn: 'root',
      factory: (): ProfileComparisonManagementPort => {
        const apiService: ProfileComparisonsApiService = inject(
          ProfileComparisonsApiService,
        );
        return {
          listMine: (): Observable<ProfileComparisonSummary[]> =>
            apiService.listMine(),
          revoke: (shareId: string): Observable<ProfileComparisonRevocation> =>
            apiService.revoke(shareId),
        };
      },
    },
  );
