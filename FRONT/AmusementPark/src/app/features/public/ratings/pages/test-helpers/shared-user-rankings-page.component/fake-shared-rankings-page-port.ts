import { Observable, of } from 'rxjs';

import { SharedUserRankingProfile, UserParkItemRatingRankingsPage, UserParkRatingRankingsPage } from '@app/models/ratings/rating.models';

import { DEFAULT_PAGINATION } from '@shared/models/contracts';

import { SharedUserRankingsPort } from '../../../state/shared-user-rankings-state-data.ports';

function createRating() {
  return {
    id: 'rating-1',
    targetType: 'ParkItem' as const,
    targetId: 'item-1',
    targetName: 'Talocan',
    parkId: 'park-1',
    parkName: 'Phantasialand',
    parkItemCategory: 'Attraction',
    parkItemType: 'FlatRide',
    value: 4.5,
    updatedAtUtc: '2026-08-20T18:00:00Z',
    summary: {
      targetType: 'ParkItem' as const,
      targetId: 'item-1',
      ratingCount: 1,
      averageRating: 4.5,
      bayesianScore: 4,
    },
  };
}

export class FakeSharedRankingsPagePort implements SharedUserRankingsPort {
  readonly itemCalls: Array<{ category: string; type: string | null }> = [];
  profile: SharedUserRankingProfile = {
    displayName: 'Camille',
    publishedAtUtc: '2026-08-20T18:00:00Z',
    isOwner: false,
    stats: {
      totalRatings: 2,
      averageRating: 4.5,
      highestRating: 5,
      lowestRating: 4
    },
  };

  getSharedProfile(_shareId: string): Observable<SharedUserRankingProfile> {
    return of(this.profile);
  }

  getSharedParkRankings(
    _shareId: string,
    _page: number,
    _size: number,
    _search: string | null,
  ): Observable<UserParkRatingRankingsPage> {
    return of({
      items: [{
        rank: 1,
        parkId: 'park-1',
        parkName: 'Phantasialand',
        ratingCount: 2,
        averageRating: 4.5,
        parkRating: null,
        categories: [{
          parkItemCategory: 'Attraction',
          averageRating: 4.5,
          items: [createRating()],
        }],
      }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }

  getSharedParkItemRankings(
    _shareId: string,
    _page: number,
    _size: number,
    category: string,
    type: string | null,
    _search: string | null,
  ): Observable<UserParkItemRatingRankingsPage> {
    this.itemCalls.push({ category, type });
    return of({
      items: [{ rank: 1, rating: createRating() }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }
}
