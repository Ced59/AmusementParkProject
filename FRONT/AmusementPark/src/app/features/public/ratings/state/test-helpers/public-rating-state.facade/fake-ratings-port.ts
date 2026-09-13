import { Observable, of } from 'rxjs';

import { RatingSummary, RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';

import { PublicRatingRatingsPort } from '../../public-rating-state-data.ports';

function createUserRating(
  value: number,
  ratingCount: number,
  averageRating: number,
): UserRating {
  return {
    id: 'rating-1',
    targetType: 'ParkItem',
    targetId: 'item-1',
    parkId: 'park-1',
    parkItemCategory: 'Attraction',
    parkItemType: 'RollerCoaster',
    value,
    createdAtUtc: '2026-06-19T10:00:00Z',
    updatedAtUtc: '2026-06-19T10:00:00Z',
    summary: {
      targetType: 'ParkItem',
      targetId: 'item-1',
      ratingCount,
      averageRating,
      bayesianScore: 3.8,
    },
  };
}

function createMethodology(version: string = 'ratings-2026-01'): RatingMethodology {
  return {
    version,
    effectiveDate: '2026-09-01',
    isCurrent: true,
    previousVersion: null,
    ratingScale: { minimum: 0.5, maximum: 5, step: 0.5 },
    bayesian: { priorMean: 3.5, priorWeight: 5 },
    parkComposition: {
      directRatingWeight: 0.4,
      itemRatingWeight: 0.6,
      balancesItemCategoriesEqually: true,
      minimumEligibleItems: 3,
      minimumItemsPerCategory: 1,
      minimumCategories: 2,
    },
    evidenceThresholds: {
      provisional: 3,
      eligible: 10,
      established: 25,
      strong: 50,
    },
    publicationRules: {
      minimumEligibleEntries: 3,
      scoreTieEpsilon: 0.001,
      rankingConvention: 'competition',
    },
  };
}

export class FakeRatingsPort implements PublicRatingRatingsPort {
  readonly getCurrentMethodologyCalls: number[] = [];
  readonly getMethodologyCalls: string[] = [];
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  readonly getSummaryCalls: Array<{
    targetType: RatingTargetType;
    targetId: string;
  }> = [];
  readonly getMyRatingCalls: Array<{
    targetType: RatingTargetType;
    targetId: string;
  }> = [];
  readonly deleteMyRatingCalls: Array<{
    targetType: RatingTargetType;
    targetId: string;
  }> = [];
  ratingResponse: UserRating = createUserRating(4.5, 3, 4.5);
  myRatingResponse: UserRating | null = null;
  summaryResponse: RatingSummary = {
    targetType: 'ParkItem',
    targetId: 'item-1',
    ratingCount: 1,
    averageRating: 4,
    bayesianScore: 3.4,
    rank: 7,
  };
  deleteResponse: RatingSummary = {
    targetType: 'ParkItem',
    targetId: 'item-1',
    ratingCount: 1,
    averageRating: 4,
    bayesianScore: 3.4,
  };

  getCurrentMethodology(): Observable<RatingMethodology> {
    this.getCurrentMethodologyCalls.push(this.getCurrentMethodologyCalls.length + 1);
    return of(createMethodology());
  }

  getMethodology(version: string): Observable<RatingMethodology> {
    this.getMethodologyCalls.push(version);
    return of(createMethodology(version));
  }

  getSummary(
    targetType: RatingTargetType,
    targetId: string,
  ): Observable<RatingSummary> {
    this.getSummaryCalls.push({ targetType, targetId });
    return of(this.summaryResponse);
  }

  getMyRating(
    targetType: RatingTargetType,
    targetId: string,
  ): Observable<UserRating | null> {
    this.getMyRatingCalls.push({ targetType, targetId });
    return of(this.myRatingResponse);
  }

  deleteMyRating(
    targetType: RatingTargetType,
    targetId: string,
  ): Observable<RatingSummary> {
    this.deleteMyRatingCalls.push({ targetType, targetId });
    return of(this.deleteResponse);
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    this.upsertCalls.push(request);
    return of(this.ratingResponse);
  }
}
