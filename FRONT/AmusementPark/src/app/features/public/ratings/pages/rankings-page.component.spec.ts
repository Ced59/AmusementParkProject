import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import {
  ParkItemRatingRankingsPage,
  ParkRatingRanking,
  RatingRankingsPage,
} from '@app/models/ratings/rating.models';
import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { TranslationService } from '@app/services/translation.service';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SeoService } from '@core/seo/seo.service';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import {
  RANKINGS_RATINGS_PORT,
  RankingsRatingsPort,
} from '../state/rankings-state-data.ports';
import { RankingsPageComponent } from './rankings-page.component';

registerLocaleData(localeFr);

class FakeRankingsRatingsPort implements RankingsRatingsPort {
  methodologyError: unknown | null = null;
  methodologyCalls: number = 0;
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

    return of(createMethodology());
  }

  getRankings(
    _page: number,
    _size: number,
    _category: string | null,
    _search: string | null,
    _options?: AnonymousHttpOptions,
  ): Observable<RatingRankingsPage> {
    return of({
      items: [createRanking()],
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
          methodologyVersion: 'ratings-2026-01',
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

describe('RankingsPageComponent', () => {
  let fixture: ComponentFixture<RankingsPageComponent>;
  let port: FakeRankingsRatingsPort;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RankingsPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: RANKINGS_RATINGS_PORT,
          useClass: FakeRankingsRatingsPort,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
            parent: null,
          },
        },
        {
          provide: TranslationService,
          useValue: {
            getCurrentLang: (): string => 'fr',
            languageChanged: of('fr'),
          },
        },
        {
          provide: SeoService,
          useValue: {
            applyRouteDefaults: vi.fn(),
          },
        },
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      ratings: {
        rankings: {
          title: 'Classements des visiteurs',
          ratingCount: {
            one: '{{count}} note',
            other: '{{count}} notes',
          },
          totalRatingCount: {
            one: '{{count}} note au total',
            other: '{{count}} notes au total',
          },
          rankingScore: 'Score agrégé du classement',
          parkSignal: 'Note directe du parc',
          itemsSignal: 'Moyenne de tous les lieux',
          evidenceSummary: {
            label: 'Résumé des preuves',
            threshold: 'Le classement demande {{threshold}} contributeurs uniques.',
            rankedLabel: 'Classés affichés',
            rankedDisplayed: {
              one: '{{count}} résultat',
              other: '{{count}} résultats',
            },
            provisionalLabel: 'Provisoires affichés',
            provisionalDisplayed: {
              one: '{{count}} résultat',
              other: '{{count}} résultats',
            },
            methodology: 'Méthode {{version}}',
            generatedAt: 'Calculé le {{date}}',
            generatedAtUnavailable: 'Date indisponible',
            displayedScope: 'Compte les résultats actuellement chargés.',
          },
        },
        methodology: {
          actions: {
            rankings: 'Comprendre la méthode',
          },
        },
        evidence: createEvidenceTranslations(),
        categories: {
          Attraction: 'Attractions',
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(RankingsPageComponent);
    port = TestBed.inject(RANKINGS_RATINGS_PORT) as FakeRankingsRatingsPort;
  });

  it('renders one localized page heading', () => {
    fixture.detectChanges();

    const headings: NodeListOf<HTMLHeadingElement> =
      fixture.nativeElement.querySelectorAll('h1');

    expect(headings).toHaveLength(1);
    expect(headings[0]?.textContent?.trim()).toBe('Classements des visiteurs');
  });

  it('maps raw rating counts to every ranking level', () => {
    fixture.detectChanges();

    const parkCount: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__park-main .rating-tree__rating-count',
    );
    const metricCounts: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-tree__metric-count');
    const metrics: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-tree__metric');
    const scoreLabel: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-tree__score-label');
    const sectionCount: HTMLElement | null =
      fixture.nativeElement.querySelector(
        '.rating-tree__section-main .rating-tree__rating-count',
      );
    const itemCount: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__item-main .rating-tree__rating-count',
    );

    expect(parkCount?.textContent?.trim()).toBe('8 notes au total');
    expect(scoreLabel?.textContent?.trim()).toBe('Score agrégé du classement');
    expect(metrics[0]?.textContent).toContain('Note directe du parc');
    expect(metrics[1]?.textContent).toContain('Moyenne de tous les lieux');
    expect(
      Array.from(metricCounts).map((element: HTMLElement): string =>
        element.textContent?.trim() ?? '',
      ),
    ).toEqual(['2 notes', '6 notes']);
    expect(sectionCount?.textContent?.trim()).toBe('6 notes');
    expect(itemCount?.textContent?.trim()).toBe('3 notes');
  });

  it('hides an ineligible park place and exposes its evidence composition', () => {
    fixture.detectChanges();

    const parkSummary: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__park-summary',
    );
    const evidence: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__park-content app-rating-evidence',
    );

    expect(parkSummary?.textContent).not.toContain('#1');
    expect(parkSummary?.textContent).toContain('Provisoire');
    expect(evidence?.textContent).toContain('2 personnes l’ont évalué directement');
    expect(evidence?.textContent).toContain('Notes directes');
    expect(evidence?.textContent).toContain('Composition du parc');
    expect(evidence?.textContent).toContain('3');
  });

  it('summarizes loaded eligibility data with the versioned methodology link', () => {
    fixture.detectChanges();

    const summary: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rankings-evidence-summary',
    );
    const link: HTMLAnchorElement | null = summary?.querySelector('a') ?? null;

    expect(summary?.textContent).toContain('10 contributeurs uniques');
    expect(summary?.textContent).toContain('0 résultat');
    expect(summary?.textContent).toContain('1 résultat');
    expect(summary?.textContent).toContain('Compte les résultats actuellement chargés.');
    expect(link?.getAttribute('href')).toBe(
      '/fr/rankings/methodology/ratings-2026-01',
    );
  });

  it('keeps rankings available when the methodology endpoint is temporarily unavailable', () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    port.methodologyError = new Error('methodology unavailable');

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-rating-tree')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.rankings-evidence-summary')).toBeNull();
    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Error loading rating methodology',
      port.methodologyError,
    );
  });

  it('shows a cross-park attraction ranking and filters it by attraction type', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.rankings-filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const list: HTMLElement | null =
      fixture.nativeElement.querySelector('app-rating-ranking-list');
    expect(list?.textContent).toContain('#2');
    expect(list?.textContent).toContain('Taron');
    expect(list?.textContent).toContain('Phantasialand');
    expect(fixture.nativeElement.querySelector('app-rating-tree')).toBeNull();
    expect(port.parkItemCalls[0]).toEqual({
      page: 1,
      category: 'Attraction',
      type: null,
      search: null,
    });

    const typeSelect: HTMLSelectElement =
      fixture.nativeElement.querySelector('.rankings-type-filter select');
    typeSelect.value = 'FlatRide';
    typeSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(port.parkItemCalls.at(-1)).toEqual({
      page: 1,
      category: 'Attraction',
      type: 'FlatRide',
      search: null,
    });
    expect(port.methodologyCalls).toBe(1);
  });

  it('loads the next park item search page with the active search term', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.rankings-filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const searchInput: HTMLInputElement =
      fixture.nativeElement.querySelector('.rankings-search input');
    searchInput.value = ' ride ';
    searchInput.dispatchEvent(new Event('input'));
    const searchButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.rankings-search button');
    searchButton.click();
    fixture.detectChanges();

    const loadMoreButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.rankings-more button');
    expect(loadMoreButton).toBeTruthy();
    loadMoreButton.click();
    fixture.detectChanges();

    expect(port.parkItemCalls.at(-1)).toEqual({
      page: 2,
      category: 'Attraction',
      type: null,
      search: 'ride',
    });
  });
});

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
        one: '{{count}} personne l’a évalué directement.',
        other: '{{count}} personnes l’ont évalué directement.',
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
