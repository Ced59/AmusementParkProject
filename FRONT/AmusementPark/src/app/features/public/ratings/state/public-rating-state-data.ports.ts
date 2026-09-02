import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { RatingSummary, RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface PublicRatingRatingsPort {
  getCurrentMethodology(options?: AnonymousHttpOptions): Observable<RatingMethodology>;
  getSummary(targetType: RatingTargetType, targetId: string): Observable<RatingSummary>;
  getMyRating(targetType: RatingTargetType, targetId: string): Observable<UserRating | null>;
  deleteMyRating(targetType: RatingTargetType, targetId: string): Observable<RatingSummary>;
  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating>;
}

export const PUBLIC_RATING_RATINGS_PORT = new InjectionToken<PublicRatingRatingsPort>('PUBLIC_RATING_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
