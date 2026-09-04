import { CreatePassportRideOccurrenceBatchItem } from '@app/models/passport/passport-ride-occurrence.models';
import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';

export const PASSPORT_ANONYMOUS_DRAFT_SCHEMA_VERSION: number = 1;
export const PASSPORT_ANONYMOUS_DRAFT_MAX_RIDE_COUNT: number = 2000;

export interface PassportAnonymousRideDraft extends CreatePassportRideOccurrenceBatchItem {
  id: string;
  attractionName: string;
}

export interface PassportAnonymousDraft {
  schemaVersion: number;
  id: string;
  visitOperationId: string;
  rideOperationId: string;
  parkName: string;
  visit: CreatePassportVisitRequest;
  rides: PassportAnonymousRideDraft[];
  pendingImport?: PassportAnonymousPendingImport | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export type PassportAnonymousImportChoice = 'Separate' | 'Merge' | 'Ignore';

export type PassportAnonymousMetadataChoice = 'KeepServer' | 'UseLocal';

export interface PassportAnonymousPendingImport {
  choice: Exclude<PassportAnonymousImportChoice, 'Ignore'>;
  targetVisitId: string | null;
  metadataChoice: PassportAnonymousMetadataChoice;
  startedAtUtc: string;
}

export interface PassportAnonymousImportDecision {
  draftId: string;
  choice: PassportAnonymousImportChoice;
  targetVisitId: string | null;
  metadataChoice: PassportAnonymousMetadataChoice;
}

export interface PassportAnonymousServerRidePreview {
  id: string;
  attractionName: string;
  status: string;
  localTime: string | null;
  privateNote: string | null;
}

export interface PassportAnonymousDraftPreview {
  draft: PassportAnonymousDraft;
  similarVisits: PassportVisit[];
  selectedTarget: PassportVisit | null;
  serverRides: PassportAnonymousServerRidePreview[] | null;
  decision: PassportAnonymousImportDecision;
}

export type PassportAnonymousImportOutcome = 'Imported' | 'Merged' | 'Ignored' | 'Failed';

export interface PassportAnonymousImportReportItem {
  draftId: string;
  parkName: string;
  outcome: PassportAnonymousImportOutcome;
  serverVisitId: string | null;
  importedRideCount: number;
  errorKey: string | null;
}

export interface PassportAnonymousImportReport {
  items: PassportAnonymousImportReportItem[];
  importedVisitCount: number;
  mergedVisitCount: number;
  importedRideCount: number;
  ignoredCount: number;
  failedCount: number;
}
