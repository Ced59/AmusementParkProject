export type TripEffectiveRole = 'Owner' | 'Editor' | 'Participant' | 'Viewer';
export type TripDelegatedRole = Exclude<TripEffectiveRole, 'Owner'>;

export interface TripParticipant {
  memberId: string;
  displayName: string;
  role: TripEffectiveRole;
  isCurrentUser: boolean;
  joinedAtUtc: string;
}

export interface TripParticipantList {
  participants: TripParticipant[];
  canManageRoles: boolean;
  canTransferOwnership: boolean;
  canLeave: boolean;
  tripVersion: number;
}
