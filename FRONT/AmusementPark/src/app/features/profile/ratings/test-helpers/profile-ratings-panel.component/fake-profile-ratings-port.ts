import { Observable, of, Subject } from 'rxjs';

import { UserParkItemRatingRanking, UserParkItemRatingRankingsPage, UserParkRatingRanking, UserParkRatingRankingsPage, UserRating, UserRatingListItem, UserRatingStats, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';

import { DEFAULT_PAGINATION } from '@shared/models/contracts';

import { ProfileRatingsPort } from '../../profile-ratings-state-data.ports';

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

export class FakeProfileRatingsPort implements ProfileRatingsPort {
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  parkItemResponse: Subject<UserParkItemRatingRankingsPage> | null = null;
  readonly parkItemCalls: Array<{
    page: number;
    category: string;
    type: string | null;
    search: string | null;
    targetId: string | null;
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

  getMyParkRankings(
    _page: number,
    _size: number,
    _search: string | null,
    _targetId: string | null
  ): Observable<UserParkRatingRankingsPage> {
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
    search: string | null,
    targetId: string | null
  ): Observable<UserParkItemRatingRankingsPage> {
    this.parkItemCalls.push({ page, category, type, search, targetId });
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
