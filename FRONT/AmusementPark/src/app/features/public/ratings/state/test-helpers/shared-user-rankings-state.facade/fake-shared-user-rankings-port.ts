import { Observable, of } from 'rxjs';

import { SharedUserRankingProfile, UserParkItemRatingRankingsPage, UserParkRatingRankingsPage } from '@app/models/ratings/rating.models';

import { DEFAULT_PAGINATION } from '@shared/models/contracts';

import { SharedUserRankingsPort } from '../../shared-user-rankings-state-data.ports';

function createProfile(): SharedUserRankingProfile {
  return {
    displayName: 'Camille',
    publishedAtUtc: '2026-08-20T18:00:00Z',
    isOwner: false,
    publicationVersion: 7,
    stats: {
      totalRatings: 2,
      averageRating: 4.5,
      highestRating: 5,
      lowestRating: 4
    },
  };
}

export class FakeSharedUserRankingsPort implements SharedUserRankingsPort {
  profileResponse: Observable<SharedUserRankingProfile> = of(createProfile());
  readonly parkCalls: Array<{ shareId: string; page: number; search: string | null }> = [];
  readonly itemCalls: Array<{
    shareId: string;
    page: number;
    category: string;
    type: string | null;
    search: string | null;
  }> = [];

  getSharedProfile(_shareId: string): Observable<SharedUserRankingProfile> {
    return this.profileResponse;
  }

  getSharedParkRankings(
    shareId: string,
    page: number,
    _size: number,
    search: string | null,
  ): Observable<UserParkRatingRankingsPage> {
    this.parkCalls.push({ shareId, page, search });
    return of({
      items: [{
        rank: page,
        parkId: `park-${page}`,
        parkName: `Park ${page}`,
        ratingCount: 1,
        averageRating: 4.5,
        parkRating: null,
        categories: [],
      }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: page,
        itemsPerPage: 10,
        totalItems: 2,
        totalPages: 2,
      },
    });
  }

  getSharedParkItemRankings(
    shareId: string,
    page: number,
    _size: number,
    category: string,
    type: string | null,
    search: string | null,
  ): Observable<UserParkItemRatingRankingsPage> {
    this.itemCalls.push({ shareId, page, category, type, search });
    return of({
      items: [],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: page,
        itemsPerPage: 10,
        totalItems: 0,
        totalPages: 0,
      },
    });
  }
}
