export interface DataSourceSummary {
  key: string;
  label: string;
  description: string;
  icon: string;
  isEnabled: boolean;
  lastImportUtc: string | null;
  totalSessions: number;
  statusLabel: string;
}

export interface CaptainCoasterStatusResponse {
  source: string;
  isEnabled: boolean;
  lastSuccessfulImportUtc: string | null;
  totalSessionsCount: number;
}

export interface CaptainCoasterSessionResponse {
  id: string;
  status: string;
  progressPercentage: number;
  currentStep: string;
  message: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  parksFetched: number;
  coastersFetched: number;
  comparisonResults: number;
  appliedChanges: number;
  logs: CaptainCoasterSessionLogResponse[];
}

export interface CaptainCoasterSessionLogResponse {
  occurredAtUtc: string;
  level: string;
  message: string;
}

export interface CaptainCoasterComparisonPagedResponse {
  items: CaptainCoasterComparisonResultResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  sessionUpdatedCount: number;
  sessionMissingCount: number;
  sessionAppliedCount: number;
}

export interface CaptainCoasterComparisonResultResponse {
  id: string;
  entityType: string;
  changeType: string;
  displayName: string;
  localEntityId: string | null;
  externalEntityId: string | null;
  matchConfidence: string;
  isApplied: boolean;
  changes: CaptainCoasterFieldChangeResponse[];
}

export interface CaptainCoasterFieldChangeResponse {
  field: string;
  localValue: string | null;
  externalValue: string | null;
  isDifferent: boolean;
}

export interface ComparisonFilters {
  entityType: string | null;
  changeType: string | null;
  isApplied: boolean | null;
}
