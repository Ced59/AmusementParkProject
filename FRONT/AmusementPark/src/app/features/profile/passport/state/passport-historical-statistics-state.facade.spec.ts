import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { PassportHistoricalStatistics } from '@app/models/passport/passport-statistics.models';
import { PASSPORT_STATISTICS_API_PORT } from './passport-statistics-state-data.ports';
import { PassportHistoricalStatisticsStateFacade } from './passport-historical-statistics-state.facade';

describe('PassportHistoricalStatisticsStateFacade', () => {
  it('loads private historical statistics', () => {
    const statistics: PassportHistoricalStatistics = createStatistics();
    TestBed.configureTestingModule({
      providers: [
        PassportHistoricalStatisticsStateFacade,
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: { getHistoricalStatistics: () => of(statistics) } }
      ]
    });
    const facade: PassportHistoricalStatisticsStateFacade = TestBed.inject(
      PassportHistoricalStatisticsStateFacade
    );

    facade.load();

    expect(facade.statistics()).toEqual(statistics);
    expect(facade.loading()).toBe(false);
    expect(facade.errorKey()).toBeNull();
  });

  it('keeps a localized error state when loading fails', () => {
    TestBed.configureTestingModule({
      providers: [
        PassportHistoricalStatisticsStateFacade,
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: { getHistoricalStatistics: () => throwError(() => new Error('offline')) } }
      ]
    });
    const facade: PassportHistoricalStatisticsStateFacade = TestBed.inject(
      PassportHistoricalStatisticsStateFacade
    );

    facade.load();

    expect(facade.statistics()).toBeNull();
    expect(facade.errorKey()).toBe('passport.historicalStatistics.errors.load');
  });
});

function createStatistics(): PassportHistoricalStatistics {
  return {
    visitCount: 2,
    firstVisitYear: 2001,
    completedRideCount: 1,
    canonicallyResolvedRideCount: 1,
    canonicalCoverageRate: 1,
    parkCountAcrossMultipleEras: 1,
    parksAcrossEras: [],
    disappearedAttractions: [],
    transformations: [],
    historicalNames: [],
    historicalCategories: []
  };
}
