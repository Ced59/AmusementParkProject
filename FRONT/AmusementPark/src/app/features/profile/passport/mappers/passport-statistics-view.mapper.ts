import {
  PassportCategoryCoverage,
  PassportItemStatistics,
  PassportParkStatistics,
  PassportRatingDistribution,
  PassportStatisticsSummary,
  PassportYearStatistics
} from '@app/models/passport/passport-statistics.models';
import { PassportVisitDate } from '@app/models/passport/passport-visit.models';
import {
  PassportStatisticCardViewModel,
  PassportStatisticsTableRowViewModel,
  PassportStatisticsTableViewModel,
  PassportStatisticsTimelinePointViewModel,
  PassportStatisticsTrendViewModel,
  PassportStatisticsViewModel
} from '../models/passport-statistics-view.models';

const emptyValue: string = '—';

export function mapItemStatisticsView(
  statistics: PassportItemStatistics,
  targetName: string | null,
  language: string
): PassportStatisticsViewModel {
  const number: Intl.NumberFormat = createNumberFormatter(language);
  const rating: Intl.NumberFormat = createRatingFormatter(language);
  const percent: Intl.NumberFormat = createPercentFormatter(language);
  const byVisit: PassportStatisticsTableRowViewModel[] = statistics.byVisit.map((row) => ({
    id: row.visitId,
    cells: [
      { columnKey: 'date', value: formatVisitDate(row.date, language) },
      { columnKey: 'rides', value: number.format(row.rideCount) },
      { columnKey: 'coverage', value: formatCoverage(row.ratingCoverage.ratedRideCount, row.ratingCoverage.totalRideCount, row.ratingCoverage.rate, number, percent) },
      { columnKey: 'average', value: formatDistributionAverage(row.historicalRatings, rating) }
    ],
    navigation: { kind: 'visit', targetId: row.visitId, labelKey: 'passport.statistics.actions.openVisit' }
  }));
  const byYear: PassportStatisticsTableRowViewModel[] = statistics.byYear.map((row) => ({
    id: String(row.year),
    cells: [
      { columnKey: 'year', value: number.format(row.year) },
      { columnKey: 'visits', value: number.format(row.visitCount) },
      { columnKey: 'rides', value: number.format(row.rideCount) },
      { columnKey: 'coverage', value: formatCoverage(row.ratingCoverage.ratedRideCount, row.ratingCoverage.totalRideCount, row.ratingCoverage.rate, number, percent) },
      { columnKey: 'average', value: formatDistributionAverage(row.historicalRatings, rating) }
    ],
    navigation: { kind: 'year', targetId: String(row.year), labelKey: 'passport.statistics.actions.openYear' }
  }));
  const timeline: PassportStatisticsTimelinePointViewModel[] = statistics.ratingTimeline.map((point) => ({
    id: point.rideOccurrenceId,
    visitId: point.visitId,
    dateLabel: formatVisitDate(point.date, language),
    ratingLabel: `${rating.format(point.rating)} / 5`,
    positionLabel: number.format(point.sortPosition)
  }));

  return {
    scope: { kind: 'item', targetId: statistics.parkItemId },
    title: targetName?.trim() || statistics.parkItemId,
    subtitleKey: 'passport.statistics.item.subtitle',
    cards: [
      card('rides', 'pi pi-replay', 'passport.statistics.cards.rides', number.format(statistics.rideCount)),
      card('visits', 'pi pi-calendar', 'passport.statistics.cards.visits', number.format(statistics.visitCount)),
      card('firstExperience', 'pi pi-step-backward', 'passport.statistics.cards.firstExperience', formatItemExperience(statistics.firstExperience, language)),
      card('lastExperience', 'pi pi-step-forward', 'passport.statistics.cards.lastExperience', formatItemExperience(statistics.lastExperience, language)),
      card(
        'rideCoverage',
        'pi pi-check-circle',
        'passport.statistics.cards.ratedRides',
        percent.format(statistics.ratingCoverage.rate),
        'passport.statistics.cards.coverageDetail',
        { rated: number.format(statistics.ratingCoverage.ratedRideCount), total: number.format(statistics.ratingCoverage.totalRideCount) }
      ),
      card('historicalAverage', 'pi pi-chart-line', 'passport.statistics.cards.historicalAverage', formatDistributionAverage(statistics.historicalRatings, rating), distributionDetailKey(statistics.historicalRatings), distributionDetailParams(statistics.historicalRatings, rating, number)),
      card('currentRating', 'pi pi-star', 'passport.statistics.cards.currentGlobalRating', formatNullableRating(statistics.currentGlobalRating, rating), 'passport.statistics.cards.communitySeparation'),
      card('difference', 'pi pi-arrows-h', 'passport.statistics.cards.globalDifference', formatSignedRating(statistics.currentGlobalMinusHistoricalAverage, rating), 'passport.statistics.cards.differenceDetail')
    ],
    timelineTitleKey: 'passport.statistics.item.timelineTitle',
    timelineDescriptionKey: 'passport.statistics.item.timelineDescription',
    timeline,
    trend: mapTrend(statistics.trend, rating),
    tables: [
      table('item-by-visit', 'passport.statistics.item.byVisitTitle', 'passport.statistics.item.byVisitDescription', ['date', 'rides', 'coverage', 'average'], byVisit),
      table('item-by-year', 'passport.statistics.item.byYearTitle', 'passport.statistics.item.byYearDescription', ['year', 'visits', 'rides', 'coverage', 'average'], byYear)
    ],
    isEmpty: statistics.rideCount === 0 && statistics.visitCount === 0
  };
}

export function mapParkStatisticsView(
  statistics: PassportParkStatistics,
  targetName: string | null,
  language: string
): PassportStatisticsViewModel {
  const number: Intl.NumberFormat = createNumberFormatter(language);
  const rating: Intl.NumberFormat = createRatingFormatter(language);
  const percent: Intl.NumberFormat = createPercentFormatter(language);
  const timeline: PassportStatisticsTimelinePointViewModel[] = statistics.assessmentTimeline.map((point) => ({
    id: point.visitId,
    visitId: point.visitId,
    dateLabel: formatVisitDate(point.date, language),
    ratingLabel: `${rating.format(point.rating)} / 5`,
    positionLabel: null
  }));
  const byYear: PassportStatisticsTableRowViewModel[] = statistics.byYear.map((row) => ({
    id: String(row.year),
    cells: summaryCells(row.summary, number, rating, [
      { columnKey: 'year', value: number.format(row.year) }
    ]),
    navigation: { kind: 'year', targetId: String(row.year), labelKey: 'passport.statistics.actions.openYear' }
  }));
  const currentTop: PassportStatisticsTableRowViewModel[] = statistics.currentTopItems.map((row) => ({
    id: row.parkItemId,
    cells: [
      { columnKey: 'target', value: row.parkItemId },
      { columnKey: 'rating', value: `${rating.format(row.rating)} / 5` }
    ],
    navigation: { kind: 'item', targetId: row.parkItemId, labelKey: 'passport.statistics.actions.openItem' }
  }));
  const historicalTop: PassportStatisticsTableRowViewModel[] = statistics.historicalTopItems.map((row) => ({
    id: row.parkItemId,
    cells: [
      { columnKey: 'target', value: row.parkItemId },
      { columnKey: 'ratings', value: number.format(row.ratingCount) },
      { columnKey: 'average', value: `${rating.format(row.average)} / 5` }
    ],
    navigation: { kind: 'item', targetId: row.parkItemId, labelKey: 'passport.statistics.actions.openItem' }
  }));

  return {
    scope: { kind: 'park', targetId: statistics.parkId },
    title: targetName?.trim() || statistics.parkId,
    subtitleKey: 'passport.statistics.park.subtitle',
    cards: mapSummaryCards(statistics.summary, number, rating, percent, language, [
      card('currentRating', 'pi pi-star', 'passport.statistics.cards.currentGlobalRating', formatNullableRating(statistics.currentGlobalRating, rating), 'passport.statistics.cards.communitySeparation'),
      card('difference', 'pi pi-arrows-h', 'passport.statistics.cards.globalDifference', formatSignedRating(statistics.currentGlobalMinusHistoricalAverage, rating), 'passport.statistics.cards.differenceDetail')
    ]),
    timelineTitleKey: 'passport.statistics.park.timelineTitle',
    timelineDescriptionKey: 'passport.statistics.park.timelineDescription',
    timeline,
    trend: null,
    tables: [
      table('park-by-year', 'passport.statistics.park.byYearTitle', 'passport.statistics.park.byYearDescription', ['year', 'visits', 'rides', 'parkAverage', 'rideAverage'], byYear),
      mapOutcomeTable(statistics.summary, number),
      mapCategoryTable(statistics.summary.categoryCoverage, number, percent),
      table('park-current-top', 'passport.statistics.park.currentTopTitle', 'passport.statistics.park.currentTopDescription', ['target', 'rating'], currentTop),
      table('park-historical-top', 'passport.statistics.park.historicalTopTitle', 'passport.statistics.park.historicalTopDescription', ['target', 'ratings', 'average'], historicalTop)
    ],
    isEmpty: statistics.summary.visitCount === 0
  };
}

export function mapYearStatisticsView(
  statistics: PassportYearStatistics,
  language: string
): PassportStatisticsViewModel {
  const number: Intl.NumberFormat = createNumberFormatter(language);
  const rating: Intl.NumberFormat = createRatingFormatter(language);
  const percent: Intl.NumberFormat = createPercentFormatter(language);
  const byPark: PassportStatisticsTableRowViewModel[] = statistics.byPark.map((row) => ({
    id: row.parkId,
    cells: summaryCells(row.summary, number, rating, [
      { columnKey: 'target', value: row.parkId }
    ]),
    navigation: { kind: 'park', targetId: row.parkId, labelKey: 'passport.statistics.actions.openPark' }
  }));

  return {
    scope: { kind: 'year', targetId: String(statistics.year) },
    title: number.format(statistics.year),
    subtitleKey: 'passport.statistics.year.subtitle',
    cards: [
      card('parks', 'pi pi-map', 'passport.statistics.cards.parks', number.format(statistics.parkCount)),
      ...mapSummaryCards(statistics.summary, number, rating, percent, language)
    ],
    timelineTitleKey: null,
    timelineDescriptionKey: null,
    timeline: [],
    trend: null,
    tables: [
      table('year-by-park', 'passport.statistics.year.byParkTitle', 'passport.statistics.year.byParkDescription', ['target', 'visits', 'rides', 'parkAverage', 'rideAverage'], byPark),
      mapOutcomeTable(statistics.summary, number),
      mapCategoryTable(statistics.summary.categoryCoverage, number, percent)
    ],
    isEmpty: statistics.summary.visitCount === 0
  };
}

function mapSummaryCards(
  summary: PassportStatisticsSummary,
  number: Intl.NumberFormat,
  rating: Intl.NumberFormat,
  percent: Intl.NumberFormat,
  language: string,
  additionalCards: PassportStatisticCardViewModel[] = []
): PassportStatisticCardViewModel[] {
  return [
    card('visits', 'pi pi-calendar', 'passport.statistics.cards.visits', number.format(summary.visitCount)),
    card('approximateVisits', 'pi pi-question-circle', 'passport.statistics.cards.approximateVisits', number.format(summary.approximateVisitCount)),
    card('completedRides', 'pi pi-replay', 'passport.statistics.cards.completedRides', number.format(summary.rideOutcomes.completedRideCount)),
    card('distinctItems', 'pi pi-compass', 'passport.statistics.cards.distinctItems', number.format(summary.distinctCompletedItemCount), 'passport.statistics.cards.repeatedDetail', { count: number.format(summary.repeatedCompletedItemCount) }),
    card('firstVisit', 'pi pi-step-backward', 'passport.statistics.cards.firstVisit', formatVisitExperience(summary.firstVisit, language)),
    card('lastVisit', 'pi pi-step-forward', 'passport.statistics.cards.lastVisit', formatVisitExperience(summary.lastVisit, language)),
    card(
      'parkAverage',
      'pi pi-building',
      'passport.statistics.cards.parkAverage',
      formatDistributionAverage(summary.historicalParkRatings, rating),
      summary.historicalParkRatings ? 'passport.statistics.cards.distributionCoverageDetail' : 'passport.statistics.cards.coveragePercentDetail',
      summaryDistributionParams(summary.historicalParkRatings, summary.parkRatingCoverage.ratedCount, summary.parkRatingCoverage.totalCount, summary.parkRatingCoverage.rate, rating, number, percent)
    ),
    card(
      'rideAverage',
      'pi pi-chart-line',
      'passport.statistics.cards.rideAverage',
      formatDistributionAverage(summary.historicalRideRatings, rating),
      summary.historicalRideRatings ? 'passport.statistics.cards.distributionCoverageDetail' : 'passport.statistics.cards.coveragePercentDetail',
      summaryDistributionParams(summary.historicalRideRatings, summary.rideRatingCoverage.ratedCount, summary.rideRatingCoverage.totalCount, summary.rideRatingCoverage.rate, rating, number, percent)
    ),
    ...additionalCards
  ];
}

function mapTrend(
  trend: PassportItemStatistics['trend'],
  rating: Intl.NumberFormat
): PassportStatisticsTrendViewModel | null {
  if (!trend) {
    return null;
  }

  const kind: PassportStatisticsTrendViewModel['kind'] = trend.kind === 'Rising' || trend.kind === 1
    ? 'rising'
    : trend.kind === 'Falling' || trend.kind === 2
      ? 'falling'
      : 'stable';
  return {
    kind,
    labelKey: `passport.statistics.trend.${kind}`,
    deltaLabel: formatSignedRating(trend.delta, rating),
    firstAverageLabel: `${rating.format(trend.firstWindowAverage)} / 5`,
    lastAverageLabel: `${rating.format(trend.lastWindowAverage)} / 5`,
    firstCount: trend.firstWindowRatingCount,
    lastCount: trend.lastWindowRatingCount
  };
}

function mapOutcomeTable(
  summary: PassportStatisticsSummary,
  number: Intl.NumberFormat
): PassportStatisticsTableViewModel {
  const outcomes: Array<[string, number]> = [
    ['completed', summary.rideOutcomes.completedRideCount],
    ['attempted', summary.rideOutcomes.attemptedCount],
    ['missedClosed', summary.rideOutcomes.missedClosedCount],
    ['missedUnavailable', summary.rideOutcomes.missedUnavailableCount],
    ['skippedByChoice', summary.rideOutcomes.skippedByChoiceCount]
  ];
  const rows: PassportStatisticsTableRowViewModel[] = outcomes.map(([key, value]) => ({
    id: key,
    cells: [
      { columnKey: 'outcome', value: `passport.statistics.outcomes.${key}`, translate: true },
      { columnKey: 'count', value: number.format(value) }
    ],
    navigation: null
  }));
  return table('outcomes', 'passport.statistics.outcomes.title', 'passport.statistics.outcomes.description', ['outcome', 'count'], rows);
}

function mapCategoryTable(
  categories: PassportCategoryCoverage[],
  number: Intl.NumberFormat,
  percent: Intl.NumberFormat
): PassportStatisticsTableViewModel {
  const rows: PassportStatisticsTableRowViewModel[] = categories.map((category, index) => ({
    id: `${category.category ?? 'unknown'}-${index}`,
    cells: [
      {
        columnKey: 'category',
        value: category.category || 'passport.statistics.categories.unknown',
        translate: category.category == null
      },
      { columnKey: 'rides', value: number.format(category.completedRideCount) },
      { columnKey: 'distinct', value: number.format(category.distinctItemCount) },
      { columnKey: 'coverage', value: percent.format(category.completedRideRate) },
      { columnKey: 'reference', value: `${number.format(category.historicalReferenceRideCount)} / ${number.format(category.currentReferenceRideCount)} / ${number.format(category.unknownReferenceRideCount)}` }
    ],
    navigation: null
  }));
  return table('categories', 'passport.statistics.categories.title', 'passport.statistics.categories.description', ['category', 'rides', 'distinct', 'coverage', 'reference'], rows);
}

function summaryCells(
  summary: PassportStatisticsSummary,
  number: Intl.NumberFormat,
  rating: Intl.NumberFormat,
  prefix: PassportStatisticsTableRowViewModel['cells']
): PassportStatisticsTableRowViewModel['cells'] {
  return [
    ...prefix,
    { columnKey: 'visits', value: number.format(summary.visitCount) },
    { columnKey: 'rides', value: number.format(summary.rideOutcomes.completedRideCount) },
    { columnKey: 'parkAverage', value: formatDistributionAverage(summary.historicalParkRatings, rating) },
    { columnKey: 'rideAverage', value: formatDistributionAverage(summary.historicalRideRatings, rating) }
  ];
}

function table(
  id: string,
  titleKey: string,
  descriptionKey: string,
  columnKeys: string[],
  rows: PassportStatisticsTableRowViewModel[]
): PassportStatisticsTableViewModel {
  return {
    id,
    titleKey,
    descriptionKey,
    emptyKey: 'passport.statistics.tables.empty',
    columns: columnKeys.map((key) => ({ key, labelKey: `passport.statistics.columns.${key}` })),
    rows
  };
}

function card(
  id: string,
  iconClass: string,
  labelKey: string,
  value: string,
  detailKey: string | null = null,
  detailParams: Readonly<Record<string, string | number>> = {}
): PassportStatisticCardViewModel {
  return { id, iconClass, labelKey, value, detailKey, detailParams };
}

function distributionDetailKey(distribution: PassportRatingDistribution | null): string | null {
  return distribution ? 'passport.statistics.cards.distributionDetail' : null;
}

function distributionDetailParams(
  distribution: PassportRatingDistribution | null,
  rating: Intl.NumberFormat,
  number: Intl.NumberFormat
): Readonly<Record<string, string | number>> {
  return distribution
    ? {
        count: number.format(distribution.ratingCount),
        median: rating.format(distribution.median),
        minimum: rating.format(distribution.minimum),
        maximum: rating.format(distribution.maximum),
        deviation: rating.format(distribution.populationStandardDeviation)
      }
    : {};
}

function summaryDistributionParams(
  distribution: PassportRatingDistribution | null,
  rated: number,
  total: number,
  rate: number,
  rating: Intl.NumberFormat,
  number: Intl.NumberFormat,
  percent: Intl.NumberFormat
): Readonly<Record<string, string | number>> {
  return {
    ...distributionDetailParams(distribution, rating, number),
    rated: number.format(rated),
    total: number.format(total),
    percent: percent.format(rate)
  };
}

function formatDistributionAverage(
  distribution: PassportRatingDistribution | null,
  rating: Intl.NumberFormat
): string {
  return distribution ? `${rating.format(distribution.average)} / 5` : emptyValue;
}

function formatNullableRating(value: number | null, rating: Intl.NumberFormat): string {
  return value === null ? emptyValue : `${rating.format(value)} / 5`;
}

function formatSignedRating(value: number | null, rating: Intl.NumberFormat): string {
  if (value === null) {
    return emptyValue;
  }

  const prefix: string = value > 0 ? '+' : '';
  return `${prefix}${rating.format(value)}`;
}

function formatCoverage(
  rated: number,
  total: number,
  rate: number,
  number: Intl.NumberFormat,
  percent: Intl.NumberFormat
): string {
  return `${number.format(rated)} / ${number.format(total)} · ${percent.format(rate)}`;
}

function formatItemExperience(
  experience: PassportItemStatistics['firstExperience'],
  language: string
): string {
  return experience ? formatVisitDate(experience.date, language) : emptyValue;
}

function formatVisitExperience(
  experience: PassportStatisticsSummary['firstVisit'],
  language: string
): string {
  return experience ? formatVisitDate(experience.date, language) : emptyValue;
}

function formatVisitDate(date: PassportVisitDate, language: string): string {
  const value: Date = new Date(Date.UTC(date.year, (date.month ?? 1) - 1, date.day ?? 1));
  const options: Intl.DateTimeFormatOptions = date.precision === 'Year'
    ? { year: 'numeric', timeZone: 'UTC' }
    : date.precision === 'Month'
      ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
      : { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' };
  const label: string = new Intl.DateTimeFormat(language, options).format(value);
  return date.isApproximate ? `≈ ${label}` : label;
}

function createNumberFormatter(language: string): Intl.NumberFormat {
  return new Intl.NumberFormat(language, { maximumFractionDigits: 0 });
}

function createRatingFormatter(language: string): Intl.NumberFormat {
  return new Intl.NumberFormat(language, { minimumFractionDigits: 1, maximumFractionDigits: 2 });
}

function createPercentFormatter(language: string): Intl.NumberFormat {
  return new Intl.NumberFormat(language, { style: 'percent', maximumFractionDigits: 0 });
}
