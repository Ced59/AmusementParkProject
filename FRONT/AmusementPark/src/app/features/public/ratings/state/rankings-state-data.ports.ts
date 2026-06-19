import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { RatingRankingsPage } from '@app/models/ratings/rating.models';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface RankingsRatingsPort {
  getRankings(page: number, size: number, category: string | null, search: string | null, options?: AnonymousHttpOptions): Observable<RatingRankingsPage>;
}

export const RANKINGS_RATINGS_PORT = new InjectionToken<RankingsRatingsPort>('RANKINGS_RATINGS_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
