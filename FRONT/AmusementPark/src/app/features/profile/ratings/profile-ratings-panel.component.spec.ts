import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, Subject } from 'rxjs';

import {
  UserParkItemRatingRanking,
  UserParkItemRatingRankingsPage,
  UserParkRatingRanking,
  UserParkRatingRankingsPage,
  UserRating,
  UserRatingListItem,
  UserRatingStats,
  UserRatingUpsertRequest
} from '@app/models/ratings/rating.models';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';
import { ProfileRatingsPanelComponent } from './profile-ratings-panel.component';

class FakeProfileRatingsPort implements ProfileRatingsPort {
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  parkItemResponse: Subject<UserParkItemRatingRankingsPage> | null = null;
  readonly parkItemCalls: Array<{
    page: number;
    category: string;
    type: string | null;
    search: string | null;
  }> = [];
  readonly parkRankings: UserParkRatingRanking[] = [
    {
      rank: 1,
      parkId: 'park-1',
      parkName: 'Phantasialand',
      ratingCount: 2,
      averageRating: 4.5,
      parkRating: createRatingListItem('rating-park-1', 'Park', 'park-1', 'Phantasialand', 5, null),
      categories: [
        {
          parkItemCategory: 'Attraction',
          averageRating: 4,
          items: [
            createRatingListItem('rating-item-1', 'ParkItem', 'item-1', 'Taron', 4, 'Attraction')
          ]
        }
      ]
    }
  ];
  readonly parkItemRankings: UserParkItemRatingRanking[] = [
    {
      rank: 1,
      rating: createRatingListItem('rating-item-1', 'ParkItem', 'item-1', 'Taron', 4, 'Attraction')
    }
  ];

  getMyParkRankings(_page: number, _size: number, _search: string | null): Observable<UserParkRatingRankingsPage> {
    return of({
      items: this.parkRankings,
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: this.parkRankings.length,
        totalPages: 1
      }
    });
  }

  getMyParkItemRankings(
    page: number,
    _size: number,
    category: string,
    type: string | null,
    search: string | null
  ): Observable<UserParkItemRatingRankingsPage> {
    this.parkItemCalls.push({ page, category, type, search });
    if (this.parkItemResponse) {
      return this.parkItemResponse;
    }

    return of({
      items: this.parkItemRankings,
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: page,
        itemsPerPage: 10,
        totalItems: search ? 20 : this.parkItemRankings.length,
        totalPages: search ? 2 : 1
      }
    });
  }

  getMyRatingStats(): Observable<UserRatingStats> {
    return of(createStats());
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    this.upsertCalls.push(request);
    const ratings: UserRatingListItem[] = [
      this.parkRankings[0].parkRating!,
      ...this.parkRankings[0].categories.flatMap(category => category.items)
    ];
    const rating: UserRatingListItem | undefined = ratings.find((item: UserRatingListItem): boolean => {
      return item.targetType === request.targetType && item.targetId === request.targetId;
    });

    return of({
      id: rating?.id ?? 'rating-1',
      targetType: request.targetType,
      targetId: request.targetId,
      parkId: rating?.parkId ?? 'park-1',
      parkItemCategory: rating?.parkItemCategory ?? null,
      parkItemType: rating?.parkItemType ?? null,
      value: request.value,
      createdAtUtc: '2026-06-19T10:00:00Z',
      updatedAtUtc: '2026-06-19T11:00:00Z',
      summary: {
        targetType: request.targetType,
        targetId: request.targetId,
        ratingCount: 2,
        averageRating: request.value,
        bayesianScore: request.value
      }
    });
  }
}

describe('ProfileRatingsPanelComponent', () => {
  let fixture: ComponentFixture<ProfileRatingsPanelComponent>;
  let port: FakeProfileRatingsPort;

  beforeEach(async () => {
    port = new FakeProfileRatingsPort();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ProfileRatingsPanelComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: PROFILE_RATINGS_PORT, useValue: port }
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileRatingsPanelComponent);
  });

  it('renders direct park ratings as park metrics instead of nested park sections', () => {
    fixture.detectChanges();

    const tree: HTMLElement | null = fixture.nativeElement.querySelector('app-rating-tree');
    const text: string = tree?.textContent ?? '';

    expect(text).toContain('ratings.rankings.parkSignal');
    expect(text).toContain('ratings.rankings.itemsSignal');
    expect(text).toContain('ratings.categories.Attraction');
    expect(text).not.toContain('ratings.targetTypes.Park');
  });

  it('updates an already displayed rating from inline stars', () => {
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.rating-tree__items .rating-tree__star-hit--right');
    buttons[2]?.click();

    expect(port.upsertCalls).toEqual([
      { targetType: 'ParkItem', targetId: 'item-1', value: 3 }
    ]);
  });

  it('shows a flat attraction ranking with its place and parent park', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const list: HTMLElement | null = fixture.nativeElement.querySelector('app-rating-ranking-list');
    expect(list?.textContent).toContain('#1');
    expect(list?.textContent).toContain('Taron');
    expect(list?.textContent).toContain('Phantasialand');
    expect(fixture.nativeElement.querySelector('app-rating-tree')).toBeNull();
  });

  it('opens coaster and flat ride rankings from the quick filters', () => {
    fixture.detectChanges();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    categoryButtons[1]?.click();
    fixture.detectChanges();

    const quickFilterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__quick-filters button');
    quickFilterButtons[1]?.click();
    quickFilterButtons[2]?.click();

    expect(port.parkItemCalls.slice(-2)).toEqual([
      { page: 1, category: 'Attraction', type: 'RollerCoaster', search: null },
      { page: 1, category: 'Attraction', type: 'FlatRide', search: null }
    ]);
  });

  it('exposes every park item category as a direct ranking filter', () => {
    fixture.detectChanges();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');

    expect(categoryButtons).toHaveLength(10);
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Animal');
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Show');
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Transport');
  });

  it('hides the previous result count while a new ranking is loading', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.profile-ratings__result-count')).not.toBeNull();
    port.parkItemResponse = new Subject<UserParkItemRatingRankingsPage>();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    categoryButtons[1]?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.profile-ratings__result-count')).toBeNull();
  });

  it('loads the next personal park item search page with the active search term', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const searchInput: HTMLInputElement =
      fixture.nativeElement.querySelector('.profile-ratings__search input');
    searchInput.value = ' ride ';
    searchInput.dispatchEvent(new Event('input'));
    const searchButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.profile-ratings__search button');
    searchButton.click();
    fixture.detectChanges();

    const loadMoreButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.profile-ratings__more button');
    expect(loadMoreButton).toBeTruthy();
    loadMoreButton.click();
    fixture.detectChanges();

    expect(port.parkItemCalls.at(-1)).toEqual({
      page: 2,
      category: 'Attraction',
      type: null,
      search: 'ride'
    });
  });
});

function createRatingListItem(
  id: string,
  targetType: 'Park' | 'ParkItem',
  targetId: string,
  targetName: string,
  value: number,
  category: string | null
): UserRatingListItem {
  return {
    id,
    targetType,
    targetId,
    targetName,
    parkId: 'park-1',
    parkName: 'Phantasialand',
    parkItemCategory: category,
    parkItemType: null,
    value,
    updatedAtUtc: '2026-06-19T10:00:00Z',
    summary: {
      targetType,
      targetId,
      ratingCount: 2,
      averageRating: value,
      bayesianScore: value
    }
  };
}

function createStats(): UserRatingStats {
  return {
    totalRatings: 2,
    averageRating: 4.5,
    highestRating: 5,
    lowestRating: 4,
    byPark: [
      { key: 'park-1', label: 'Phantasialand', count: 2, averageRating: 4.5 }
    ],
    byTargetType: [
      { key: 'Park', label: 'Parcs', count: 1, averageRating: 5 },
      { key: 'ParkItem', label: 'Lieux', count: 1, averageRating: 4 }
    ],
    byParkItemCategory: [
      { key: 'Attraction', label: 'Attractions', count: 1, averageRating: 4 }
    ]
  };
}
