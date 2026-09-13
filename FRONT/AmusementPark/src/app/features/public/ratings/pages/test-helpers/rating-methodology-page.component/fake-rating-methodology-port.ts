import { Observable, of } from 'rxjs';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { RatingMethodologyPort } from '../../../state/rating-methodology-state-data.ports';

function createMethodology(): RatingMethodology {
  return {
    version: 'ratings-2026-01', effectiveDate: '2026-08-31', isCurrent: true, previousVersion: null,
    ratingScale: { minimum: 0.5, maximum: 5, step: 0.5 }, bayesian: { priorMean: 3.5, priorWeight: 10 },
    parkComposition: { directRatingWeight: 0.7, itemRatingWeight: 0.3, balancesItemCategoriesEqually: true, minimumEligibleItems: 5, minimumItemsPerCategory: 2, minimumCategories: 2 },
    evidenceThresholds: { provisional: 3, eligible: 10, established: 30, strong: 100 },
    publicationRules: { minimumEligibleEntries: 3, scoreTieEpsilon: 0.0001, rankingConvention: 'competition' }
  };
}

export class FakeRatingMethodologyPort implements RatingMethodologyPort {
  getCurrentMethodology(_options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    return of(createMethodology());
  }

  getMethodology(_version: string, _options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    return of(createMethodology());
  }

  getMethodologyHistory(_options?: AnonymousHttpOptions): Observable<RatingMethodology[]> {
    return of([createMethodology()]);
  }
}
