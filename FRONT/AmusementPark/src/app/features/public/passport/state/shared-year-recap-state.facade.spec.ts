import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { SHARED_YEAR_RECAP_PORT, SharedYearRecapPort } from './shared-year-recap-state-data.ports';
import { SharedYearRecapStateFacade } from './shared-year-recap-state.facade';

describe('SharedYearRecapStateFacade', () => {
  it('loads a frozen annual recap from an opaque link', () => {
    const port: SharedYearRecapPort = {
      getSharedYear: (_shareId: string): Observable<SharedYearRecap> => of(createRecap())
    };
    TestBed.configureTestingModule({
      providers: [
        SharedYearRecapStateFacade,
        { provide: SHARED_YEAR_RECAP_PORT, useValue: port }
      ]
    });
    const facade: SharedYearRecapStateFacade = TestBed.inject(SharedYearRecapStateFacade);

    facade.load('opaque-share-id');

    expect(facade.recap()?.yearRecap.year).toBe(2026);
    expect(facade.loading()).toBe(false);
    expect(facade.notFound()).toBe(false);
  });

  it('maps a revoked or unknown link to not found', () => {
    const port: SharedYearRecapPort = {
      getSharedYear: (_shareId: string): Observable<SharedYearRecap> =>
        throwError(() => ({ status: 404 }))
    };
    TestBed.configureTestingModule({
      providers: [
        SharedYearRecapStateFacade,
        { provide: SHARED_YEAR_RECAP_PORT, useValue: port }
      ]
    });
    const facade: SharedYearRecapStateFacade = TestBed.inject(SharedYearRecapStateFacade);

    facade.load('revoked-share-id');

    expect(facade.recap()).toBeNull();
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
  });
});

function createRecap(): SharedYearRecap {
  return {
    publishedAtUtc: '2026-09-11T08:00:00Z',
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
