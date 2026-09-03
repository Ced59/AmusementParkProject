import {
  PassportHistoricalConsistency,
  PassportRideOccurrence,
  PassportRideOccurrenceStatus
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisitDatePrecision } from '@app/models/passport/passport-visit.models';

export interface PassportVisitEditorAttraction {
  id: string;
  name: string;
  zoneId: string | null;
  lifecycleStatus: string | null;
  isHistorical: boolean;
}

export interface PassportVisitEditorZone {
  id: string;
  name: string;
}

export interface PassportVisitParkAssessmentDraft {
  value: number | null;
  privateComment: string;
}

export interface PassportVisitMetadataDraft {
  precision: PassportVisitDatePrecision;
  year: number | null;
  month: number | null;
  day: number | null;
  isApproximate: boolean;
  timeZoneId: string;
  title: string;
  privateNote: string;
}

export interface PassportAttractionSelectionDraft {
  parkItemId: string;
  attractionName: string;
  status: PassportRideOccurrenceStatus;
  count: number;
  localTime: string;
  isApproximate: boolean;
  privateNote: string;
  confirmHistoricalConflict: boolean;
}

export interface PassportOccurrenceEditDraft {
  status: PassportRideOccurrenceStatus;
  localTime: string;
  isApproximate: boolean;
  privateNote: string;
  confirmHistoricalConflict: boolean;
}

export interface PassportRideOccurrenceRow {
  occurrence: PassportRideOccurrence;
  attractionName: string;
  occurrenceNumber: number;
  occurrenceCount: number;
  historicalConsistency: PassportHistoricalConsistency;
}

export interface PassportAttractionSelectionPatch {
  status?: PassportRideOccurrenceStatus;
  count?: number;
  localTime?: string;
  isApproximate?: boolean;
  privateNote?: string;
  confirmHistoricalConflict?: boolean;
}
