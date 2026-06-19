import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { UserRating, UserRatingStats, UserRatingUpsertRequest, UserRatingsPage } from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface ProfileRatingsPort {
  getMyRatings(page: number, size: number, search: string | null): Observable<UserRatingsPage>;
  getMyRatingStats(): Observable<UserRatingStats>;
  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating>;
}

export const PROFILE_RATINGS_PORT = new InjectionToken<ProfileRatingsPort>('PROFILE_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
