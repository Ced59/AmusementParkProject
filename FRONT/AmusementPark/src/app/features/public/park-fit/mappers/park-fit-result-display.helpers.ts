import {
  ParkFitCriticalSource,
  ParkFitOpeningTimeRange
} from '@app/models/park-fit/park-fit-search.models';

type ParkFitTranslationFormatter = (
  key: string,
  params?: Record<string, string | number>
) => string;

const SCORE_STATES: readonly string[] = ['Available', 'Capped', 'Suspended'];
const CONFIDENCE_LEVELS: readonly string[] = ['Unknown', 'Low', 'Medium', 'High'];
const AVAILABILITY_STATES: readonly string[] = ['NotRequested', 'Available', 'Unavailable', 'Unknown'];
const CALENDAR_STATES: readonly string[] = [
  'OpenConfirmed',
  'ClosedConfirmed',
  'CalendarNotPublished',
  'CalendarIncomplete',
  'ExceptionalClosure',
  'OpeningHoursUnknown'
];
const DISTANCE_METHODS: readonly string[] = ['DirectGeodesic'];
const QUALITY_STATES: readonly string[] = [
  'NotAssessed',
  'Insufficient',
  'EligibleForDiscoveryOnly',
  'EligibleForFitComparison',
  'TemporarilyStale',
  'Suspended'
];
const COMPONENT_KINDS: readonly string[] = [
  'GroupCompatibility',
  'PreferenceCoverage',
  'TravelConvenience',
  'IndoorResilience',
  'BudgetFit'
];
const COMPONENT_STATES: readonly string[] = ['Known', 'Unknown', 'NotApplicable'];
const SUBSCORE_REASONS: readonly string[] = [
  'KnownFactsNormalized',
  'UnknownFactsExcluded',
  'MinimumMemberBoundApplied',
  'NoKnownFact',
  'DirectDistanceCalculated'
];
const SCORE_REASONS: readonly string[] = [
  'ScoreAvailable',
  'HardFilterFailed',
  'DateUnavailable',
  'CriticalDataExcluded',
  'CriticalDataSuspended',
  'NoKnownSubscore',
  'DataConfidenceUnknown',
  'IncompleteCoverageCapApplied',
  'DataConfidenceCapApplied',
  'CriticalUnknownCapApplied',
  'OptionalSubscoreNotApplicable'
];
const SOURCE_KINDS: readonly string[] = [
  'Official',
  'OperatorProvided',
  'VerifiedSecondary',
  'CommunityUnverified',
  'Unknown'
];

export function parkFitScoreStateKey(value: string): string {
  return enumTranslationKey('parkFit.results.scoreStates', value, SCORE_STATES);
}

export function parkFitConfidenceKey(value: string): string {
  return enumTranslationKey('parkFit.results.confidenceLevels', value, CONFIDENCE_LEVELS);
}

export function parkFitAvailabilityKey(value: string): string {
  return enumTranslationKey('parkFit.results.availabilityStates', value, AVAILABILITY_STATES);
}

export function parkFitCalendarStateKey(value: string): string {
  return enumTranslationKey('parkFit.results.calendar.states', value, CALENDAR_STATES);
}

export function parkFitDistanceMethodKey(value: string | null): string {
  return enumTranslationKey('parkFit.results.distance.methods', value ?? '', DISTANCE_METHODS);
}

export function parkFitQualityKey(value: string): string {
  return enumTranslationKey('parkFit.results.qualityStates', value, QUALITY_STATES);
}

export function parkFitComponentKindKey(value: string): string {
  return enumTranslationKey('parkFit.results.componentKinds', value, COMPONENT_KINDS);
}

export function parkFitComponentStateKey(value: string): string {
  return enumTranslationKey('parkFit.results.componentStates', value, COMPONENT_STATES);
}

export function parkFitSubscoreReasonKey(value: string): string {
  return enumTranslationKey('parkFit.results.subscoreReasons', value, SUBSCORE_REASONS);
}

export function parkFitScoreReasonKey(value: string): string {
  return enumTranslationKey('parkFit.results.scoreReasons', value, SCORE_REASONS);
}

export function parkFitSourceKindKey(value: string): string {
  return enumTranslationKey('parkFit.results.sourceKinds', value, SOURCE_KINDS);
}

export function resolveParkFitSourceSummary(
  source: ParkFitCriticalSource,
  language: string
): string | null {
  const preferredLanguages: readonly string[] = [language.toLowerCase(), 'en'];
  for (const preferredLanguage of preferredLanguages) {
    const value: string | null | undefined = source.summaries.find(
      (summary): boolean => summary.languageCode.toLowerCase() === preferredLanguage
    )?.value;
    if (value?.trim()) {
      return value.trim();
    }
  }

  return source.summaries.find((summary): boolean => Boolean(summary.value?.trim()))?.value?.trim() ?? null;
}

export function resolveParkFitSourceUrl(source: ParkFitCriticalSource): string | null {
  const value: string = source.url?.trim() ?? '';
  return /^https:\/\//i.test(value) ? value : null;
}

export function resolveParkFitHttpsUrl(value: string | null): string | null {
  const normalized: string = value?.trim() ?? '';
  return /^https:\/\//i.test(normalized) ? normalized : null;
}

export function parkFitMethodLabelKey(methodVersion: string): string {
  if (methodVersion === 'park-fit-2026-02') {
    return 'parkFit.results.method.parkFit202602';
  }

  return methodVersion === 'park-fit-2026-01'
    ? 'parkFit.results.method.parkFit202601'
    : 'parkFit.results.method.versioned';
}

export function formatParkFitOpeningTimeRange(
  range: ParkFitOpeningTimeRange,
  translate: ParkFitTranslationFormatter
): string {
  const nextDay: string = range.closesNextDay
    ? translate('parkFit.results.calendar.nextDay')
    : '';
  const hours: string = translate('parkFit.results.calendar.timeRange', {
    opensAt: range.opensAt,
    closesAt: range.closesAt,
    nextDay
  });
  if (!range.lastAdmissionAt) {
    return hours;
  }

  const lastAdmissionNextDay: string = range.lastAdmissionNextDay
    ? translate('parkFit.results.calendar.nextDay')
    : '';
  const lastAdmission: string = translate('parkFit.results.calendar.lastAdmission', {
    time: range.lastAdmissionAt,
    nextDay: lastAdmissionNextDay
  });
  return `${hours} · ${lastAdmission}`;
}

export function formatParkFitDate(value: string, language: string): string | null {
  const day: Date = new Date(value.includes('T') ? value : `${value}T12:00:00Z`);
  if (Number.isNaN(day.getTime())) {
    return null;
  }

  return new Intl.DateTimeFormat(language, {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC'
  }).format(day);
}

function enumTranslationKey(prefix: string, value: string, knownValues: readonly string[]): string {
  return knownValues.includes(value) ? `${prefix}.${value}` : `${prefix}.Unknown`;
}
