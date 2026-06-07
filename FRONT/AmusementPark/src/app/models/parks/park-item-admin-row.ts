import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';

export interface ParkItemAdminRow {
  id: string;
  parkId: string;
  parkName: string;
  zoneId?: string | null;
  name: string;
  category: ParkItemCategory;
  type: ParkItemType;
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
  contentQuality?: ParkItemContentQuality;
  publicationSignals?: ParkItemAdminPublicationSignals;
}

export interface ParkItemContentQuality {
  structureComplete: boolean;
  hasAnyDescription: boolean;
  hasFrenchDescription: boolean;
  hasEnglishDescription: boolean;
  hasZone: boolean;
  hasPreciseType: boolean;
  hasLocation: boolean;
  hasAccessConditions: boolean;
  isPublishable: boolean;
  availableLanguageCodes: string[];
  missingRequirementKeys: string[];
}

export interface ParkItemAdminPublicationSignals {
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
  lastUpdatedAtUtc?: string | null;
  availableLanguageCodes: string[];
  isPublishable: boolean;
}
