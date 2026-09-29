import { LiveTargetType } from './live-target-mapping.models';

export type LiveOperationalScopeType = 'Source' | 'Park' | 'Target';
export type LivePollingDisposition =
  | 'OutsideActiveWindow'
  | 'Success'
  | 'NotModified'
  | 'RateLimited'
  | 'Failed';

export interface LiveOperationalScope {
  readonly scopeType: LiveOperationalScopeType;
  readonly sourceId: string;
  readonly externalEntityId: string | null;
  readonly internalParkId: string | null;
  readonly targetType: LiveTargetType | null;
  readonly internalTargetId: string | null;
  readonly displayName: string;
  readonly parentDisplayName: string | null;
  readonly collectionEnabled: boolean;
  readonly publicReadEnabled: boolean;
  readonly effectiveCollectionEnabled: boolean;
  readonly effectivePublicReadEnabled: boolean;
  readonly revision: number;
  readonly reason: string | null;
  readonly changedByUserId: string | null;
  readonly recordedAtUtc: string | null;
}

export interface LiveOperationsPolling {
  readonly externalEntityId: string;
  readonly nextAttemptAtUtc: string | null;
  readonly lastPolledAtUtc: string | null;
  readonly lastSuccessfulPollAtUtc: string | null;
  readonly consecutiveFailures: number;
  readonly circuitOpenUntilUtc: string | null;
  readonly lastDisposition: LivePollingDisposition | null;
  readonly leaseActive: boolean;
  readonly leaseExpiresAtUtc: string | null;
}

export interface LiveOperationsSummary {
  readonly mappingCount: number;
  readonly eligibleMappingCount: number;
  readonly candidateMappingCount: number;
  readonly suspendedMappingCount: number;
  readonly pendingIncidentCount: number;
  readonly replayablePendingIncidentCount: number;
}

export interface LiveOperationsDashboard {
  readonly sourceId: string;
  readonly sourceDisplayName: string;
  readonly configuredCollectionEnabled: boolean;
  readonly configuredPublicReadEnabled: boolean;
  readonly adapterVersion: string;
  readonly transformationVersion: string;
  readonly usagePolicyVersion: string;
  readonly termsUrl: string;
  readonly usagePolicyReviewedAtUtc: string;
  readonly attributionText: string;
  readonly attributionUrl: string;
  readonly polling: LiveOperationsPolling;
  readonly summary: LiveOperationsSummary;
  readonly scopes: readonly LiveOperationalScope[];
  readonly generatedAtUtc: string;
}

export interface UpdateLiveOperationalControlRequest {
  readonly scopeType: LiveOperationalScopeType;
  readonly sourceId: string;
  readonly externalEntityId: string | null;
  readonly internalParkId: string | null;
  readonly targetType: LiveTargetType | null;
  readonly internalTargetId: string | null;
  readonly collectionEnabled: boolean;
  readonly publicReadEnabled: boolean;
  readonly expectedRevision: number;
  readonly reason: string;
}

export interface LiveQualityReplay {
  readonly examinedCount: number;
  readonly resolvedCount: number;
  readonly stillBlockedCount: number;
  readonly persistedCount: number;
  readonly ignoredAsOlderCount: number;
}
