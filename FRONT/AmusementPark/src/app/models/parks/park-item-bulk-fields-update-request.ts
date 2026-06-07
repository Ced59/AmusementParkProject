import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';

export interface ParkItemBulkFieldsUpdateRequest {
  ids: string[];
  updateZone?: boolean;
  zoneId?: string | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  updateManufacturer?: boolean;
  manufacturerId?: string | null;
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
}
