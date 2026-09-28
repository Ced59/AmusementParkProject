export type LiveTargetType = 'Park' | 'ParkItem';

export type LiveMappingStatus =
  | 'Candidate'
  | 'Verified'
  | 'Suspended'
  | 'Superseded'
  | 'Rejected';

export type LiveMappingConfidence = 'Low' | 'Medium' | 'High';

export type LiveTargetMappingDecision =
  | 'Verify'
  | 'Correct'
  | 'Suspend'
  | 'Supersede'
  | 'Reject';

export interface LiveExternalTarget {
  readonly type: LiveTargetType;
  readonly id: string;
  readonly parentId: string | null;
  readonly displayName: string;
  readonly parentDisplayName: string | null;
  readonly countryCode: string;
}

export interface LiveInternalTarget {
  readonly type: LiveTargetType;
  readonly id: string;
  readonly parkId: string;
  readonly displayName: string;
  readonly parkDisplayName: string;
  readonly countryCode: string;
}

export interface LiveTargetMapping {
  readonly mappingId: string;
  readonly version: string;
  readonly sourceId: string;
  readonly externalTarget: LiveExternalTarget;
  readonly target: LiveInternalTarget | null;
  readonly status: LiveMappingStatus;
  readonly confidence: LiveMappingConfidence;
  readonly validFromUtc: string;
  readonly validToUtc: string | null;
  readonly revision: number;
  readonly supersedesRevision: number | null;
  readonly reviewedByUserId: string | null;
  readonly reviewNote: string | null;
  readonly recordedAtUtc: string;
  readonly isEligibleForLiveUse: boolean;
}

export interface LiveTargetMappingQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly sourceId?: string | null;
  readonly status?: LiveMappingStatus | null;
  readonly targetType?: LiveTargetType | null;
  readonly search?: string | null;
}

export interface CreateLiveTargetMappingCandidateRequest {
  readonly sourceId: string;
  readonly targetType: LiveTargetType;
  readonly externalTargetId: string;
  readonly externalParentTargetId: string | null;
  readonly externalDisplayName: string;
  readonly externalParentDisplayName: string | null;
  readonly externalCountryCode: string;
  readonly suggestedInternalTargetId: string | null;
  readonly suggestedParkId: string | null;
}

export interface ReviewLiveTargetMappingRequest {
  readonly expectedRevision: number;
  readonly decision: LiveTargetMappingDecision;
  readonly internalTargetId: string | null;
  readonly parkId: string | null;
  readonly reviewNote: string | null;
}

export interface LiveTargetMappingPage {
  readonly data: readonly LiveTargetMapping[];
  readonly pagination: {
    readonly totalItems: number | null;
    readonly totalPages: number | null;
    readonly currentPage: number | null;
    readonly itemsPerPage: number | null;
  } | null;
}
