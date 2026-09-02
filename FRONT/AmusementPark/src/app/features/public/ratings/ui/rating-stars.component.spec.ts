import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { RatingSummary } from '@app/models/ratings/rating.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { PublicRatingStateFacade } from '../state/public-rating-state.facade';
import { RatingStarsComponent } from './rating-stars.component';

class FakePublicRatingStateFacade {
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

describe('RatingStarsComponent', () => {
  let fixture: ComponentFixture<RatingStarsComponent>;
  let facade: FakePublicRatingStateFacade;

  afterEach(() => {
    vi.restoreAllMocks();
  });

  beforeEach(async () => {
    facade = new FakePublicRatingStateFacade();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingStarsComponent],
      providers: provideCommonTestDependencies(),
    })
      .overrideComponent(RatingStarsComponent, {
        set: {
          providers: [
            {
              provide: PublicRatingStateFacade,
              useValue: facade,
            },
          ],
        },
      })
      .compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      ratings: {
        stars: {
          yourRating: 'Ta note : {{value}}/5',
          clearRating: 'Effacer ma note',
          clearRatingConfirm: 'Veux-tu vraiment effacer ta note ?',
          prompt: 'Choisis ta note',
          rankLabel: 'Classé #{{rank}}',
          historicalHint: 'Ces notes reflètent des visites passées.',
        },
        methodology: {
          actions: {
            ratingZone: 'Comprendre le classement',
          },
        },
        evidence: createEvidenceTranslations(),
      },
      publicCounts: {
        averageRating: {
          one: 'Note moyenne {{value}} sur 5',
          other: 'Note moyenne {{value}} sur 5',
        },
        rating: {
          one: '{{count}} note',
          other: '{{count}} notes',
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(RatingStarsComponent);
    fixture.componentRef.setInput('targetType', 'ParkItem');
    fixture.componentRef.setInput('targetId', 'item-1');
    fixture.detectChanges();
  });

  it('shows the exact personal rating and offers to clear it', () => {
    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');

    expect(message?.textContent).toContain('Ta note : 3,5/5');
    expect(clearButton?.textContent?.trim()).toBe('Effacer ma note');

    clearButton?.click();

    expect(confirmSpy).toHaveBeenCalledWith(
      'Veux-tu vraiment effacer ta note ?',
    );
    expect(facade.removeRating).toHaveBeenCalledTimes(1);
  });

  it('shows the target place when it belongs to a ranking', () => {
    const rank: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__rank');

    expect(rank?.textContent?.trim()).toBe('Classé #4');
  });

  it('hides an ineligible place and explains the provisional evidence', () => {
    facade.summary.set({
      targetType: 'ParkItem',
      targetId: 'item-1',
      ratingCount: 9,
      ratingObservationCount: 9,
      uniqueContributorCount: 7,
      averageRating: 4.8,
      bayesianScore: 4.1,
      rank: 4,
      methodologyVersion: 'ratings-2026-01',
      evidence: {
        level: 'Provisional',
        isEligibleForMainRanking: false,
        ineligibilityReason: 'TooFewUniqueContributors',
        nextThreshold: 10,
      },
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-stars__rank')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Provisoire');
    expect(fixture.nativeElement.textContent).toContain('7 contributeurs uniques');
    expect(fixture.nativeElement.textContent).toContain('10');
    expect(
      fixture.nativeElement.querySelector('.rating-stars__methodology')?.getAttribute('href'),
    ).toBe('/fr/rankings/methodology/ratings-2026-01');
  });

  it('keeps an eligible rank and identifies its methodology version', () => {
    facade.summary.set({
      targetType: 'ParkItem',
      targetId: 'item-1',
      ratingCount: 41,
      uniqueContributorCount: 38,
      averageRating: 4.6,
      bayesianScore: 4.3,
      rank: 12,
      methodologyVersion: 'ratings-2026-01',
      evidence: {
        level: 'Established',
        isEligibleForMainRanking: true,
        nextThreshold: 50,
      },
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-stars__rank')?.textContent).toContain('#12');
    expect(fixture.nativeElement.textContent)
      .toContain('méthode ratings-2026-01');
  });

  it('explains when ratings describe past visits', () => {
    fixture.componentRef.setInput('contextHintKey', 'ratings.stars.historicalHint');
    fixture.detectChanges();

    const context: HTMLElement | null = fixture.nativeElement.querySelector('.rating-stars__context');

    expect(context?.textContent).toContain('Ces notes reflètent des visites passées.');
  });

  it('keeps the rating when removal is not confirmed', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');

    clearButton?.click();

    expect(facade.removeRating).not.toHaveBeenCalled();
  });

  it('formats the public and personal ratings with the active locale', () => {
    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      ratings: {
        stars: {
          averageLabel: 'Average rating',
          yourRating: 'Your rating: {{value}}/5',
          clearRating: 'Clear my rating',
        },
      },
      publicCounts: {
        averageRating: {
          one: 'Average rating {{value}} out of 5',
          other: 'Average rating {{value}} out of 5',
        },
        rating: {
          one: '{{count}} rating',
          other: '{{count}} ratings',
        },
      },
    });
    translateService.use('en');
    fixture.detectChanges();

    const average: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__average');
    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');

    expect(average?.textContent?.trim()).toBe('4.0');
    expect(message?.textContent).toContain('Your rating: 3.5/5');
  });

  it('fills the interactive stars from the personal rating rather than the community average', () => {
    const stars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-stars__star');

    expect(stars[0]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(stars[2]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(stars[3]?.style.getPropertyValue('--fill')).toBe('50%');
    expect(stars[4]?.style.getPropertyValue('--fill')).toBe('0%');
  });

  it('keeps the personal control empty when the visitor has not rated the target', () => {
    facade.userRatingValue.set(null);
    fixture.detectChanges();

    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');
    const stars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-stars__star');

    expect(message?.textContent?.trim()).toBe('Choisis ta note');
    expect(clearButton).toBeNull();
    expect(
      Array.from(stars).map((star: HTMLElement): string =>
        star.style.getPropertyValue('--fill'),
      ),
    ).toEqual(['0%', '0%', '0%', '0%', '0%']);
  });
});

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

function createEvidenceTranslations(): Record<string, unknown> {
  return {
    detailsAction: 'Voir les preuves',
    levels: {
      noEvidence: 'Aucune donnée',
      insufficient: 'Données insuffisantes',
      provisional: 'Provisoire',
      eligible: 'Éligible',
      established: 'Établi',
      strongEvidence: 'Preuves solides',
      excluded: 'Exclu',
    },
    messages: {
      noEvidence: { one: 'Aucune preuve.', other: 'Aucune preuve.' },
      excluded: { one: 'Exclu.', other: 'Exclu.' },
      insufficient: {
        one: '{{count}} contributeur unique sur {{threshold}}.',
        other: '{{count}} contributeurs uniques sur {{threshold}}.',
      },
      provisional: {
        one: '{{count}} contributeur unique sur {{threshold}}.',
        other: '{{count}} contributeurs uniques sur {{threshold}}.',
      },
      insufficientWithoutThreshold: {
        one: 'Échantillon insuffisant.',
        other: 'Échantillon insuffisant.',
      },
      provisionalWithoutThreshold: {
        one: 'Tendance provisoire.',
        other: 'Tendance provisoire.',
      },
      parkDirectProvisional: {
        one: '{{count}} contributeur direct au parc.',
        other: '{{count}} contributeurs directs au parc.',
      },
      ranked: {
        one: 'Classé #{{rank}} avec la méthode {{version}}.',
        other: 'Classé #{{rank}} avec la méthode {{version}}.',
      },
      eligibleWithoutRank: {
        one: 'Éligible avec la méthode {{version}}.',
        other: 'Éligible avec la méthode {{version}}.',
      },
    },
    facts: {
      uniqueContributors: 'Contributeurs uniques',
      observations: 'Notes conservées',
      directObservations: 'Notes directes',
      nextEvidenceThreshold: 'Prochain seuil',
    },
    composition: {
      title: 'Composition du parc',
      directContributors: 'Contributeurs directs',
      itemContributors: 'Contributeurs des lieux',
      eligibleItems: 'Lieux éligibles',
      eligibleCategories: 'Catégories éligibles',
    },
    reasonLabel: 'Pourquoi :',
    reasons: {
      tooFewUniqueContributors: 'Pas assez de contributeurs uniques.',
    },
  };
}
