import { PagedResult } from '@shared/models/contracts';

export type ParkFitDataQualityStatus =
  | 'NotAssessed'
  | 'Insufficient'
  | 'EligibleForDiscoveryOnly'
  | 'EligibleForFitComparison'
  | 'TemporarilyStale'
  | 'Suspended';

export type ParkFitDataQualityIssue =
  | 'NotPubliclyDiscoverable'
  | 'MissingCoordinates'
  | 'MissingParkType'
  | 'MissingSupportedLanguageContent'
  | 'MissingOpeningCalendar'
  | 'OpeningCalendarStale'
  | 'NoVisibleAttractions'
  | 'MissingPreciseAttractionType'
  | 'MissingIndoorOutdoorClassification'
  | 'MissingAccessibilitySource'
  | 'MissingAccessConditions'
  | 'MissingAuthoritativeSource'
  | 'MissingEvidenceTimestamp'
  | 'StaleEvidence'
  | 'AmbiguousRestriction'
  | 'IncompleteRestrictionCoverage';

export interface ParkFitDataQualityItem {
  parkItemId: string;
  parkItemName: string;
  issues: ParkFitDataQualityIssue[];
}

export interface ParkFitDataQuality {
  parkId: string;
  parkName: string;
  status: ParkFitDataQualityStatus;
  coveragePercent: number;
  visibleAttractionCount: number;
  attractionWithConditionsCount: number;
  decisionEligibleAttractionCount: number;
  conditionCount: number;
  decisionEligibleConditionCount: number;
  issueItemCount: number;
  missingSourceItemCount: number;
  missingTimestampItemCount: number;
  staleEvidenceItemCount: number;
  ambiguousItemCount: number;
  lastVerifiedAtUtc?: string | null;
  issues: ParkFitDataQualityIssue[];
  issueSamples: ParkFitDataQualityItem[];
}

export type ParkFitDataQualityPage = PagedResult<ParkFitDataQuality>;
