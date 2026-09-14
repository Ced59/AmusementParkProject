import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedProfileComparison } from '@app/models/sharing/profile-comparison.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import {
  SHARED_PROFILE_COMPARISON_PORT,
  SharedProfileComparisonPort,
} from './shared-profile-comparison-state-data.ports';
import { SharedProfileComparisonStateFacade } from './shared-profile-comparison-state.facade';

describe('SharedProfileComparisonStateFacade', () => {
  it('loads the consented result from its opaque token', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const comparison: SharedProfileComparison = {
      createdAtUtc: '2026-09-13T12:00:00Z',
      creatorDisplayName: 'Camille',
      acceptorDisplayName: 'Alex',
      categories: ['VisitedParks'],
      parks: [],
      ratings: [],
      years: [],
      missedItems: [],
      commonRatingCount: 0,
      minimumRatingsForCorrelation: 5,
      ratingCorrelation: null,
      hasIncompleteCatalog: false,
      calculationVersion: 'profile-comparison-v1',
    };
    const port: SharedProfileComparisonPort = {
      getShared: (shareId: string): Observable<SharedProfileComparison> => {
        expect(shareId).toBe('opaque-share');
        return of(comparison);
      },
    };
    TestBed.configureTestingModule({
      providers: [
        SharedProfileComparisonStateFacade,
        { provide: SHARED_PROFILE_COMPARISON_PORT, useValue: port },
        {
          provide: SHARE_PRODUCT_ANALYTICS_PORT,
          useValue: {
            track: (event: ShareProductEvent): void => {
              analyticsEvents.push(event);
            }
          }
        },
      ],
    });
    const facade: SharedProfileComparisonStateFacade = TestBed.inject(
      SharedProfileComparisonStateFacade,
    );

    facade.load('opaque-share');

    expect(facade.comparison()).toEqual(comparison);
    expect(facade.loading()).toBe(false);
    expect(analyticsEvents).toEqual([
      { type: 'share_opened', recapType: 'profile-comparison' }
    ]);
  });

  it('tracks a non-not-found rendering failure', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedProfileComparisonPort = {
      getShared: (): Observable<SharedProfileComparison> => throwError(() => ({ status: 503 }))
    };
    TestBed.configureTestingModule({ providers: [
      SharedProfileComparisonStateFacade,
      { provide: SHARED_PROFILE_COMPARISON_PORT, useValue: port },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: SharedProfileComparisonStateFacade = TestBed.inject(
      SharedProfileComparisonStateFacade
    );

    facade.load('opaque-share');

    expect(analyticsEvents).toEqual([
      { type: 'share_render_failed', recapType: 'profile-comparison' }
    ]);
  });
});
