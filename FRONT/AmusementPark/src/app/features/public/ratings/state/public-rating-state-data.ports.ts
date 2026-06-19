import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface PublicRatingRatingsPort {
  getMyRating(targetType: RatingTargetType, targetId: string): Observable<UserRating | null>;
  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating>;
}

export const PUBLIC_RATING_RATINGS_PORT = new InjectionToken<PublicRatingRatingsPort>('PUBLIC_RATING_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
