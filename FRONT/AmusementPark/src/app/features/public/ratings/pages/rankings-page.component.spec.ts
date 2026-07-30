import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import {
  ParkItemRatingRankingsPage,
  ParkRatingRanking,
  RatingRankingsPage,
} from '@app/models/ratings/rating.models';
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

class FakeRankingsRatingsPort implements RankingsRatingsPort {
  readonly parkItemCalls: Array<{
    category: string;
    type: string | null;
  }> = [];

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
    _page: number,
    _size: number,
    category: string,
    type: string | null,
    _search: string | null,
    _options?: AnonymousHttpOptions,
  ): Observable<ParkItemRatingRankingsPage> {
    this.parkItemCalls.push({ category, type });
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
          averageRating: 4.5,
          bayesianScore: 4.1,
        },
      ],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 20,
        totalItems: 1,
        totalPages: 1,
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
        },
        categories: {
          Attraction: 'Attractions',
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(RankingsPageComponent);
    port = TestBed.inject(RANKINGS_RATINGS_PORT) as FakeRankingsRatingsPort;
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
      category: 'Attraction',
      type: null,
    });

    const typeSelect: HTMLSelectElement =
      fixture.nativeElement.querySelector('.rankings-type-filter select');
    typeSelect.value = 'FlatRide';
    typeSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(port.parkItemCalls.at(-1)).toEqual({
      category: 'Attraction',
      type: 'FlatRide',
    });
  });
});

function createRanking(): ParkRatingRanking {
  return {
    rank: 1,
    parkId: 'park-1',
    parkName: 'Phantasialand',
    ratingCount: 8,
    score: 4.3,
    parkRatingCount: 2,
    parkAverageRating: 4.5,
    itemsRatingCount: 6,
    itemsAverageRating: 4.2,
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
