import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import { SHARED_YEAR_RECAP_PORT, SharedYearRecapPort } from './shared-year-recap-state-data.ports';
import { SharedYearRecapStateFacade } from './shared-year-recap-state.facade';

describe('SharedYearRecapStateFacade', () => {
  it('loads a frozen annual recap from an opaque link', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedYearRecapPort = {
      getSharedYear: (_shareId: string): Observable<SharedYearRecap> => of(createRecap())
    };
    TestBed.configureTestingModule({
      providers: [
        SharedYearRecapStateFacade,
        { provide: SHARED_YEAR_RECAP_PORT, useValue: port },
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
    const facade: SharedYearRecapStateFacade = TestBed.inject(SharedYearRecapStateFacade);

    facade.load('opaque-share-id');

    expect(facade.recap()?.yearRecap.year).toBe(2026);
    expect(facade.loading()).toBe(false);
    expect(facade.notFound()).toBe(false);
    facade.trackPassportCta();
    expect(analyticsEvents).toEqual([
      { type: 'share_opened', recapType: 'year-recap' },
      { type: 'share_cta_passport_started', recapType: 'year-recap' }
    ]);
  });

  it('maps a revoked or unknown link to not found', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedYearRecapPort = {
      getSharedYear: (_shareId: string): Observable<SharedYearRecap> =>
        throwError(() => ({ status: 404 }))
    };
    TestBed.configureTestingModule({
      providers: [
        SharedYearRecapStateFacade,
        { provide: SHARED_YEAR_RECAP_PORT, useValue: port },
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
    const facade: SharedYearRecapStateFacade = TestBed.inject(SharedYearRecapStateFacade);

    facade.load('revoked-share-id');

    expect(facade.recap()).toBeNull();
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
    expect(analyticsEvents).toEqual([]);
  });

  it('tracks a render failure without treating a revoked link as an error', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedYearRecapPort = {
      getSharedYear: (): Observable<SharedYearRecap> => throwError(() => ({ status: 503 }))
    };
    TestBed.configureTestingModule({ providers: [
      SharedYearRecapStateFacade,
      { provide: SHARED_YEAR_RECAP_PORT, useValue: port },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: SharedYearRecapStateFacade = TestBed.inject(SharedYearRecapStateFacade);

    facade.load('opaque-share-id');

    expect(analyticsEvents).toEqual([
      { type: 'share_render_failed', recapType: 'year-recap' }
    ]);
  });
});

function createRecap(): SharedYearRecap {
  return {
    publishedAtUtc: '2026-09-11T08:00:00Z',
    publicationVersion: 3,
    yearRecap: {
      year: 2026,
      visitCount: 2,
      approximateVisitCount: 0,
      approximateVisitRate: 0,
      categories: ['Attraction'],
      mostVisitedParks: [{ name: 'Denain Évasion', visitCount: 2 }],
      nowClosedItems: [],
      hasIncompleteCatalog: false,
      calculationVersion: 'passport-year-recap-v1',
      isEmpty: false
    }
  };
}
