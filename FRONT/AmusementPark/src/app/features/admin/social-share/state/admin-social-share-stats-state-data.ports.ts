import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminSocialShareStatsApiService } from '@data-access/social-share/admin-social-share-stats-api.service';
import {
  SocialShareStatsQuery,
  SocialShareStatsResult
} from '@app/models/social-share/social-share.models';

export interface AdminSocialShareStatsDataPort {
  getStats(query: SocialShareStatsQuery): Observable<SocialShareStatsResult>;
}

export const ADMIN_SOCIAL_SHARE_STATS_DATA_PORT = new InjectionToken<AdminSocialShareStatsDataPort>('ADMIN_SOCIAL_SHARE_STATS_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(AdminSocialShareStatsApiService)
});
