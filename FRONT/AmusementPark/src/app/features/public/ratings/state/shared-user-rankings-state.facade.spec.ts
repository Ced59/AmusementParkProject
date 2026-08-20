import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRankingsPage,
  UserParkRatingRankingsPage,
} from '@app/models/ratings/rating.models';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import {
  SHARED_USER_RANKINGS_PORT,
  SharedUserRankingsPort,
} from './shared-user-rankings-state-data.ports';
import { SharedUserRankingsStateFacade } from './shared-user-rankings-state.facade';

class FakeSharedUserRankingsPort implements SharedUserRankingsPort {
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

describe('SharedUserRankingsStateFacade', () => {
  let facade: SharedUserRankingsStateFacade;
  let port: FakeSharedUserRankingsPort;

  beforeEach(() => {
    port = new FakeSharedUserRankingsPort();
    TestBed.configureTestingModule({
      providers: [
        SharedUserRankingsStateFacade,
        { provide: SHARED_USER_RANKINGS_PORT, useValue: port },
      ],
    });
    facade = TestBed.inject(SharedUserRankingsStateFacade);
  });

  it('loads the public profile and its park ranking from the opaque link', () => {
    facade.loadProfile('opaque-share-id');

    expect(facade.profile()?.displayName).toBe('Camille');
    expect(facade.parkRankings().map(item => item.parkId)).toEqual(['park-1']);
    expect(port.parkCalls).toEqual([
      { shareId: 'opaque-share-id', page: 1, search: null },
    ]);

    facade.loadMore();

    expect(facade.parkRankings().map(item => item.parkId)).toEqual([
      'park-1',
      'park-2',
    ]);
  });

  it('keeps category, attraction type and trimmed search in public filters', () => {
    facade.loadProfile('opaque-share-id');

    facade.load('Attraction', '  ride  ', 'FlatRide');

    expect(port.itemCalls).toEqual([
      {
        shareId: 'opaque-share-id',
        page: 1,
        category: 'Attraction',
        type: 'FlatRide',
        search: 'ride',
      },
    ]);
    expect(facade.parkRankings()).toEqual([]);
  });

  it('treats a revoked or unknown share link as not found', () => {
    port.profileResponse = throwError(() => ({ status: 404 }));

    facade.loadProfile('revoked-share-id');

    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
    expect(facade.profile()).toBeNull();
    expect(port.parkCalls).toEqual([]);
  });
});

function createProfile(): SharedUserRankingProfile {
  return {
    displayName: 'Camille',
    publishedAtUtc: '2026-08-20T18:00:00Z',
    isOwner: false,
    stats: {
      totalRatings: 2,
      averageRating: 4.5,
      highestRating: 5,
      lowestRating: 4,
      byPark: [],
      byTargetType: [],
      byParkItemCategory: [],
    },
  };
}
