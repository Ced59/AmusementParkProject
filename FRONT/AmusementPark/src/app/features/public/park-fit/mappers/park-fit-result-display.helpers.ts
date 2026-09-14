import { ParkFitCriticalSource } from '@app/models/park-fit/park-fit-search.models';

const SCORE_STATES: readonly string[] = ['Available', 'Capped', 'Suspended'];
const CONFIDENCE_LEVELS: readonly string[] = ['Unknown', 'Low', 'Medium', 'High'];
const AVAILABILITY_STATES: readonly string[] = ['NotRequested', 'Available', 'Unavailable', 'Unknown'];
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

export function parkFitQualityKey(value: string): string {
  return enumTranslationKey('parkFit.results.qualityStates', value, QUALITY_STATES);
}

export function parkFitComponentKindKey(value: string): string {
  return enumTranslationKey('parkFit.results.componentKinds', value, COMPONENT_KINDS);
}

export function parkFitComponentStateKey(value: string): string {
  return enumTranslationKey('parkFit.results.componentStates', value, COMPONENT_STATES);
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

export function parkFitMethodLabelKey(methodVersion: string): string {
  return methodVersion === 'park-fit-2026-01'
    ? 'parkFit.results.method.parkFit202601'
    : 'parkFit.results.method.versioned';
}

function enumTranslationKey(prefix: string, value: string, knownValues: readonly string[]): string {
  return knownValues.includes(value) ? `${prefix}.${value}` : `${prefix}.Unknown`;
}
