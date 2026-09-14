import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedVisitRecap } from '@app/models/sharing/share-publication.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import { SHARED_VISIT_RECAP_PORT, SharedVisitRecapPort } from './shared-visit-recap-state-data.ports';
import { SharedVisitRecapStateFacade } from './shared-visit-recap-state.facade';

describe('SharedVisitRecapStateFacade', () => {
  it('loads a public frozen recap from the opaque link', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedVisitRecapPort = {
      getSharedVisit: (_shareId: string): Observable<SharedVisitRecap> => of(createRecap())
    };
    TestBed.configureTestingModule({
      providers: [
        SharedVisitRecapStateFacade,
        { provide: SHARED_VISIT_RECAP_PORT, useValue: port },
        {
          provide: SHARE_PRODUCT_ANALYTICS_PORT,
          useValue: {
            track: (event: ShareProductEvent): void => {
              analyticsEvents.push(event);
            }
          }
        }
      ]
    });
    const facade: SharedVisitRecapStateFacade = TestBed.inject(SharedVisitRecapStateFacade);

    facade.load('opaque-share-id');

    expect(facade.recap()?.visitRecap.parkName).toBe('Denain Évasion');
    expect(facade.loading()).toBe(false);
    expect(facade.notFound()).toBe(false);
    facade.trackPassportCta();
    expect(analyticsEvents).toEqual([
      { type: 'share_opened', recapType: 'visit-recap' },
      { type: 'share_cta_passport_started', recapType: 'visit-recap' }
    ]);
  });

  it('tracks a render failure without treating it as an opening', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedVisitRecapPort = {
      getSharedVisit: (_shareId: string): Observable<SharedVisitRecap> =>
        throwError(() => ({ status: 503 }))
    };
    TestBed.configureTestingModule({
      providers: [
        SharedVisitRecapStateFacade,
        { provide: SHARED_VISIT_RECAP_PORT, useValue: port },
        {
          provide: SHARE_PRODUCT_ANALYTICS_PORT,
          useValue: {
            track: (event: ShareProductEvent): void => {
              analyticsEvents.push(event);
            }
          }
        }
      ]
    });
    const facade: SharedVisitRecapStateFacade = TestBed.inject(SharedVisitRecapStateFacade);

    facade.load('opaque-share-id');

    expect(facade.error()).toBe(true);
    expect(analyticsEvents).toEqual([
      { type: 'share_render_failed', recapType: 'visit-recap' }
    ]);
  });

  it('maps a revoked or unknown link to not found', () => {
    const port: SharedVisitRecapPort = {
      getSharedVisit: (_shareId: string): Observable<SharedVisitRecap> => throwError(() => ({ status: 404 }))
    };
    TestBed.configureTestingModule({
      providers: [
        SharedVisitRecapStateFacade,
        { provide: SHARED_VISIT_RECAP_PORT, useValue: port }
      ]
    });
    const facade: SharedVisitRecapStateFacade = TestBed.inject(SharedVisitRecapStateFacade);

    facade.load('revoked-share-id');

    expect(facade.recap()).toBeNull();
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
  });
});

function createRecap(): SharedVisitRecap {
  return {
    publishedAtUtc: '2026-09-07T08:00:00Z',
    publicationVersion: 3,
    visitRecap: {
      parkId: 'park-1',
      parkName: 'Denain Évasion',
      categories: ['Attraction'],
      items: [],
      hasHiddenDate: true,
      hasIncompleteRatings: false,
      hasIncompleteItems: false
    }
  };
}
