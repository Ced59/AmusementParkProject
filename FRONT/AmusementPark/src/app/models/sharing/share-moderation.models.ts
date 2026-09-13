export type ShareModerationTargetType =
  | 'VisitRecap'
  | 'YearRecap'
  | 'PassportProfile'
  | 'PersonalRanking'
  | 'ProfileComparison';

export type ShareModerationReason =
  | 'PersonalData'
  | 'HarassmentOrHate'
  | 'Impersonation'
  | 'InappropriateContent'
  | 'SpamOrUnsafeLink'
  | 'MisleadingContent'
  | 'Other';

export type ShareModerationReportStatus =
  | 'Pending'
  | 'Dismissed'
  | 'PublicationSuspended'
  | 'PublicationRestored';

export type ShareModerationDecision = 'Dismiss' | 'Suspend' | 'Restore';

export interface SubmitShareModerationReportRequest {
  targetType: ShareModerationTargetType;
  shareId: string;
  reason: ShareModerationReason;
  details: string | null;
}

export interface ShareModerationReport {
  reportId: string;
  targetType: ShareModerationTargetType;
  reason: ShareModerationReason;
  details: string | null;
  status: ShareModerationReportStatus;
  submittedAtUtc: string;
  reviewedAtUtc: string | null;
  decisionNote: string | null;
}

export interface ShareModerationReportQuery {
  page: number;
  size: number;
  status?: ShareModerationReportStatus;
  targetType?: ShareModerationTargetType;
  reason?: ShareModerationReason;
}

export interface ReviewShareModerationReportRequest {
  decision: ShareModerationDecision;
  note: string | null;
}
