export interface ParkGraphUpsertRequest {
  targetParkId?: string | null;
  createIfMissing: boolean;
  replaceCollections: boolean;
  document: unknown;
}

export interface ParkGraphUpsertHistoryEntry {
  id: string;
  operationKind: string;
  targetParkId?: string | null;
  targetParkName?: string | null;
  requestedByUserId?: string | null;
  createdAtUtc: string;
  rawJson: string;
  result: ParkGraphUpsertResult;
}

export interface ParkGraphUpsertResult {
  operationId: string;
  mode: string;
  isApplied: boolean;
  canApply: boolean;
  previewedAtUtc: string;
  appliedAtUtc?: string | null;
  targetParkId?: string | null;
  targetParkName?: string | null;
  counts: ParkGraphUpsertCounts;
  changes: ParkGraphUpsertChange[];
  warnings: string[];
  errors: string[];
}

export interface ParkGraphUpsertCounts {
  created: number;
  updated: number;
  deleted: number;
  unchanged: number;
  warnings: number;
  errors: number;
}

export interface ParkGraphUpsertChange {
  entityType: string;
  entityId?: string | null;
  entityKey?: string | null;
  displayName: string;
  changeType: string;
  matchedBy: string;
  fields: ParkGraphUpsertFieldChange[];
}

export interface ParkGraphUpsertFieldChange {
  field: string;
  oldValue?: string | null;
  newValue?: string | null;
}
