import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';

export interface ParkItemBulkCreateDraft {
  rowNumber: number;
  name?: string | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  zoneId?: string | null;
  zoneName?: string | null;
  manufacturerId?: string | null;
  manufacturerName?: string | null;
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  descriptionFr?: string | null;
}

export interface ParkItemsBulkCreateRequest {
  parkId: string;
  rows: ParkItemBulkCreateDraft[];
}

export interface ParkItemsBulkCreatePreviewResult {
  rows: ParkItemBulkCreatePreviewRow[];
  readyCount: number;
  warningCount: number;
  errorCount: number;
}

export interface ParkItemsBulkCreateApplyResult {
  rows: ParkItemBulkCreatePreviewRow[];
  createdIds: string[];
  requestedCount: number;
  createdCount: number;
  ignoredCount: number;
}

export interface ParkItemBulkCreatePreviewRow {
  rowNumber: number;
  name: string;
  category: ParkItemCategory;
  type: ParkItemType;
  zoneId?: string | null;
  zoneName?: string | null;
  manufacturerId?: string | null;
  manufacturerName?: string | null;
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
  descriptionFr?: string | null;
  canApply: boolean;
  errors: string[];
  warnings: string[];
}
