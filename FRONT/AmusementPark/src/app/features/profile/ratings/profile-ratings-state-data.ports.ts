import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { UserRatingStats, UserRatingsPage } from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface ProfileRatingsPort {
  getMyRatings(page: number, size: number): Observable<UserRatingsPage>;
  getMyRatingStats(): Observable<UserRatingStats>;
}

export const PROFILE_RATINGS_PORT = new InjectionToken<ProfileRatingsPort>('PROFILE_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
