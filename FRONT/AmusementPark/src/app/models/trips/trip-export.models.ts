import {
  TripDateProposal,
  TripDayBlockType,
  TripItemDecisionStatus,
  TripParkCandidateState
} from './trip.models';

export interface TripExportCandidate {
  parkName: string | null;
  isParkAvailable: boolean;
  candidateDates: string[];
  state: TripParkCandidateState;
  collectiveNote: string | null;
}

export interface TripExportDayBlock {
  type: TripDayBlockType;
  title: string;
  details: string | null;
  localTime: string | null;
}

export interface TripExportDay {
  localDate: string;
  parkName: string | null;
  isParkAvailable: boolean;
  desiredArrivalTime: string | null;
  groupNote: string | null;
  blocks: TripExportDayBlock[];
}

export interface TripExportDecision {
  parkName: string | null;
  parkItemName: string | null;
  isParkItemAvailable: boolean;
  status: TripItemDecisionStatus;
  reason: string;
  decidedAtUtc: string;
}

export interface TripExport {
  schemaVersion: string;
  title: string;
  dateProposal: TripDateProposal;
  destinationTimeZoneId: string | null;
  status: string;
  generatedAtUtc: string;
  candidateParks: TripExportCandidate[];
  days: TripExportDay[];
  collectiveDecisions: TripExportDecision[];
}
