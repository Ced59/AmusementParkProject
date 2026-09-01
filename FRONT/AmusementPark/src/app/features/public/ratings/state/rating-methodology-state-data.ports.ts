import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { RatingsApiService } from '@data-access/ratings/ratings-api.service';

export interface RatingMethodologyPort {
  getCurrentMethodology(options?: AnonymousHttpOptions): Observable<RatingMethodology>;
  getMethodology(version: string, options?: AnonymousHttpOptions): Observable<RatingMethodology>;
  getMethodologyHistory(options?: AnonymousHttpOptions): Observable<RatingMethodology[]>;
}

export const RATING_METHODOLOGY_PORT = new InjectionToken<RatingMethodologyPort>('RATING_METHODOLOGY_PORT', {
  providedIn: 'root',
  factory: () => inject(RatingsApiService)
});
