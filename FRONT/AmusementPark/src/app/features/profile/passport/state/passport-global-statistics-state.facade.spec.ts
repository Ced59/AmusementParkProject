import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { PassportGlobalStatistics } from '@app/models/passport/passport-statistics.models';
import {
  PASSPORT_GLOBAL_STATISTICS_FILTER_STORE,
  PassportGlobalStatisticsFilter
} from './passport-global-statistics-filter.ports';
import { PassportGlobalStatisticsStateFacade } from './passport-global-statistics-state.facade';
import { PASSPORT_STATISTICS_API_PORT } from './passport-statistics-state-data.ports';

describe('PassportGlobalStatisticsStateFacade', () => {
  const initialFilter: PassportGlobalStatisticsFilter = { year: 2025, parkId: 'park-1' };
  let api: { getGlobalStatistics: ReturnType<typeof vi.fn> };
  let store: {
    read: ReturnType<typeof vi.fn>;
    write: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    api = { getGlobalStatistics: vi.fn().mockReturnValue(of(createStatistics())) };
    store = { read: vi.fn().mockReturnValue(initialFilter), write: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        PassportGlobalStatisticsStateFacade,
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: api },
        { provide: PASSPORT_GLOBAL_STATISTICS_FILTER_STORE, useValue: store }
      ]
    });
  });

  it('restores session filters and loads server-calculated aggregates', () => {
    const facade: PassportGlobalStatisticsStateFacade = TestBed.inject(PassportGlobalStatisticsStateFacade);

    facade.load();

    expect(api.getGlobalStatistics).toHaveBeenCalledWith(2025, 'park-1');
    expect(facade.statistics()?.parkCount).toBe(1);
    expect(facade.loading()).toBe(false);
  });

  it('persists filter changes and reloads without putting identifiers in navigation state', () => {
    const facade: PassportGlobalStatisticsStateFacade = TestBed.inject(PassportGlobalStatisticsStateFacade);
    facade.load();

    facade.selectYear(null);
    facade.selectPark(null);

    expect(store.write).toHaveBeenNthCalledWith(1, { year: null, parkId: 'park-1' });
    expect(store.write).toHaveBeenNthCalledWith(2, { year: null, parkId: null });
    expect(api.getGlobalStatistics).toHaveBeenLastCalledWith(null, null);
  });

  it('restores the last successful filter when a reload fails', () => {
    const facade: PassportGlobalStatisticsStateFacade = TestBed.inject(PassportGlobalStatisticsStateFacade);
    facade.load();
    api.getGlobalStatistics.mockReturnValue(throwError(() => new Error('network')));

    facade.selectYear(2024);

    expect(facade.filter()).toEqual(initialFilter);
    expect(facade.statistics()?.parkCount).toBe(1);
    expect(facade.errorKey()).toBe('passport.globalStatistics.errors.load');
    expect(store.write).toHaveBeenLastCalledWith(initialFilter);
  });

  it('clears unavailable session filters before presenting their results', () => {
    api.getGlobalStatistics
      .mockReturnValueOnce(of({
        ...createStatistics(),
        availableYears: [2024],
        availableParks: []
      }))
      .mockReturnValueOnce(of({
        ...createStatistics(),
        selectedYear: null,
        selectedParkId: null,
        availableYears: [2024],
        availableParks: []
      }));
    const facade: PassportGlobalStatisticsStateFacade = TestBed.inject(PassportGlobalStatisticsStateFacade);

    facade.load();

    expect(api.getGlobalStatistics).toHaveBeenNthCalledWith(1, 2025, 'park-1');
    expect(store.write).toHaveBeenCalledWith({ year: null, parkId: null });
    expect(api.getGlobalStatistics).toHaveBeenNthCalledWith(2, null, null);
    expect(facade.filter()).toEqual({ year: null, parkId: null });
    expect(facade.statistics()?.selectedParkId).toBeNull();
  });
});

function createStatistics(): PassportGlobalStatistics {
  return {
    selectedYear: 2025,
    selectedParkId: 'park-1',
    availableYears: [2025],
    availableParks: [{ parkId: 'park-1', parkName: 'Parc test' }],
    parkCount: 1,
    summary: {
      visitCount: 1,
      approximateVisitCount: 0,
      parkRatingCoverage: { ratedCount: 0, totalCount: 1, rate: 0 },
      historicalParkRatings: null,
      firstVisit: null,
      lastVisit: null,
      rideOutcomes: { recordedOutcomeCount: 0, completedRideCount: 0, attemptedCount: 0, missedClosedCount: 0, missedUnavailableCount: 0, skippedByChoiceCount: 0 },
      rideRatingCoverage: { ratedCount: 0, totalCount: 0, rate: 0 },
      historicalRideRatings: null,
      distinctCompletedItemCount: 0,
      repeatedCompletedItemCount: 0,
      categoryCoverage: []
    },
    activityByYear: [], topParks: [], topItems: [], ratingEvolution: []
  };
}
