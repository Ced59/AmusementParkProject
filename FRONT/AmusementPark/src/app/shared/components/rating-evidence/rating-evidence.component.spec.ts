import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import {
  RankingEvidenceLevel,
  RankingIneligibilityReason,
} from '@app/models/ratings/rating.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import {
  RatingEvidenceComponent,
  RatingEvidenceViewModel,
} from './rating-evidence.component';

describe('RatingEvidenceComponent', () => {
  let fixture: ComponentFixture<RatingEvidenceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingEvidenceComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      ratings: {
        evidence: createTranslations(),
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(RatingEvidenceComponent);
  });

  it.each([
    ['NoEvidence', 'Aucune donnée'],
    ['Insufficient', 'Données insuffisantes'],
    ['Provisional', 'Provisoire'],
    ['Eligible', 'Éligible'],
    ['Established', 'Établi'],
    ['StrongEvidence', 'Preuves solides'],
    ['Excluded', 'Exclu'],
  ] satisfies Array<[RankingEvidenceLevel, string]>) (
    'renders the %s evidence level without opening a panel',
    (level: RankingEvidenceLevel, expectedLabel: string) => {
      fixture.componentRef.setInput('mode', 'badge');
      fixture.componentRef.setInput('model', createModel(level));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.rating-evidence__badge')?.textContent)
        .toContain(expectedLabel);
      expect(fixture.nativeElement.querySelector('details')).toBeNull();
    },
  );

  it('explains progress using unique contributors separately from observations', () => {
    fixture.componentRef.setInput('model', {
      ...createModel('Provisional'),
      uniqueContributorCount: 7,
      ratingObservationCount: 9,
      evidence: {
        level: 'Provisional',
        isEligibleForMainRanking: false,
        nextThreshold: 10,
        ineligibilityReason: 'TooFewUniqueContributors',
      },
    } satisfies RatingEvidenceViewModel);
    fixture.detectChanges();

    const details: HTMLDetailsElement | null = fixture.nativeElement.querySelector('details');
    const facts: HTMLElement | null = fixture.nativeElement.querySelector('.rating-evidence__facts');

    expect(details).not.toBeNull();
    expect(details?.querySelector('summary')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('7 contributeurs uniques sur 10');
    expect(facts?.textContent).toContain('Contributeurs uniques');
    expect(facts?.textContent).toContain('7');
    expect(facts?.textContent).toContain('Notes conservées');
    expect(facts?.textContent).toContain('9');
    expect(facts?.textContent).toContain('Prochain seuil');
    expect(fixture.nativeElement.textContent).toContain('Pas assez de contributeurs uniques.');
  });

  it('shows the park evidence composition without inventing absent values', () => {
    fixture.componentRef.setInput('model', {
      ...createModel('Insufficient', 'InsufficientItemCoverage'),
      targetType: 'Park',
      uniqueContributorCount: 2,
      ratingObservationCount: 4,
      evidence: {
        level: 'Insufficient',
        isEligibleForMainRanking: false,
        directParkContributorCount: 2,
        itemContributorCount: 3,
        eligibleItemCount: 1,
        eligibleCategoryCount: null,
        ineligibilityReason: 'InsufficientItemCoverage',
        nextThreshold: 3,
      },
    } satisfies RatingEvidenceViewModel);
    fixture.detectChanges();

    const composition: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-evidence__composition',
    );

    expect(composition?.textContent).toContain('Composition du parc');
    expect(composition?.textContent).toContain('Contributeurs directs');
    expect(composition?.textContent).toContain('Contributeurs des lieux');
    expect(composition?.textContent).toContain('Lieux éligibles');
    expect(composition?.textContent).not.toContain('Catégories éligibles');
    expect(fixture.nativeElement.textContent).toContain('Pas assez de lieux éligibles.');
  });

  it('states the public rank and exact methodology for eligible evidence', () => {
    fixture.componentRef.setInput('model', {
      ...createModel('Established'),
      uniqueContributorCount: 38,
      rank: 12,
      methodologyVersion: 'ratings-2026-01',
      evidence: {
        level: 'Established',
        isEligibleForMainRanking: true,
        nextThreshold: 50,
      },
    } satisfies RatingEvidenceViewModel);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent)
      .toContain('Classé #12 avec la méthode ratings-2026-01.');
  });
});

function createModel(
  level: RankingEvidenceLevel,
  ineligibilityReason: RankingIneligibilityReason | null = null,
): RatingEvidenceViewModel {
  return {
    evidence: {
      level,
      isEligibleForMainRanking: ['Eligible', 'Established', 'StrongEvidence'].includes(level),
      ineligibilityReason,
      nextThreshold: null,
    },
    uniqueContributorCount: 0,
    ratingObservationCount: null,
    targetType: 'ParkItem',
    rank: null,
    methodologyVersion: 'ratings-2026-01',
    eligibilityThreshold: 10,
  };
}

function createTranslations(): Record<string, unknown> {
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
      insufficientItemCoverage: 'Pas assez de lieux éligibles.',
    },
  };
}
