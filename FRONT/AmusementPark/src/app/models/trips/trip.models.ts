export type TripDateProposalKind = 'None' | 'Fixed' | 'Range' | 'Candidates';
export type TripParkCandidateSource = 'Manual' | 'Wishlist' | 'Comparator';
export type TripParkCandidateState = 'Proposed' | 'Shortlisted' | 'Selected' | 'Rejected';
export type TripParkCandidatePlacement = 'First' | 'Before' | 'After' | 'Last';
export type TripDayBlockType = 'Meal' | 'Event' | 'Note';
export type TripItemPreferenceLevel = 'Unknown' | 'MustDo' | 'WantToDo' | 'Optional' | 'NotForMe';
export type TripItemPreferenceReason = 'Sensations' | 'Height' | 'AlreadyDone' | 'Unavailable' | 'Other';
export type TripPreferenceCompatibility = 'Unknown' | 'Consensus' | 'Mixed' | 'Conflict';
export type TripItemDecisionStatus = 'Review' | 'Retained' | 'SplitGroup' | 'Optional' | 'Excluded';

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
  effectiveRole: 'Owner' | 'Editor' | 'Participant' | 'Viewer';
  canEditPlan: boolean;
  canEditProgram: boolean;
  canInvite: boolean;
  canChangeRoles: boolean;
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

export interface TripItemPreference {
  parkId: string;
  parkName: string;
  parkItemId: string;
  parkItemName: string;
  mainImageId: string | null;
  level: TripItemPreferenceLevel;
  reason: TripItemPreferenceReason | null;
  version: number | null;
}

export interface TripPreferenceBoard {
  tripPlanId: string;
  tripTitle: string;
  planVersion: number;
  canVote: boolean;
  items: TripItemPreference[];
}

export interface SetTripItemPreferenceRequest {
  expectedPlanVersion: number;
  expectedPreferenceVersion: number | null;
  level: TripItemPreferenceLevel;
  reason: TripItemPreferenceReason | null;
}

export interface BulkTripItemPreferenceRequest {
  parkItemId: string;
  expectedPreferenceVersion: number | null;
  level: TripItemPreferenceLevel;
  reason: TripItemPreferenceReason | null;
}

export interface BulkSetTripItemPreferencesRequest {
  expectedPlanVersion: number;
  preferences: BulkTripItemPreferenceRequest[];
}

export const TRIP_ITEM_PREFERENCE_MAX_BATCH_SIZE: number = 250;

export interface TripItemDecision {
  status: TripItemDecisionStatus;
  reason: string;
  decidedByDisplayName: string;
  decidedAtUtc: string;
  version: number;
}

export interface TripItemPreferenceSummary {
  parkId: string;
  parkName: string;
  parkItemId: string;
  parkItemName: string;
  mainImageId: string | null;
  mustDoCount: number;
  wantToDoCount: number;
  optionalCount: number;
  notForMeCount: number;
  unansweredCount: number;
  compatibility: TripPreferenceCompatibility;
  isCompatibilityKnown: boolean;
  hasIndividualConstraint: boolean;
  isGroupPriority: boolean;
  officialStatus: string | null;
  officialSourceUrl: string | null;
  officialStatusVerifiedAtUtc: string | null;
  decision: TripItemDecision | null;
}

export interface TripPreferenceSummary {
  tripPlanId: string;
  tripTitle: string;
  planVersion: number;
  participantCount: number;
  canDecide: boolean;
  items: TripItemPreferenceSummary[];
}

export interface SetTripItemDecisionRequest {
  expectedPlanVersion: number;
  expectedDecisionVersion: number | null;
  status: TripItemDecisionStatus;
  reason: string;
}

export type TripProgramCoherenceSeverity = 'Information' | 'Attention' | 'Critical';
export type TripProgramOpeningState = 'Unknown' | 'Open' | 'Closed';
export type TripProgramCoherenceCode =
  | 'DateOutsideProposal'
  | 'MultipleParksSameDate'
  | 'CandidateMissing'
  | 'CandidateNotSelected'
  | 'ParkUnavailable'
  | 'ParkNotOperating'
  | 'OpeningHoursUnknown'
  | 'OpeningHoursClosed'
  | 'OpeningHoursStale'
  | 'OpeningHoursVerifiedAfterPlanning'
  | 'AttractionUnavailable'
  | 'AttractionClosed'
  | 'NewMemberConstraint';

export interface TripProgramDayEvidence {
  localDate: string;
  parkId: string;
  parkName: string | null;
  isParkAvailable: boolean;
  parkStatus: string | null;
  openingState: TripProgramOpeningState;
  openingHoursSourceUrl: string | null;
  openingHoursVerifiedAtUtc: string | null;
  dayPlanUpdatedAtUtc: string;
}

export interface TripProgramTravelSegment {
  fromDate: string;
  fromParkId: string;
  fromParkName: string | null;
  toDate: string;
  toParkId: string;
  toParkName: string | null;
  distanceKilometers: number;
  estimatedTravelDurationMinutes: number;
  estimationMethod: 'GeodesicEstimate';
}

export interface TripProgramCoherenceIssue {
  code: TripProgramCoherenceCode;
  severity: TripProgramCoherenceSeverity;
  localDate: string | null;
  parkId: string | null;
  parkName: string | null;
  parkItemId: string | null;
  parkItemName: string | null;
  officialStatus: string | null;
  officialSourceUrl: string | null;
  officialVerifiedAtUtc: string | null;
}

export interface TripProgramCoherence {
  tripPlanId: string;
  tripTitle: string;
  planVersion: number;
  evaluatedAtUtc: string;
  criticalCount: number;
  attentionCount: number;
  informationCount: number;
  days: TripProgramDayEvidence[];
  travelSegments: TripProgramTravelSegment[];
  issues: TripProgramCoherenceIssue[];
}
