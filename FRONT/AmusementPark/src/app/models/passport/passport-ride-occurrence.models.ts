export type PassportRideOccurrenceStatus =
  | 'Completed'
  | 'Attempted'
  | 'MissedClosed'
  | 'MissedUnavailable'
  | 'SkippedByChoice';

export type PassportRideLogSource = 'Manual' | 'Import' | 'SystemMigration' | 'TripTransition';

export type PassportHistoricalConsistency = 'Verified' | 'Unverified' | 'ConfirmedConflict';

export type PassportHistoricalOperationalState = 'KnownOpen' | 'KnownClosed' | 'PossiblyOpen' | 'Unknown';

export type PassportHistoricalRideTargetScope = 'KnownOpen' | 'PossiblyOpen' | 'AllHistory';

export type PassportRideOccurrencePlacement = 'First' | 'Last' | 'Before' | 'After';

export interface PassportRideOccurrenceMoment {
  localTime: string | null;
  isApproximate: boolean;
}

export interface PassportRideOccurrenceTarget {
  name: string;
  category: string | null;
  lifecycleStatus: string | null;
  isHistoricalSnapshot: boolean;
  openingDate?: string | null;
  closingDate?: string | null;
}

export interface PassportVisitRideTargetEvaluation {
  parkItemId: string;
  name?: string;
  category?: string;
  operationalState?: PassportHistoricalOperationalState;
  historicalConsistency: PassportHistoricalConsistency;
  isHistoricalOnly?: boolean;
  mainImageId?: string | null;
  zoneId?: string | null;
  lifecycleStatus?: string | null;
  openingDate: string | null;
  closingDate: string | null;
}

export interface PassportHistoricalRideTargetPage {
  items: PassportVisitRideTargetEvaluation[];
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  knownOpenCount: number;
  possiblyOpenCount: number;
  allHistoryCount: number;
  coverageStatus: 'Partial' | 'Substantial' | 'HighConfidence';
  coveragePercent: number;
  methodologyVersion: string;
}

export interface PassportRideAssessment {
  value: number;
  privateComment: string | null;
  revision: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PassportRideAssessmentDraft {
  value: number | null;
  privateComment: string;
}

export interface UpsertPassportRideAssessmentRequest {
  value: number;
  privateComment: string | null;
  expectedVersion: number;
}

export interface CreatePassportRideOccurrenceBatchItem {
  parkItemId: string;
  moment: PassportRideOccurrenceMoment;
  status: PassportRideOccurrenceStatus;
  privateNote: string | null;
  confirmHistoricalConflict: boolean;
  count: number;
}

export interface CreatePassportRideOccurrencesBatchRequest {
  items: CreatePassportRideOccurrenceBatchItem[];
}

export interface UpdatePassportRideOccurrenceRequest {
  expectedVersion: number;
  moment: PassportRideOccurrenceMoment;
  status: PassportRideOccurrenceStatus;
  privateNote: string | null;
  confirmHistoricalConflict: boolean;
}

export interface ReorderPassportRideOccurrenceRequest {
  occurrenceId: string;
  expectedVersion: number;
  anchorOccurrenceId: string | null;
  placement: PassportRideOccurrencePlacement;
}

export interface PassportRideOccurrence {
  id: string;
  visitId: string;
  parkId: string;
  parkItemId: string;
  sortPosition: number;
  moment: PassportRideOccurrenceMoment;
  status: PassportRideOccurrenceStatus;
  source: PassportRideLogSource;
  historicalConsistency: PassportHistoricalConsistency;
  historicalConflictConfirmed?: boolean;
  privateNote: string | null;
  countsAsRide: boolean;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  target?: PassportRideOccurrenceTarget | null;
  assessment?: PassportRideAssessment | null;
}

export interface PassportRideOccurrencePage {
  items: PassportRideOccurrence[];
  nextCursor: string | null;
}

export interface PassportRideOccurrenceMutationResult {
  occurrences: PassportRideOccurrence[];
  wasReplayed: boolean;
  wasOrderNormalized: boolean;
}
