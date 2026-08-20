import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { UserRankingShareSettings } from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface UserRankingSharePort {
  getMyShareSettings(): Observable<UserRankingShareSettings>;
  setMyShareVisibility(isPublic: boolean): Observable<UserRankingShareSettings>;
}

export const USER_RANKING_SHARE_PORT = new InjectionToken<UserRankingSharePort>('USER_RANKING_SHARE_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
