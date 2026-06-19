import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { UserRating, UserRatingListItem, UserRatingStats, UserRatingUpsertRequest, UserRatingsPage } from '@app/models/ratings/rating.models';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';
import { ProfileRatingsPanelComponent } from './profile-ratings-panel.component';

class FakeProfileRatingsPort implements ProfileRatingsPort {
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  ratings: UserRatingListItem[] = [
    createRatingListItem('rating-park-1', 'Park', 'park-1', 'Phantasialand', 5, null),
    createRatingListItem('rating-item-1', 'ParkItem', 'item-1', 'Taron', 4, 'Attraction')
  ];

  getMyRatings(_page: number, _size: number, _search: string | null): Observable<UserRatingsPage> {
    return of({
      items: this.ratings,
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: this.ratings.length,
        totalPages: 1
      }
    });
  }

  getMyRatingStats(): Observable<UserRatingStats> {
    return of(createStats());
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    this.upsertCalls.push(request);
    const rating: UserRatingListItem | undefined = this.ratings.find((item: UserRatingListItem): boolean => {
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
