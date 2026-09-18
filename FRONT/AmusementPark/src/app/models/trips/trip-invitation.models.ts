export type TripInvitationRole = 'Editor' | 'Participant' | 'Viewer';
export type TripInvitationPeriodKind = 'Unspecified' | 'SingleMonth' | 'MonthRange';
export type TripInvitationMemberCountBand = 'One' | 'TwoToFive' | 'SixToTen' | 'ElevenToFifty';

export interface CreateTripInvitationRequest {
  expectedPlanVersion: number;
  proposedRole: TripInvitationRole;
  lifetimeHours: number;
  targetEmail: string | null;
}

export interface TripInvitationCreation {
  invitationId: string;
  token: string;
  inviterDisplayName: string;
  proposedRole: TripInvitationRole;
  expiresAtUtc: string;
  isTargeted: boolean;
  wasReplayed: boolean;
}

export interface TripInvitationSummary {
  invitationId: string;
  tokenHint: string;
  proposedRole: TripInvitationRole;
  expiresAtUtc: string;
  isTargeted: boolean;
  version: number;
  createdAtUtc: string;
}

export interface TripInvitationList {
  inviterDisplayName: string;
  invitations: TripInvitationSummary[];
}

export interface TripInvitationPreview {
  tripTitle: string;
  inviterDisplayName: string;
  proposedRole: TripInvitationRole;
  periodKind: TripInvitationPeriodKind;
  startMonth: string | null;
  endMonth: string | null;
  memberCountBand: TripInvitationMemberCountBand;
  expiresAtUtc: string;
  isTargeted: boolean;
}
