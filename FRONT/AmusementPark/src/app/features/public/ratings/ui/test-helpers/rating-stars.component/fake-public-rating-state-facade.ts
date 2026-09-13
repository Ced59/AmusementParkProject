import { signal, WritableSignal } from '@angular/core';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';

import { RatingSummary } from '@app/models/ratings/rating.models';

function createMethodology(): RatingMethodology {
  return {
    version: 'ratings-2026-01',
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

export class FakePublicRatingStateFacade {
  readonly methodology: WritableSignal<RatingMethodology | null> = signal<RatingMethodology | null>(
    createMethodology(),
  );
  readonly summary: WritableSignal<RatingSummary | null> = signal<RatingSummary | null>({
    targetType: 'ParkItem',
    targetId: 'item-1',
    ratingCount: 2,
    averageRating: 4,
    bayesianScore: 3.5,
    rank: 4,
  });
  readonly saving: WritableSignal<boolean> = signal<boolean>(false);
  readonly messageKey: WritableSignal<string | null> = signal<string | null>(null);
  readonly userRatingValue: WritableSignal<number | null> = signal<number | null>(3.5);
  readonly configure = vi.fn();
  readonly rate = vi.fn();
  readonly removeRating = vi.fn();
}
