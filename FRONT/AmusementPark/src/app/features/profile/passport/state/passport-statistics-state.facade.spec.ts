import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import {
  PassportItemStatistics,
  PassportStatisticsSummary
} from '@app/models/passport/passport-statistics.models';
import {
  PASSPORT_STATISTICS_API_PORT
} from './passport-statistics-state-data.ports';
import { PassportStatisticsStateFacade } from './passport-statistics-state.facade';

describe('PassportStatisticsStateFacade', () => {
  let statisticsApi: {
    getItemStatistics: ReturnType<typeof vi.fn>;
    getParkStatistics: ReturnType<typeof vi.fn>;
    getYearStatistics: ReturnType<typeof vi.fn>;
  };
  beforeEach(() => {
    statisticsApi = {
      getItemStatistics: vi.fn().mockReturnValue(of(createItemStatistics())),
      getParkStatistics: vi.fn().mockReturnValue(of({
        parkId: 'park-1', parkName: 'Parc test', summary: createSummary(), currentGlobalRating: null,
        currentGlobalMinusHistoricalAverage: null, assessmentTimeline: [], byYear: [],
        currentTopItems: [], historicalTopItems: []
      })),
      getYearStatistics: vi.fn().mockReturnValue(of({
        year: 2025, parkCount: 1, summary: createSummary(), byPark: []
      }))
    };
    TestBed.configureTestingModule({
      providers: [
        PassportStatisticsStateFacade,
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: statisticsApi }
      ]
    });
  });

  it('loads item statistics once and maps the server-provided target label', () => {
    const facade: PassportStatisticsStateFacade = TestBed.inject(PassportStatisticsStateFacade);

    facade.load({ kind: 'item', targetId: 'item-1' }, 'fr');

    expect(statisticsApi.getItemStatistics).toHaveBeenCalledWith('item-1');
    expect(facade.viewModel()).toEqual(expect.objectContaining({ title: 'Attraction test' }));
    expect(facade.loading()).toBe(false);
  });

  it('keeps statistics available without exposing the identifier when the target label is unavailable', () => {
    statisticsApi.getItemStatistics.mockReturnValue(of({
      ...createItemStatistics(),
      parkItemName: null
    }));
    const facade: PassportStatisticsStateFacade = TestBed.inject(PassportStatisticsStateFacade);

    facade.load({ kind: 'item', targetId: 'item-1' }, 'en');

    expect(facade.viewModel()?.title).toBe('');
    expect(facade.viewModel()?.titleKey).toBe('passport.statistics.targets.unavailableItem');
    expect(facade.errorKey()).toBeNull();
  });

  it('loads park and year scopes through the statistics port', () => {
    const facade: PassportStatisticsStateFacade = TestBed.inject(PassportStatisticsStateFacade);

    facade.load({ kind: 'park', targetId: 'park-1' }, 'fr');
    expect(facade.viewModel()).toEqual(expect.objectContaining({ title: 'Parc test' }));
    facade.load({ kind: 'year', targetId: '2025' }, 'fr');

    expect(statisticsApi.getParkStatistics).toHaveBeenCalledWith('park-1');
    expect(statisticsApi.getYearStatistics).toHaveBeenCalledWith(2025);
    expect(facade.viewModel()?.scope).toEqual({ kind: 'year', targetId: '2025' });
  });

  it('rejects invalid years locally and exposes recoverable API failures', () => {
    const facade: PassportStatisticsStateFacade = TestBed.inject(PassportStatisticsStateFacade);
    facade.load({ kind: 'year', targetId: 'not-a-year' }, 'en');

    expect(statisticsApi.getYearStatistics).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('passport.statistics.errors.invalidScope');

    statisticsApi.getYearStatistics.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 503 })));
    facade.load({ kind: 'year', targetId: '2025' }, 'en');
    expect(facade.errorKey()).toBe('passport.statistics.errors.load');
  });

  it('reformats the existing result on language changes without another API call', () => {
    const facade: PassportStatisticsStateFacade = TestBed.inject(PassportStatisticsStateFacade);
    facade.load({ kind: 'item', targetId: 'item-1' }, 'en');

    facade.changeLanguage('fr');

    expect(statisticsApi.getItemStatistics).toHaveBeenCalledTimes(1);
    expect(facade.viewModel()?.cards.find((card) => card.id === 'historicalAverage')?.value).toBe('4,25 / 5');
  });
});

function createItemStatistics(): PassportItemStatistics {
  return {
    parkItemId: 'item-1', parkItemName: 'Attraction test', rideCount: 1, visitCount: 1,
    ratingCoverage: { ratedRideCount: 1, totalRideCount: 1, rate: 1 },
    firstExperience: null, lastExperience: null,
    historicalRatings: { ratingCount: 1, average: 4.25, median: 4.25, minimum: 4.25, maximum: 4.25, populationStandardDeviation: 0 },
    currentGlobalRating: null, currentGlobalMinusHistoricalAverage: null,
    byVisit: [], byYear: [], ratingTimeline: [], trend: null
  };
}

function createSummary(): PassportStatisticsSummary {
  return {
    visitCount: 1, approximateVisitCount: 0,
    parkRatingCoverage: { ratedCount: 0, totalCount: 1, rate: 0 }, historicalParkRatings: null,
    firstVisit: null, lastVisit: null,
    rideOutcomes: { recordedOutcomeCount: 1, completedRideCount: 1, attemptedCount: 0, missedClosedCount: 0, missedUnavailableCount: 0, skippedByChoiceCount: 0 },
    rideRatingCoverage: { ratedCount: 0, totalCount: 1, rate: 0 }, historicalRideRatings: null,
    distinctCompletedItemCount: 1, repeatedCompletedItemCount: 0, categoryCoverage: []
  };
}
