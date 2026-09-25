export type TripActivityKind =
  | 'TripCreated'
  | 'TripRenamed'
  | 'DatesChanged'
  | 'CandidateAdded'
  | 'CandidateUpdated'
  | 'CandidateMoved'
  | 'CandidateRemoved'
  | 'DayUpdated'
  | 'DayRemoved'
  | 'InvitationCreated'
  | 'InvitationRevoked'
  | 'InvitationAccepted'
  | 'InvitationDeclined'
  | 'ParticipantRoleChanged'
  | 'OwnershipTransferred'
  | 'ParticipantLeft'
  | 'PreferencesUpdated'
  | 'CollectiveDecisionUpdated'
  | 'PlanExported';

export interface TripActivityEntry {
  sequence: number;
  kind: TripActivityKind;
  actorDisplayName: string;
  isCurrentUser: boolean;
  affectedCount: number;
  occurredAtUtc: string;
}

export interface TripActivityPage {
  tripTitle: string;
  entries: TripActivityEntry[];
  nextBeforeSequence: number | null;
}
