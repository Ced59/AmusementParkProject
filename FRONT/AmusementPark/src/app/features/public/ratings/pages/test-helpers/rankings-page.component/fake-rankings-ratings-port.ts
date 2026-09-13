import { Observable, of, throwError } from 'rxjs';

import { ParkItemRatingRankingsPage, ParkRatingRanking, RatingRankingsPage } from '@app/models/ratings/rating.models';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { DEFAULT_PAGINATION } from '@shared/models/contracts';

import { RankingsRatingsPort } from '../../../state/rankings-state-data.ports';

function createRanking(): ParkRatingRanking {
  return {
    rank: 1,
    parkId: 'park-1',
    parkName: 'Phantasialand',
    ratingCount: 8,
    ratingObservationCount: 2,
    uniqueContributorCount: 5,
    score: 4.3,
    parkRatingCount: 2,
    parkAverageRating: 4.5,
    itemsRatingCount: 6,
    itemsAverageRating: 4.2,
    methodologyVersion: 'ratings-2026-01',
    generatedAtUtc: '2026-09-02T08:00:00Z',
    evidence: {
      level: 'Provisional',
      isEligibleForMainRanking: false,
      directParkContributorCount: 2,
      itemContributorCount: 4,
      eligibleItemCount: 3,
      eligibleCategoryCount: 2,
      ineligibilityReason: 'TooFewUniqueContributors',
      nextThreshold: 10,
    },
    categories: [
      {
        parkItemCategory: 'Attraction',
        ratingCount: 6,
        averageRating: 4.2,
        bayesianScore: 4,
        items: [
          {
            targetId: 'item-1',
            targetName: 'Taron',
            parkItemCategory: 'Attraction',
            parkItemType: 'RollerCoaster',
            ratingCount: 3,
            averageRating: 4.5,
            bayesianScore: 4.1,
          },
        ],
      },
    ],
  };
}

function createMethodology(
  version: string = 'ratings-2026-01',
  eligibleThreshold: number = 10,
): RatingMethodology {
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
      eligible: eligibleThreshold,
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

export class FakeRankingsRatingsPort implements RankingsRatingsPort {
  methodologyError: unknown | null = null;
  methodologyCalls: number = 0;
  methodologyVersion: string = 'ratings-2026-01';
  methodologyEligibleThreshold: number = 10;
  readonly requestedMethodologyVersions: string[] = [];
  readonly parkItemCalls: Array<{
    page: number;
    category: string;
    type: string | null;
    search: string | null;
  }> = [];

  getCurrentMethodology(): Observable<RatingMethodology> {
    this.methodologyCalls += 1;
    if (this.methodologyError) {
      return throwError(() => this.methodologyError);
    }

    return of(createMethodology(this.methodologyVersion, this.methodologyEligibleThreshold));
  }

  getMethodology(version: string): Observable<RatingMethodology> {
    this.methodologyCalls += 1;
    this.requestedMethodologyVersions.push(version);
    if (this.methodologyError) {
      return throwError(() => this.methodologyError);
    }

    return of(createMethodology(version, this.methodologyEligibleThreshold));
  }

  getRankings(
    _page: number,
    _size: number,
    _category: string | null,
    _search: string | null,
    _options?: AnonymousHttpOptions,
  ): Observable<RatingRankingsPage> {
    const ranking: ParkRatingRanking = createRanking();
    return of({
      items: [{ ...ranking, methodologyVersion: this.methodologyVersion }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 20,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }

  getParkItemRankings(
    page: number,
    _size: number,
    category: string,
    type: string | null,
    search: string | null,
    _options?: AnonymousHttpOptions,
  ): Observable<ParkItemRatingRankingsPage> {
    this.parkItemCalls.push({ page, category, type, search });
    return of({
      items: [
        {
          rank: 2,
          targetId: 'item-1',
          targetName: 'Taron',
          parkId: 'park-1',
          parkName: 'Phantasialand',
          parkItemCategory: 'Attraction',
          parkItemType: 'RollerCoaster',
          ratingCount: 3,
          ratingObservationCount: 3,
          uniqueContributorCount: 38,
          averageRating: 4.5,
          bayesianScore: 4.1,
          methodologyVersion: this.methodologyVersion,
          generatedAtUtc: '2026-09-02T08:00:00Z',
          evidence: {
            level: 'Established',
            isEligibleForMainRanking: true,
            nextThreshold: 50,
          },
        },
      ],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: page,
        itemsPerPage: 20,
        totalItems: search ? 40 : 1,
        totalPages: search ? 2 : 1,
      },
    });
  }
}
