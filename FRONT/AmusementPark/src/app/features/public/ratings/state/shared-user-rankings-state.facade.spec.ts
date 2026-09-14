import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRankingsPage,
  UserParkRatingRankingsPage,
} from '@app/models/ratings/rating.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import {
  SHARED_USER_RANKINGS_PORT,
  SharedUserRankingsPort,
} from './shared-user-rankings-state-data.ports';
import { SharedUserRankingsStateFacade } from './shared-user-rankings-state.facade';
import { FakeSharedUserRankingsPort } from './test-helpers/shared-user-rankings-state.facade/fake-shared-user-rankings-port';

describe('SharedUserRankingsStateFacade', () => {
  let facade: SharedUserRankingsStateFacade;
  let port: FakeSharedUserRankingsPort;
  let analyticsEvents: ShareProductEvent[];

  beforeEach(() => {
    port = new FakeSharedUserRankingsPort();
    analyticsEvents = [];
    TestBed.configureTestingModule({
      providers: [
        SharedUserRankingsStateFacade,
        { provide: SHARED_USER_RANKINGS_PORT, useValue: port },
        {
          provide: SHARE_PRODUCT_ANALYTICS_PORT,
          useValue: {
            track: (event: ShareProductEvent): void => {
              analyticsEvents.push(event);
            }
          },
        },
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
    expect(analyticsEvents).toEqual([
      { type: 'share_opened', recapType: 'personal-ranking' },
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
    expect(analyticsEvents).toEqual([]);
  });

  it('tracks a public ranking render failure without any technical identifier', () => {
    port.profileResponse = throwError(() => ({ status: 503 }));

    facade.loadProfile('opaque-share-id');

    expect(facade.error()).toBe(true);
    expect(analyticsEvents).toEqual([
      { type: 'share_render_failed', recapType: 'personal-ranking' },
    ]);
  });
});

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
