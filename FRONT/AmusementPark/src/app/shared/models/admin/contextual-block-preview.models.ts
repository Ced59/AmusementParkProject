export interface ContextualBlockPreviewTarget {
  readonly entityType: string;
  readonly entityId: string;
  readonly displayName: string;
}

export interface ContextualBlockPreviewCounts {
  readonly created: number;
  readonly updated: number;
  readonly deleted: number;
  readonly unchanged: number;
  readonly warnings: number;
  readonly errors: number;
}

export interface ContextualBlockPreviewChange {
  readonly entityType: string;
  readonly entityId: string;
  readonly displayName: string;
  readonly field: string;
  readonly languageCode: string | null;
  readonly changeType: string;
  readonly oldValue: string | null;
  readonly newValue: string | null;
}

export interface ContextualBlockPreviewResult {
  readonly operationId: string;
  readonly blockType: string;
  readonly isApplied: boolean;
  readonly canApply: boolean;
  readonly previewedAtUtc: string;
  readonly target: ContextualBlockPreviewTarget;
  readonly counts: ContextualBlockPreviewCounts;
  readonly changes: readonly ContextualBlockPreviewChange[];
  readonly warnings: readonly string[];
  readonly errors: readonly string[];
}
