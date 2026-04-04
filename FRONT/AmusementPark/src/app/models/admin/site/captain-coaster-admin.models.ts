export interface AdminDataSourceSummaryResponse {
  sourceKey: string;
  displayName: string;
  description: string;
  inputMode: string;
  isEnabled: boolean;
  lastSuccessfulImportUtc?: string | null;
  lastImportedParkCount: number;
  lastImportedCoasterCount: number;
  lastComparisonResultCount: number;
}

export interface CaptainCoasterSettingsResponse {
  sourceKey: string;
  displayName: string;
  description: string;
  inputMode: string;
  isEnabled: boolean;
  lastSuccessfulImportUtc?: string | null;
  expectedFiles: string[];
}

export interface UpdateCaptainCoasterSettingsRequest {
  isEnabled: boolean;
}

export interface CaptainCoasterSyncLogResponse {
  occurredAtUtc: string;
  level: string;
  message: string;
}

export interface CaptainCoasterSyncSessionResponse {
  id: string;
  sourceKey: string;
  status: string;
  progressPercentage: number;
  currentStep: string;
  message: string;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  sourceFileCount: number;
  sourceFileNames: string[];
  parksFetched: number;
  coastersFetched: number;
  comparisonResults: number;
  appliedChanges: number;
  manifestSummary?: string | null;
  logs: CaptainCoasterSyncLogResponse[];
}

export interface CaptainCoasterFieldChangeResponse {
  field: string;
  localValue?: string | null;
  externalValue?: string | null;
  isDifferent: boolean;
}

export interface CaptainCoasterComparisonResultResponse {
  id: string;
  entityType: string;
  changeType: string;
  displayName: string;
  localEntityId?: string | null;
  externalEntityId?: string | null;
  matchConfidence: string;
  isApplied: boolean;
  changes: CaptainCoasterFieldChangeResponse[];
}
