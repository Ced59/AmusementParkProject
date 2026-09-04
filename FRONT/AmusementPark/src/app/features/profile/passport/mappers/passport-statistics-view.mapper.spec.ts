import {
  PassportItemStatistics,
  PassportParkStatistics,
  PassportStatisticsSummary,
  PassportYearStatistics
} from '@app/models/passport/passport-statistics.models';
import {
  mapItemStatisticsView,
  mapParkStatisticsView,
  mapYearStatisticsView
} from './passport-statistics-view.mapper';

describe('passport statistics view mapper', () => {
  it('maps raw item observations, cautious trend and explicit denominators without recalculating averages', () => {
    const statistics: PassportItemStatistics = createItemStatistics();

    const view = mapItemStatisticsView(statistics, 'fr');

    expect(view.title).toBe('OzIris');
    expect(view.cards.find((card) => card.id === 'rideCoverage')).toEqual(expect.objectContaining({
      value: '67 %',
      detailParams: { rated: '2', total: '3' }
    }));
    expect(view.cards.find((card) => card.id === 'historicalAverage')?.value).toBe('4,25 / 5');
    expect(view.trend).toEqual(expect.objectContaining({
      kind: 'rising',
      deltaLabel: '+1,0'
    }));
    expect(view.timeline.map((point) => point.ratingLabel)).toEqual(['3,5 / 5', '4,5 / 5']);
    expect(view.tables[0].rows[0].navigation).toEqual(expect.objectContaining({ kind: 'visit' }));
    expect(view.tables[1].rows[0].navigation).toEqual(expect.objectContaining({ kind: 'year' }));
  });

  it('keeps the trend absent when the API deliberately withholds it', () => {
    const view = mapItemStatisticsView({
      ...createItemStatistics(),
      parkItemName: null,
      trend: null
    }, 'en');

    expect(view.title).toBe('');
    expect(view.titleKey).toBe('passport.statistics.targets.unavailableItem');
    expect(view.trend).toBeNull();
    expect(view.timeline).toHaveLength(2);
  });

  it('accepts the numeric enum representation currently emitted by the HTTP contract', () => {
    const view = mapItemStatisticsView({
      ...createItemStatistics(),
      trend: { ...createItemStatistics().trend!, kind: 2 }
    }, 'en');

    expect(view.trend?.kind).toBe('falling');
  });

  it('keeps a current global rating visible when the item has no passport history', () => {
    const statistics: PassportItemStatistics = {
      ...createItemStatistics(),
      rideCount: 0,
      visitCount: 0,
      ratingCoverage: { ratedRideCount: 0, totalRideCount: 0, rate: 0 },
      firstExperience: null,
      lastExperience: null,
      historicalRatings: null,
      currentGlobalRating: 4.5,
      currentGlobalMinusHistoricalAverage: null,
      byVisit: [],
      byYear: [],
      ratingTimeline: [],
      trend: null
    };

    const view = mapItemStatisticsView(statistics, 'fr');

    expect(view.isEmpty).toBe(false);
    expect(view.cards.find((card) => card.id === 'currentRating')?.value).toBe('4,5 / 5');
  });

  it('maps park timelines, outcomes, categories and both deliberately separate tops', () => {
    const statistics: PassportParkStatistics = {
      parkId: 'park-1',
      parkName: 'Parc Astérix',
      summary: createSummary(),
      currentGlobalRating: 5,
      currentGlobalMinusHistoricalAverage: 0.75,
      assessmentTimeline: [{
        visitId: 'visit-1',
        date: { year: 2025, month: 8, day: null, precision: 'Month', isApproximate: true },
        rating: 4
      }],
      byYear: [{ year: 2025, summary: createSummary() }],
      currentTopItems: [{ parkItemId: 'item-current', parkItemName: 'OzIris', rating: 5 }],
      historicalTopItems: [{ parkItemId: 'item-history', parkItemName: 'Tonnerre 2 Zeus', ratingCount: 3, average: 4.5 }]
    };

    const view = mapParkStatisticsView(statistics, 'fr');

    expect(view.timeline[0].dateLabel).toContain('août 2025');
    expect(view.tables.map((table) => table.id)).toEqual([
      'park-by-year', 'outcomes', 'categories', 'park-current-top', 'park-historical-top'
    ]);
    expect(view.tables[3].rows[0].navigation).toEqual(expect.objectContaining({
      kind: 'item', targetId: 'item-current'
    }));
    expect(view.tables[3].rows[0].cells[0].value).toBe('OzIris');
    expect(view.tables[4].rows[0].cells[0].value).toBe('Tonnerre 2 Zeus');
    expect(view.cards.find((card) => card.id === 'difference')?.value).toBe('+0,75');
    expect(view.tables[2].rows[0].cells[0]).toEqual({
      columnKey: 'category',
      value: 'ratings.categories.Attraction',
      translate: true
    });
  });

  it('keeps a safe literal fallback for unknown historical category values', () => {
    const summary: PassportStatisticsSummary = {
      ...createSummary(),
      categoryCoverage: [
        { ...createSummary().categoryCoverage[0], category: 'Show' },
        { ...createSummary().categoryCoverage[0], category: 'LegacyCategory' },
        { ...createSummary().categoryCoverage[0], category: null }
      ]
    };
    const statistics: PassportParkStatistics = {
      parkId: 'park-1',
      parkName: null,
      summary,
      currentGlobalRating: null,
      currentGlobalMinusHistoricalAverage: null,
      assessmentTimeline: [],
      byYear: [],
      currentTopItems: [],
      historicalTopItems: []
    };

    const categories = mapParkStatisticsView(statistics, 'fr').tables[2];

    expect(categories.rows.map((row) => row.cells[0])).toEqual([
      { columnKey: 'category', value: 'ratings.categories.Show', translate: true },
      { columnKey: 'category', value: 'LegacyCategory', translate: false },
      { columnKey: 'category', value: 'passport.statistics.categories.unknown', translate: true }
    ]);
  });

  it('uses translated neutral labels instead of technical identifiers when catalog names are unavailable', () => {
    const statistics: PassportParkStatistics = {
      parkId: '01d50831-1d2f-4928-aced-6616a2092248',
      parkName: null,
      summary: createSummary(),
      currentGlobalRating: null,
      currentGlobalMinusHistoricalAverage: null,
      assessmentTimeline: [],
      byYear: [],
      currentTopItems: [{
        parkItemId: '4a996381-4c44-454b-b129-f21a16a4e64c',
        parkItemName: null,
        rating: 5
      }],
      historicalTopItems: [{
        parkItemId: '52f6bff7-f680-4537-a253-68b4b52c69ad',
        parkItemName: null,
        ratingCount: 2,
        average: 4.5
      }]
    };

    const view = mapParkStatisticsView(statistics, 'fr');

    expect(view.title).toBe('');
    expect(view.titleKey).toBe('passport.statistics.targets.unavailablePark');
    expect(view.tables[3].rows[0].cells[0]).toEqual({
      columnKey: 'target',
      value: 'passport.statistics.targets.unavailableItem',
      translate: true
    });
    expect(view.tables[4].rows[0].cells[0]).toEqual({
      columnKey: 'target',
      value: 'passport.statistics.targets.unavailableItem',
      translate: true
    });
  });

  it('maps a yearly breakdown without manufacturing a graphical timeline', () => {
    const statistics: PassportYearStatistics = {
      year: 2025,
      parkCount: 2,
      summary: createSummary(),
      byPark: [{ parkId: 'park-1', parkName: 'Parc test', summary: createSummary() }]
    };

    const view = mapYearStatisticsView(statistics, 'en');

    expect(view.timelineTitleKey).toBeNull();
    expect(view.timeline).toEqual([]);
    expect(view.cards[0]).toEqual(expect.objectContaining({ id: 'parks', value: '2' }));
    expect(view.tables[0].rows[0].navigation).toEqual(expect.objectContaining({
      kind: 'park', targetId: 'park-1'
    }));
    expect(view.tables[0].rows[0].cells[0].value).toBe('Parc test');
  });
});

function createItemStatistics(): PassportItemStatistics {
  return {
    parkItemId: 'item-1',
    parkItemName: 'OzIris',
    rideCount: 3,
    visitCount: 2,
    ratingCoverage: { ratedRideCount: 2, totalRideCount: 3, rate: 2 / 3 },
    firstExperience: {
      visitId: 'visit-1',
      date: { year: 2024, month: null, day: null, precision: 'Year', isApproximate: false }
    },
    lastExperience: {
      visitId: 'visit-2',
      date: { year: 2025, month: 8, day: 3, precision: 'Day', isApproximate: false }
    },
    historicalRatings: {
      ratingCount: 2,
      average: 4.25,
      median: 4.25,
      minimum: 3.5,
      maximum: 5,
      populationStandardDeviation: 0.75
    },
    currentGlobalRating: 5,
    currentGlobalMinusHistoricalAverage: 0.75,
    byVisit: [{
      visitId: 'visit-1',
      date: { year: 2024, month: null, day: null, precision: 'Year', isApproximate: false },
      rideCount: 1,
      ratingCoverage: { ratedRideCount: 1, totalRideCount: 1, rate: 1 },
      historicalRatings: { ratingCount: 1, average: 3.5, median: 3.5, minimum: 3.5, maximum: 3.5, populationStandardDeviation: 0 }
    }],
    byYear: [{
      year: 2024,
      rideCount: 1,
      visitCount: 1,
      ratingCoverage: { ratedRideCount: 1, totalRideCount: 1, rate: 1 },
      historicalRatings: { ratingCount: 1, average: 3.5, median: 3.5, minimum: 3.5, maximum: 3.5, populationStandardDeviation: 0 }
    }],
    ratingTimeline: [
      {
        rideOccurrenceId: 'occurrence-1', visitId: 'visit-1',
        date: { year: 2024, month: null, day: null, precision: 'Year', isApproximate: false },
        sortPosition: 1024, rating: 3.5
      },
      {
        rideOccurrenceId: 'occurrence-2', visitId: 'visit-2',
        date: { year: 2025, month: 8, day: 3, precision: 'Day', isApproximate: false },
        sortPosition: 1024, rating: 4.5
      }
    ],
    trend: {
      kind: 'Rising', firstWindowRatingCount: 1, lastWindowRatingCount: 1,
      firstWindowAverage: 3.5, lastWindowAverage: 4.5, delta: 1
    }
  };
}

function createSummary(): PassportStatisticsSummary {
  return {
    visitCount: 2,
    approximateVisitCount: 1,
    parkRatingCoverage: { ratedCount: 1, totalCount: 2, rate: 0.5 },
    historicalParkRatings: { ratingCount: 1, average: 4.25, median: 4.25, minimum: 4.25, maximum: 4.25, populationStandardDeviation: 0 },
    firstVisit: { visitId: 'visit-1', parkId: 'park-1', date: { year: 2024, month: null, day: null, precision: 'Year', isApproximate: true } },
    lastVisit: { visitId: 'visit-2', parkId: 'park-1', date: { year: 2025, month: 8, day: 3, precision: 'Day', isApproximate: false } },
    rideOutcomes: { recordedOutcomeCount: 6, completedRideCount: 3, attemptedCount: 1, missedClosedCount: 1, missedUnavailableCount: 1, skippedByChoiceCount: 0 },
    rideRatingCoverage: { ratedCount: 2, totalCount: 3, rate: 2 / 3 },
    historicalRideRatings: { ratingCount: 2, average: 4, median: 4, minimum: 3.5, maximum: 4.5, populationStandardDeviation: 0.5 },
    distinctCompletedItemCount: 2,
    repeatedCompletedItemCount: 1,
    categoryCoverage: [{ category: 'Attraction', completedRideCount: 3, distinctItemCount: 2, historicalReferenceRideCount: 2, currentReferenceRideCount: 1, unknownReferenceRideCount: 0, completedRideRate: 1 }]
  };
}
