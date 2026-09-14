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

export type ParkFitRecommendationState = 'Active' | 'Suspended';

export interface ParkFitOperationalDecision {
  type: 'Suspended' | 'Restored';
  reason: string;
  decidedAtUtc: string;
  revision: number;
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
  recommendationState: ParkFitRecommendationState;
  operationalRevision: number;
  operationalUpdatedAtUtc?: string | null;
  pendingReportCount: number;
  recentDecisions: ParkFitOperationalDecision[];
}

export type ParkFitDataQualityPage = PagedResult<ParkFitDataQuality>;

export type ParkFitSourceReportStatus = 'Pending' | 'Resolved' | 'Dismissed';
export type ParkFitSourceReportReason = 'Outdated' | 'Incorrect' | 'Unavailable' | 'Incomplete' | 'Other';
export type ParkFitEvidenceKind = 'GeneralParkData' | 'AccessCondition' | 'OpeningCalendar';

export interface ParkFitSourceReport {
  reportId: string;
  parkId: string;
  parkName: string;
  evidenceKind: ParkFitEvidenceKind;
  sourceUrl?: string | null;
  sourceReference?: string | null;
  reason: ParkFitSourceReportReason;
  details?: string | null;
  status: ParkFitSourceReportStatus;
  submittedAtUtc: string;
  reviewedAtUtc?: string | null;
  decisionNote?: string | null;
  revision: number;
}

export type ParkFitSourceReportPage = PagedResult<ParkFitSourceReport>;

export interface ParkFitOperationsSnapshot {
  quality: ParkFitDataQualityPage;
  reports: ParkFitSourceReportPage;
}

export interface ParkFitSourceReportReviewRequest {
  decision: 'Resolved' | 'Dismissed';
  decisionNote: string | null;
  expectedRevision: number;
}

export interface ParkFitOperationalStatusRequest {
  targetState: ParkFitRecommendationState;
  reason: string;
  expectedRevision: number;
}
