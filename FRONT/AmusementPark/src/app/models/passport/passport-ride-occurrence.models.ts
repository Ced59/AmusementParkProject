export type PassportRideOccurrenceStatus =
  | 'Completed'
  | 'Attempted'
  | 'MissedClosed'
  | 'MissedUnavailable'
  | 'SkippedByChoice';

export type PassportRideLogSource = 'Manual' | 'Import' | 'SystemMigration';

export type PassportHistoricalConsistency = 'Verified' | 'Unverified' | 'ConfirmedConflict';

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
  privateNote: string | null;
  countsAsRide: boolean;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  target?: PassportRideOccurrenceTarget | null;
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
