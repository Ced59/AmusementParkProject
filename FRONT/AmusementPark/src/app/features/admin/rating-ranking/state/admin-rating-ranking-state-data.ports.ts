import { inject, InjectionToken } from '@angular/core';

import { AdminRatingRankingApiService } from '@app/data-access/admin/admin-rating-ranking-api.service';

export interface AdminRatingRankingStatePort extends Pick<
  AdminRatingRankingApiService,
  'getDashboard' | 'previewImpact' | 'rebuild'> {
}

export const ADMIN_RATING_RANKING_STATE_PORT =
  new InjectionToken<AdminRatingRankingStatePort>('ADMIN_RATING_RANKING_STATE_PORT', {
    providedIn: 'root',
    factory: () => inject(AdminRatingRankingApiService)
  });
