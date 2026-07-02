import { ParkAudienceClassificationFilter } from '../parks/park-audience-classification';

export type AdminReviewStatus = 'ToReview' | 'Validated' | 'ToProcessLater' | 'NotRelevant';

export interface BulkAdministrationUpdateRequest {
  ids: string[];
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  filterIsVisible?: boolean | null;
  filterAdminReviewStatus?: AdminReviewStatus | null;
  filterType?: string | null;
  filterAudienceClassification?: ParkAudienceClassificationFilter | null;
  filterCountryCode?: string | null;
  filterHasValidCoordinates?: boolean | null;
}

export interface BulkReviewStatusUpdateRequest {
  ids: string[];
  adminReviewStatus: AdminReviewStatus;
}

export interface BulkAdministrationUpdateResult {
  requestedCount: number;
  updatedCount: number;
}

export const ADMIN_REVIEW_STATUSES: readonly AdminReviewStatus[] = [
  'ToReview',
  'Validated',
  'ToProcessLater',
  'NotRelevant'
] as const;

export function getAdminReviewStatusTranslationKey(status: AdminReviewStatus | null | undefined): string {
  switch (status) {
    case 'Validated':
      return 'admin.reviewStatus.validated';
    case 'ToProcessLater':
      return 'admin.reviewStatus.toProcessLater';
    case 'NotRelevant':
      return 'admin.reviewStatus.notRelevant';
    case 'ToReview':
    default:
      return 'admin.reviewStatus.toReview';
  }
}

export function getAdminReviewStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
  switch (status) {
    case 'Validated':
      return 'success';
    case 'ToProcessLater':
      return 'warn';
    case 'NotRelevant':
      return 'danger';
    case 'ToReview':
    default:
      return 'info';
  }
}
