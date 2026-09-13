export type ProfileComparisonCategory =
  | 'VisitedParks'
  | 'PersonalRatings'
  | 'YearlyActivity'
  | 'MissedItems';

export type ProfileComparisonInvitationPreviewStatus =
  | 'Ready'
  | 'Accepted'
  | 'Expired'
  | 'OwnInvitation'
  | 'InviterPassportUnavailable'
  | 'InviteePassportUnavailable'
  | 'InviteeCategoriesUnavailable';

export interface ProfileComparisonInvitationCreation {
  token: string;
  expiresAtUtc: string;
  categories: ProfileComparisonCategory[];
}

export interface ProfileComparisonInvitationPreview {
  status: ProfileComparisonInvitationPreviewStatus;
  creatorDisplayName: string | null;
  inviteeDisplayName: string | null;
  expiresAtUtc: string;
  acceptedAtUtc: string | null;
  categories: ProfileComparisonCategory[];
  canAccept: boolean;
}

export interface ProfileComparisonInvitationAcceptance {
  shareId: string;
  acceptedAtUtc: string;
  categories: ProfileComparisonCategory[];
}
