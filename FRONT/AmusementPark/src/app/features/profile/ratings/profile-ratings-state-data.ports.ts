import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  UserParkItemRatingRankingsPage,
  UserParkRatingRankingsPage,
  UserRating,
  UserRatingStats,
  UserRatingUpsertRequest
} from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface ProfileRatingsPort {
  getMyParkRankings(page: number, size: number, search: string | null, targetId: string | null): Observable<UserParkRatingRankingsPage>;
  getMyParkItemRankings(page: number, size: number, category: string, type: string | null, search: string | null, targetId: string | null): Observable<UserParkItemRatingRankingsPage>;
  getMyRatingStats(): Observable<UserRatingStats>;
  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating>;
}

export const PROFILE_RATINGS_PORT = new InjectionToken<ProfileRatingsPort>('PROFILE_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
