export type AdminReviewStatus = 'Ready' | 'ToProcessLater';

export interface BulkAdministrationUpdateRequest {
  ids: string[];
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
}

export interface BulkAdministrationUpdateResult {
  requestedCount: number;
  updatedCount: number;
}
