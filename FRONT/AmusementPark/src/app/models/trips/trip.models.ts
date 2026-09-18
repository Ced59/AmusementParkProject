export type TripDateProposalKind = 'None' | 'Fixed' | 'Range' | 'Candidates';
export type TripParkCandidateSource = 'Manual' | 'Wishlist' | 'Comparator';
export type TripParkCandidateState = 'Proposed' | 'Shortlisted' | 'Selected' | 'Rejected';
export type TripParkCandidatePlacement = 'First' | 'Before' | 'After' | 'Last';
export type TripDayBlockType = 'Meal' | 'Event' | 'Note';

export interface TripDateProposal {
  kind: TripDateProposalKind;
  startDate: string | null;
  endDate: string | null;
  candidateDates: string[];
}

export interface TripPlan {
  tripPlanId: string;
  title: string;
  dateProposal: TripDateProposal;
  destinationTimeZoneId: string | null;
  status: string;
  accessScope: string;
  memberCount: number;
  isOwner: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}

export interface TripPlanWriteRequest {
  title: string;
  dateProposal: TripDateProposal;
  destinationTimeZoneId: string | null;
}

export interface SetTripPlanDatesRequest {
  expectedVersion: number;
  dateProposal: TripDateProposal;
  destinationTimeZoneId: string | null;
}

export interface TripFitRecommendationSnapshot {
  methodVersion: string;
  explanation: string;
  calculatedAtUtc: string;
}

export interface TripParkCandidate {
  candidateId: string;
  parkId: string;
  parkName: string | null;
  isParkAvailable: boolean;
  candidateDates: string[];
  source: TripParkCandidateSource;
  state: TripParkCandidateState;
  collectiveNote: string | null;
  fitSnapshot: TripFitRecommendationSnapshot | null;
  sortPosition: number;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TripDayBlock {
  blockId: string;
  type: TripDayBlockType;
  title: string;
  details: string | null;
  localTime: string | null;
  sortPosition: number;
}

export interface TripDayPlan {
  dayPlanId: string;
  localDate: string;
  parkCandidateId: string;
  parkId: string;
  parkName: string | null;
  isParkAvailable: boolean;
  desiredArrivalTime: string | null;
  groupNote: string | null;
  blocks: TripDayBlock[];
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TripProgram {
  candidates: TripParkCandidate[];
  days: TripDayPlan[];
}

export interface AddTripParkCandidateRequest {
  expectedPlanVersion: number;
  parkId: string;
  candidateDates: string[];
  source: TripParkCandidateSource;
  collectiveNote: string | null;
}

export interface ChangeTripParkCandidateStateRequest {
  expectedPlanVersion: number;
  expectedCandidateVersion: number;
  state: TripParkCandidateState;
}

export interface MoveTripParkCandidateRequest {
  expectedPlanVersion: number;
  anchorCandidateId: string | null;
  placement: TripParkCandidatePlacement;
}

export interface PutTripDayPlanRequest {
  expectedPlanVersion: number;
  expectedDayVersion: number | null;
  parkCandidateId: string;
  desiredArrivalTime: string | null;
  groupNote: string | null;
  blocks: TripDayBlock[];
}
