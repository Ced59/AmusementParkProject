import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRankingsPage,
  UserParkRatingRankingsPage
} from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface SharedUserRankingsPort {
  getSharedProfile(shareId: string): Observable<SharedUserRankingProfile>;
  getSharedParkRankings(shareId: string, page: number, size: number, search: string | null): Observable<UserParkRatingRankingsPage>;
  getSharedParkItemRankings(
    shareId: string,
    page: number,
    size: number,
    category: string,
    type: string | null,
    search: string | null
  ): Observable<UserParkItemRatingRankingsPage>;
}

export const SHARED_USER_RANKINGS_PORT = new InjectionToken<SharedUserRankingsPort>('SHARED_USER_RANKINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
