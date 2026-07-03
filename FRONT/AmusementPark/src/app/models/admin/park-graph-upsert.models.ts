export interface ParkGraphUpsertRequest {
  targetParkId?: string | null;
  createIfMissing: boolean;
  replaceCollections: boolean;
  document: unknown;
}

export type ParkGraphBulkSelectionMode = 'filtered' | 'explicit';

export type ParkGraphExportSection =
  | 'ParkBasics'
  | 'ParkAudience'
  | 'ParkLocation'
  | 'ParkAdministration'
  | 'ParkDescriptions'
  | 'ParkHomeFeature'
  | 'References'
  | 'Zones'
  | 'Items'
  | 'Images'
  | 'OpeningHours'
  | 'History';

export interface ParkGraphBulkExportRequest {
  selectionMode: ParkGraphBulkSelectionMode;
  parkIds: string[];
  searchTerm?: string | null;
  isVisible?: boolean | null;
  adminReviewStatus?: string | null;
  type?: string | null;
  audienceClassification?: string | null;
  countryCode?: string | null;
  hasValidCoordinates?: boolean | null;
  closedFilter?: string | null;
  openingHoursStatus?: string | null;
  sortBy?: string | null;
  sortDirection?: string | null;
  sections: ParkGraphExportSection[];
}

export interface BulkParkGraphUpsertRequest {
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

export interface BulkParkGraphUpsertResult {
  operationId: string;
  isApplied: boolean;
  canApply: boolean;
  previewedAtUtc: string;
  appliedAtUtc?: string | null;
  counts: ParkGraphUpsertCounts;
  parks: BulkParkGraphUpsertParkResult[];
  warnings: string[];
  errors: string[];
}

export interface BulkParkGraphUpsertParkResult {
  index: number;
  targetParkId?: string | null;
  targetParkName?: string | null;
  result: ParkGraphUpsertResult;
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
